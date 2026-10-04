using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Quản lý hệ thống Chuyển Tab (GAMEPLAY, CONTROLS, AUDIO, VISUALS) phong cách AAA:
/// - Tab được chọn: Hiện vệt cọ (Brush Stroke) màu trắng phía sau, chữ chuyển màu tối rõ nét.
/// - Tab không chọn: Ẩn vệt cọ, chữ màu xám mờ. Khi rê chuột (Hover) thì sáng nhẹ.
/// - Tự động bật/tắt Panel nội dung tương ứng khi click chuyển tab.
/// - Hỗ trợ phím Q / E (hoặc LB / RB) để chuyển tab bằng bàn phím tiện lợi như game console.
/// </summary>
public class SettingsTabGroup : MonoBehaviour
{
    [System.Serializable]
    public class TabItem
    {
        public string tabName = "New Tab";
        [Tooltip("Nút bấm của tab")]
        public Button button;
        [Tooltip("Image vệt cọ quét phía sau chữ (gán sprite UI_Highlight_ChalkBrush)")]
        public Image brushBackground;
        [Tooltip("Chữ tiêu đề tab (TextMeshPro)")]
        public TextMeshProUGUI tabText;
        [Tooltip("Panel giao diện chứa các cài đặt của tab này")]
        public GameObject contentPanel;

        [HideInInspector] public CanvasGroup brushCanvasGroup;
        [HideInInspector] public Coroutine fadeCoroutine;
    }

    [Header("=== DANH SÁCH CÁC TAB ===")]
    public List<TabItem> tabs = new List<TabItem>();

    [Header("=== TAB MẶC ĐỊNH KHI MỞ BẢNG ===")]
    [Tooltip("Vị trí tab mặc định được chọn khi mở bảng (0 là Tab đầu tiên)")]
    public int defaultTabIndex = 0;

    [Header("=== MÀU SẮC CHỮ (TEXT COLORS) ===")]
    [Tooltip("Màu chữ khi tab ĐƯỢC CHỌN (nằm trên nền cọ trắng nên để màu tối/đen)")]
    public Color textActiveColor = new Color(0.1f, 0.1f, 0.1f, 1.0f); // #1A1A1A

    [Tooltip("Màu chữ khi tab KHÔNG CHỌN (màu trắng mờ)")]
    public Color textInactiveColor = new Color(1f, 1f, 1f, 0.45f);

    [Tooltip("Màu chữ khi RÊ CHUỘT VÀO tab chưa chọn (sáng rõ hơn)")]
    public Color textHoverColor = Color.white;

    [Header("=== HIỆU ỨNG VỆT CỌ (BRUSH ANIMATION) ===")]
    [Range(0.05f, 0.5f)]
    [Tooltip("Thời gian mờ / hiện vệt cọ (giây)")]
    public float transitionDuration = 0.2f;

    [Tooltip("Độ dãn nhẹ vệt cọ khi xuất hiện")]
    public bool enableScalePulse = true;
    public Vector3 brushNormalScale = Vector3.one;
    public Vector3 brushActiveScale = new Vector3(1.05f, 1.02f, 1f);

    [Header("=== PHÍM TẮT ĐỔI TAB ===")]
    [Tooltip("Cho phép bấm Q / E để chuyển qua lại giữa các tab")]
    public bool enableKeyboardShortcuts = true;

    [Header("=== ÂM THANH (SFX) ===")]
    public bool playSounds = true;

    private int currentActiveIndex = -1;

    private void Awake()
    {
        InitializeTabs();
    }

    private void OnEnable()
    {
        // Khi bảng mở lên, luôn mở tab mặc định (hoặc tab đang chọn trước đó)
        int targetIndex = currentActiveIndex >= 0 ? currentActiveIndex : defaultTabIndex;
        SelectTab(targetIndex, playAudio: false);
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts || tabs.Count <= 1) return;

