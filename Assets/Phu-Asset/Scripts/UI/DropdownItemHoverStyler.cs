using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Gắn vào GameObject 'Item' bên trong Template của Dropdown
/// để tạo hiệu ứng hover mượt mà, vệt sáng highlight mờ phía sau và đổi màu chữ cho từng dòng.
/// </summary>
public class DropdownItemHoverStyler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("=== VỆT SÁNG HIGHLIGHT PHÍA SAU ===")]
    [Tooltip("Image nền của item (thường là GameObject 'Item Background')")]
    public Image highlightImage;

    [Range(0f, 1f)]
    [Tooltip("Độ mờ khi không hover (mặc định 0: trong suốt hoàn toàn)")]
    public float normalAlpha = 0f;

    [Range(0f, 1f)]
    [Tooltip("Độ mờ khi rê chuột vào (mặc định 0.45: vệt sáng mờ mờ huyền ảo)")]
    public float hoverAlpha = 0.45f;

    [Header("=== MÀU CHỮ (TEXT) ===")]
    public TextMeshProUGUI itemText;
    public Text legacyText;
    public Color textNormalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color textHoverColor = Color.white;

    [Header("=== ÂM THANH RÊ CHUỘT ===")]
    public bool playHoverSound = true;

    private void Awake()
    {
        if (highlightImage == null)
        {
            Transform bg = transform.Find("Item Background");
            if (bg != null) highlightImage = bg.GetComponent<Image>();
        }

        if (itemText == null)
        {
            itemText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (legacyText == null && itemText == null)
        {
            legacyText = GetComponentInChildren<Text>();
        }
    }

    private void OnEnable()
    {
        SetHoverState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverState(true);

        if (playHoverSound && AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverState(false);
    }

    private void SetHoverState(bool isHovered)
    {
        // 1. Chỉnh độ mờ của vệt sáng
        if (highlightImage != null)
        {
            Color c = highlightImage.color;
            c.a = isHovered ? hoverAlpha : normalAlpha;
            highlightImage.color = c;
        }

        // 2. Chỉnh màu chữ
        if (itemText != null)
        {
            itemText.color = isHovered ? textHoverColor : textNormalColor;
        }
        else if (legacyText != null)
        {
            legacyText.color = isHovered ? textHoverColor : textNormalColor;
        }
    }
}
