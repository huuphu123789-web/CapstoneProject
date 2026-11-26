using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Tự động dựng và vận hành giao diện đổi phím (Key Rebinding) trong tab Controls của SettingPanel.
/// Hỗ trợ đổi các phím Di chuyển (Tiến, Lùi, Trái, Phải), Chạy nhanh, Nhảy, Tương tác, Đèn pin
/// và nút Reset Defaults.
/// </summary>
public class ControlsRebindPanel : MonoBehaviour
{
    private static readonly GameAction[] DisplayedActions = new GameAction[]
    {
        GameAction.MoveForward,
        GameAction.MoveBackward,
        GameAction.MoveLeft,
        GameAction.MoveRight,
        GameAction.Sprint,
        GameAction.Jump,
        GameAction.Interact,
        GameAction.Flashlight
    };

    private readonly Dictionary<GameAction, TextMeshProUGUI> keyTexts = new Dictionary<GameAction, TextMeshProUGUI>();
    private readonly Dictionary<GameAction, Image> buttonBackgrounds = new Dictionary<GameAction, Image>();

    private bool isBuilt = false;
    private GameAction? pendingAction = null;
    private int rebindStartFrame = -1;

    private readonly Color rowBgColor = new Color(0.06f, 0.07f, 0.09f, 0.55f);
    private readonly Color btnNormalColor = new Color(0.02f, 0.02f, 0.03f, 0.94f);
    private readonly Color btnHoverColor = new Color(0.18f, 0.19f, 0.24f, 0.98f);
    private readonly Color btnListeningColor = new Color(0.78f, 0.52f, 0.10f, 0.98f);
    private readonly Color resetBtnColor = new Color(0.55f, 0.16f, 0.16f, 0.95f);
    private readonly Color resetHoverColor = new Color(0.72f, 0.22f, 0.22f, 0.98f);

    void Awake()
    {
        EnsureBuilt();
    }

    void OnEnable()
    {
        EnsureBuilt();
        CancelRebind();
        RefreshAllKeyLabels();
        KeybindManager.OnBindingsChanged += RefreshAllKeyLabels;
    }

    void OnDisable()
    {
        KeybindManager.OnBindingsChanged -= RefreshAllKeyLabels;
        CancelRebind();
    }

    void Update()
    {
        if (!pendingAction.HasValue) return;
        if (Time.frameCount == rebindStartFrame) return;

        if (KeybindManager.TryDetectPressedKey(out KeyCode pressed))
        {
            if (pressed == KeyCode.Escape)
            {
                CancelRebind();
            }
            else
            {
                GameAction target = pendingAction.Value;
                pendingAction = null;
                KeybindManager.IsRebinding = false;
                KeybindManager.SetKey(target, pressed);
                RefreshAllKeyLabels();

                if (AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
                {
                    AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
                }
            }
        }
    }

    public void EnsureBuilt()
    {
        if (isBuilt) return;
        isBuilt = true;

        KeybindManager.EnsureInitialized();

        // Xóa bỏ ảnh hướng dẫn điều khiển cũ (nếu còn dính trên Image của Panel_Controls)
        Image panelImg = GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.sprite = null;
            panelImg.color = new Color(0f, 0f, 0f, 0f);
        }

        // Lấy đúng font của Panel_GamePlay (LiberationSans SDF / mặc định TMP) cho đồng bộ 100%
        TMP_FontAsset panelFont = FindGameplayFont();
        if (panelFont == null) panelFont = TMP_Settings.defaultFontAsset;

        // Xóa container cũ nếu tồn tại
        Transform old = transform.Find("ControlsRebindList");
        if (old != null) Destroy(old.gameObject);

        // Danh sách 1 cột dọc gọn gàng, neo ngay dưới thanh Tab và phía trên nút Back
        GameObject listGO = new GameObject("ControlsRebindList");
        listGO.transform.SetParent(transform, false);
        RectTransform listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0.06f, 0.10f);
        listRT.anchorMax = new Vector2(0.94f, 0.82f);
        listRT.offsetMin = Vector2.zero;
        listRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Dòng hướng dẫn phía trên (to, rõ, không đè lên thanh Tab)
        CreateHintHeader(listGO.transform, panelFont);

        // Danh sách 8 dòng đổi phím theo kiểu list dọc
        for (int i = 0; i < DisplayedActions.Length; i++)
        {
            CreateBindingRow(listGO.transform, DisplayedActions[i], panelFont);
        }

        // Nút Reset Defaults ở cuối bên phải
        CreateResetRow(listGO.transform, panelFont);