        // Phím Q: Sang tab bên trái
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int prevIndex = (currentActiveIndex - 1 + tabs.Count) % tabs.Count;
            SelectTab(prevIndex);
        }
        // Phím E: Sang tab bên phải
        else if (Input.GetKeyDown(KeyCode.E))
        {
            int nextIndex = (currentActiveIndex + 1) % tabs.Count;
            SelectTab(nextIndex);
        }
    }

    private void InitializeTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            TabItem tab = tabs[i];
            if (tab == null) continue;

            // Thiết lập CanvasGroup cho vệt cọ để làm mượt fade
            if (tab.brushBackground != null)
            {
                tab.brushCanvasGroup = tab.brushBackground.GetComponent<CanvasGroup>();
                if (tab.brushCanvasGroup == null)
                {
                    tab.brushCanvasGroup = tab.brushBackground.gameObject.AddComponent<CanvasGroup>();
                }
                tab.brushCanvasGroup.blocksRaycasts = false;
                tab.brushCanvasGroup.interactable = false;
                tab.brushBackground.raycastTarget = false;
            }

            // Gắn sự kiện click nút tự động
            if (tab.button != null)
            {
                tab.button.onClick.RemoveAllListeners();
                tab.button.onClick.AddListener(() => SelectTab(index));

                // Bổ sung EventTrigger để bắt Hover (PointerEnter, PointerExit)
                AddHoverTrigger(tab.button.gameObject, index);
            }
        }
    }

    private void AddHoverTrigger(GameObject targetObj, int tabIndex)
    {
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObj.AddComponent<EventTrigger>();

        // Pointer Enter
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) => OnTabHover(tabIndex, true));
        trigger.triggers.Add(enter);

        // Pointer Exit
        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) => OnTabHover(tabIndex, false));
        trigger.triggers.Add(exit);
    }

    public void SelectTab(int index, bool playAudio = true)
    {
        if (index < 0 || index >= tabs.Count) return;

        int previousIndex = currentActiveIndex;
        currentActiveIndex = index;

        // Cập nhật trạng thái hiển thị của từng tab
        for (int i = 0; i < tabs.Count; i++)
        {
            TabItem tab = tabs[i];
            if (tab == null) continue;

            bool isActive = (i == currentActiveIndex);

            // Bật / tắt Panel nội dung
            if (tab.contentPanel != null)
            {
                tab.contentPanel.SetActive(isActive);
            }

            // Chuyển màu chữ
            if (tab.tabText != null)
            {
                tab.tabText.color = isActive ? textActiveColor : textInactiveColor;
            }

            // Hiệu ứng Fade vệt cọ
            if (tab.brushBackground != null)
            {
                if (tab.fadeCoroutine != null) StopCoroutine(tab.fadeCoroutine);
                tab.fadeCoroutine = StartCoroutine(AnimateBrush(tab, isActive));
            }
        }

        // Phát âm thanh chuyển tab nếu có AudioManager
        if (playAudio && playSounds && previousIndex != currentActiveIndex)
        {
            if (AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
            }
        }
    }

    private void OnTabHover(int index, bool isHovering)
    {
        // Nếu là tab đang được chọn thì không đổi màu hover
        if (index == currentActiveIndex || index < 0 || index >= tabs.Count) return;

        TabItem tab = tabs[index];
        if (tab == null || tab.tabText == null) return;

        tab.tabText.color = isHovering ? textHoverColor : textInactiveColor;

        if (isHovering && playSounds && AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
        }
    }

    private IEnumerator AnimateBrush(TabItem tab, bool show)
    {
        float duration = transitionDuration;
        float startAlpha = tab.brushCanvasGroup != null ? tab.brushCanvasGroup.alpha : (show ? 0f : 1f);
        float targetAlpha = show ? 1f : 0f;

        Transform brushT = tab.brushBackground.transform;
        Vector3 startScale = brushT.localScale;
        Vector3 targetScale = show ? brushActiveScale : brushNormalScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (tab.brushCanvasGroup != null)
            {
                tab.brushCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            }

            if (enableScalePulse)
            {
                brushT.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            }

            yield return null;
        }

        if (tab.brushCanvasGroup != null) tab.brushCanvasGroup.alpha = targetAlpha;
        if (enableScalePulse) brushT.localScale = targetScale;
        tab.fadeCoroutine = null;
    }
}
