using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tự động cập nhật con số hiển thị bên cạnh Slider khi kéo thanh trượt.
/// Hỗ trợ hiển thị phần trăm (100%), số thập phân (0.3), hoặc số nguyên (90).
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderValueDisplay : MonoBehaviour
{
    public enum DisplayFormat
    {
        Percentage,   // 0.0 - 1.0 -> 0% - 100%
        Decimal1,     // 0.3
        Decimal2,     // 0.35
        Integer       // 90
    }

    [Header("=== TEXT HIỂN THỊ CON SỐ ===")]
    [Tooltip("TextMeshPro text nằm bên cạnh thanh trượt để hiển thị giá trị")]
    public TextMeshProUGUI valueText;

    [Tooltip("Định dạng hiển thị của con số")]
    public DisplayFormat format = DisplayFormat.Decimal1;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.AddListener(UpdateValueText);
            UpdateValueText(slider.value);
        }
    }

    public void UpdateValueText(float value)
    {
        if (valueText == null) return;

        switch (format)
        {
            case DisplayFormat.Percentage:
                valueText.text = Mathf.RoundToInt(value * 100f) + "%";
                break;
            case DisplayFormat.Decimal1:
                valueText.text = value.ToString("0.0");
                break;
            case DisplayFormat.Decimal2:
                valueText.text = value.ToString("0.00");
                break;
            case DisplayFormat.Integer:
                valueText.text = Mathf.RoundToInt(value).ToString();
                break;
        }
    }
}