        RefreshAllKeyLabels();
    }

    private TMP_FontAsset FindGameplayFont()
    {
        if (transform.parent == null) return null;
        Transform gp = transform.parent.Find("Panel_GamePlay");
        if (gp != null)
        {
            TextMeshProUGUI gpText = gp.GetComponentInChildren<TextMeshProUGUI>(true);
            if (gpText != null && gpText.font != null) return gpText.font;
        }
        return null;
    }

    private void CreateHintHeader(Transform parent, TMP_FontAsset font)
    {
        GameObject hintGO = new GameObject("RebindHint");
        hintGO.transform.SetParent(parent, false);
        LayoutElement le = hintGO.AddComponent<LayoutElement>();
        le.preferredHeight = 42f;
        le.minHeight = 36f;

        TextMeshProUGUI txt = hintGO.AddComponent<TextMeshProUGUI>();
        txt.text = "Click a key to rebind  •  Press ESC to cancel";
        txt.fontSize = 30f;
        txt.fontStyle = FontStyles.Normal;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.9f, 0.45f, 1f);
        if (font != null) txt.font = font;
    }

    private void CreateBindingRow(Transform parent, GameAction action, TMP_FontAsset font)
    {
        GameObject rowGO = new GameObject("Row_" + action);
        rowGO.transform.SetParent(parent, false);

        Image rowBg = rowGO.AddComponent<Image>();
        rowBg.color = rowBgColor;

        LayoutElement rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 56f;
        rowLE.minHeight = 48f;

        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 16, 6, 6);
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Tên hành động (bên trái - đồng bộ font & cỡ chữ của Panel_GamePlay)
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowGO.transform, false);
        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;

        TextMeshProUGUI labelTxt = labelGO.AddComponent<TextMeshProUGUI>();
        labelTxt.text = KeybindManager.GetActionLabel(action);
        labelTxt.fontSize = 34f;
        labelTxt.fontStyle = FontStyles.Normal;
        labelTxt.alignment = TextAlignmentOptions.MidlineLeft;
        labelTxt.color = Color.white;
        if (font != null) labelTxt.font = font;

        // Nút bấm đổi phím (bên phải)
        GameObject btnGO = new GameObject("KeyButton");
        btnGO.transform.SetParent(rowGO.transform, false);
        LayoutElement btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 220f;
        btnLE.minWidth = 190f;

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = btnNormalColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        GameAction captured = action;
        btn.onClick.AddListener(() =>
        {
            PlayClickSound();
            StartRebind(captured);
        });
        AddHoverEvents(btnGO, () =>
        {
            PlayHoverSound();
            if (!(pendingAction.HasValue && pendingAction.Value == captured))
                btnImg.color = btnHoverColor;
        }, () =>
        {
            if (!(pendingAction.HasValue && pendingAction.Value == captured))
                btnImg.color = btnNormalColor;
        });

        GameObject btnTxtGO = new GameObject("KeyText");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTxtGO.AddComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = Vector2.zero;
        btnTxtRT.offsetMax = Vector2.zero;

        TextMeshProUGUI keyTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
        keyTxt.fontSize = 28f;
        keyTxt.fontStyle = FontStyles.Normal;
        keyTxt.alignment = TextAlignmentOptions.Center;
        keyTxt.color = Color.white;
        if (font != null) keyTxt.font = font;

        keyTexts[action] = keyTxt;
        buttonBackgrounds[action] = btnImg;
    }

    private void CreateResetRow(Transform parent, TMP_FontAsset font)
    {
        GameObject holderGO = new GameObject("ResetRow");
        holderGO.transform.SetParent(parent, false);
        LayoutElement holderLE = holderGO.AddComponent<LayoutElement>();
        holderLE.preferredHeight = 58f;
        holderLE.minHeight = 50f;

        HorizontalLayoutGroup hlg = holderGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 8, 0);
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        GameObject btnGO = new GameObject("ResetDefaultsButton");
        btnGO.transform.SetParent(holderGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(260f, 48f);

        Image img = btnGO.AddComponent<Image>();
        img.color = resetBtnColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            PlayClickSound();
            CancelRebind();
            KeybindManager.ResetToDefaults();
            RefreshAllKeyLabels();
        });
        AddHoverEvents(btnGO, () =>
        {
            PlayHoverSound();
            img.color = resetHoverColor;
        }, () =>
        {
            img.color = resetBtnColor;
        });

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = "Reset Defaults";
        txt.fontSize = 26f;
        txt.fontStyle = FontStyles.Normal;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        if (font != null) txt.font = font;
    }

    private void AddHoverEvents(GameObject target, System.Action onEnter, System.Action onExit)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter?.Invoke());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit?.Invoke());
        trigger.triggers.Add(exitEntry);
    }

    private void PlayHoverSound()
    {
        if (AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null && AudioManager.instance.buttonclickSFX != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonclickSFX);
        }
    }

    private void StartRebind(GameAction action)
    {
        pendingAction = action;
        rebindStartFrame = Time.frameCount;
        KeybindManager.IsRebinding = true;
        RefreshAllKeyLabels();
    }

    public void CancelRebind()
    {
        pendingAction = null;
        KeybindManager.IsRebinding = false;
        RefreshAllKeyLabels();
    }

    public void RefreshAllKeyLabels()
    {
        foreach (var kvp in keyTexts)
        {
            GameAction act = kvp.Key;
            TextMeshProUGUI txt = kvp.Value;
            if (txt == null) continue;

            bool listening = pendingAction.HasValue && pendingAction.Value == act;
            txt.text = listening ? "PRESS KEY..." : KeybindManager.GetKeyDisplayName(act);

            if (buttonBackgrounds.TryGetValue(act, out Image bg) && bg != null)
            {
                bg.color = listening ? btnListeningColor : btnNormalColor;
            }
        }
    }
}
