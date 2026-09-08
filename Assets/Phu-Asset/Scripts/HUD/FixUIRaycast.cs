using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên PauseMenuPanel.
/// Tự động tắt Raycast Target của các Text/Image trang trí (không phải Button/Slider/Dropdown)
/// để tránh chặn click nút Back và các nút khác.
/// </summary>
public class FixUIRaycast : MonoBehaviour
{
    void Awake()
    {
        // Tìm tất cả Graphic con (Image, Text, TMP_Text, RawImage...)
        Graphic[] allGraphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic g in allGraphics)
        {
            // Giữ Raycast Target cho các thành phần tương tác
            if (g.GetComponent<Button>() != null) continue;
            if (g.GetComponent<Slider>() != null) continue;
            if (g.GetComponent<Toggle>() != null) continue;
            if (g.GetComponent<Dropdown>() != null) continue;
            if (g.GetComponent<TMPro.TMP_Dropdown>() != null) continue;
            if (g.GetComponent<UnityEngine.UI.InputField>() != null) continue;
            if (g.GetComponent<TMPro.TMP_InputField>() != null) continue;
            if (g.GetComponent<Scrollbar>() != null) continue;
            if (g.GetComponent<ScrollRect>() != null) continue;

            // Nếu là con trực tiếp của Button/Slider thì giữ nguyên
            if (g.transform.parent != null)
            {
                if (g.transform.parent.GetComponent<Button>() != null) continue;
                if (g.transform.parent.GetComponent<Slider>() != null) continue;
                if (g.transform.parent.GetComponent<Toggle>() != null) continue;
            }

            // Tắt Raycast Target của Text, Image trang trí
            g.raycastTarget = false;
        }
    }
}
