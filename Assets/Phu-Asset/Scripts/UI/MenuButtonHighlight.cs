using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Hiệu ứng vệt sáng mờ ảo (Soft Glow / Brush Highlight) cho Menu Button.
/// - Sáng ở giữa và mờ dần ra 2 bên mép.
/// - Màu trắng mờ mờ huyền ảo nằm phía sau chữ, không làm lóa mắt.
/// - Hiện ra từ từ (Fade In) khi rê chuột/chọn và mờ dần đi (Fade Out) khi rời đi.
/// - Tùy chọn dãn nhẹ chiều ngang (Scale Expand) tạo cảm giác sống động, điện ảnh.
/// </summary>
[DisallowMultipleComponent]
public class MenuButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("=== HIGHLIGHT IMAGE (VỆT SÁNG PHÍA SAU) ===")]
    [Tooltip("Image hiển thị vệt sáng mờ. Nếu để trống, script sẽ tự tìm Image con tên 'Highlight' hoặc tự tạo.")]
    public Image highlightImage;

    [Header("=== ĐỘ MỜ & TỐC ĐỘ HIỆN RA TỪ TỪ ===")]
    [Range(0.05f, 1f)]
    [Tooltip("Độ sáng tối đa của vệt trắng mờ phía sau chữ (Mặc định 0.4 - 0.5 để mờ mờ vừa đẹp)")]
    public float maxAlpha = 0.45f;

    [Tooltip("Thời gian hiện ra từ từ khi rê chuột vào (giây)")]
    [Range(0.05f, 1f)]
    public float fadeInDuration = 0.25f;

    [Tooltip("Thời gian mờ dần biến mất khi rời chuột (giây)")]
    [Range(0.05f, 1f)]
    public float fadeOutDuration = 0.2f;

    [Header("=== HIỆU ỨNG CO GIÃN NHẸ (CINEMATIC EXPAND) ===")]
    [Tooltip("Khi hiện ra, vệt sáng sẽ nở nhẹ theo chiều ngang tạo cảm giác mượt mà")]
    public bool enableScaleEffect = true;
    public Vector3 normalScale = new Vector3(0.85f, 0.9f, 1f);
    public Vector3 hoverScale = new Vector3(1.05f, 1.0f, 1f);

    [Header("=== CHỮ (TEXTMESHPRO) ===")]
    [Tooltip("Text chữ của nút (tùy chọn). Sẽ tự động tìm nếu để trống.")]
    public TextMeshProUGUI buttonText;
    public bool changeTextColor = true;
    public Color textNormalColor = new Color(0.85f, 0.85f, 0.85f, 0.85f);
    public Color textHoverColor = Color.white;

    [Header("=== ÂM THANH HOVER (TÙY CHỌN) ===")]
    public bool playHoverSound = true;

    private Coroutine fadeRoutine;
    private CanvasGroup canvasGroup;
    private RectTransform highlightRect;

    private void Awake()
    {
        SetupHighlight();

        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        // Khởi tạo trạng thái ban đầu: hoàn toàn ẩn (alpha = 0)
        SetAlphaImmediate(0f);
        if (highlightRect != null && enableScaleEffect)
        {
            highlightRect.localScale = normalScale;
        }

        if (buttonText != null && changeTextColor)
        {
            buttonText.color = textNormalColor;
        }
    }

    private void SetupHighlight()
    {
        // Nếu chưa gán highlightImage, tìm trong các GameObject con
        if (highlightImage == null)
        {
            Transform hl = transform.Find("Highlight");
            if (hl != null)
            {
                highlightImage = hl.GetComponent<Image>();
            }
        }

        if (highlightImage != null)
        {
            highlightRect = highlightImage.rectTransform;

            // Đảm bảo vệt sáng luôn nằm PHÍA SAU chữ
            highlightImage.transform.SetAsFirstSibling();

            // Tắt raycastTarget để không chặn click chuột của nút
            highlightImage.raycastTarget = false;

            // Dùng CanvasGroup để làm mượt alpha hoàn hảo
            canvasGroup = highlightImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = highlightImage.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideHighlight();
    }

    public void ShowHighlight()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(AnimateHighlight(true));

        if (buttonText != null && changeTextColor)
        {
            buttonText.color = textHoverColor;
        }

        if (playHoverSound && AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
        }
    }

    public void HideHighlight()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(AnimateHighlight(false));

        if (buttonText != null && changeTextColor)
        {
            buttonText.color = textNormalColor;
        }
    }

    private IEnumerator AnimateHighlight(bool fadeIn)
    {
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : (highlightImage != null ? highlightImage.color.a : 0f);
        float targetAlpha = fadeIn ? maxAlpha : 0f;

        Vector3 startScale = highlightRect != null ? highlightRect.localScale : normalScale;
        Vector3 targetScale = fadeIn ? hoverScale : normalScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Dùng unscaledDeltaTime để hoạt động cả khi pause game
            float t = Mathf.Clamp01(elapsed / duration);
            // Dùng hàm làm mượt SmoothStep
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            SetAlphaImmediate(currentAlpha);

            if (highlightRect != null && enableScaleEffect)
            {
                highlightRect.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            }

            yield return null;
        }

        SetAlphaImmediate(targetAlpha);
        if (highlightRect != null && enableScaleEffect)
        {
            highlightRect.localScale = targetScale;
        }

        fadeRoutine = null;
    }

    private void SetAlphaImmediate(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        else if (highlightImage != null)
        {
            Color c = highlightImage.color;
            c.a = alpha;
            highlightImage.color = c;
        }
    }

    private void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        SetAlphaImmediate(0f);
    }
}
