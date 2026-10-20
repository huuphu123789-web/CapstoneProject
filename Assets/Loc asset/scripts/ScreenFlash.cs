using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiệu ứng chớp lóe toàn màn hình (Screen Flash).
/// Tự động tạo Canvas và Image nếu chưa được cấu hình sẵn trong Inspector.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [Header("=== Cấu Hình Chớp ===")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("=== Tham Chiếu UI (Tùy chọn) ===")]
    [Tooltip("Kéo Image UI làm màn chớp vào đây. Nếu để trống, script tự động tạo Canvas và Image lúc Start.")]
    [SerializeField] private Image flashImage;

    private float _currentAlpha = 0f;

    private void Awake()
    {
        // Thiết lập Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động sinh Canvas và Image nếu người dùng không gán sẵn
        if (flashImage == null)
        {
            CreateDefaultFlashUI();
        }
    }

    /// <summary>
    /// Tạo tự động Canvas UI để làm hiệu ứng chớp lóe mà không cần dựng tay trong Editor.
    /// </summary>
    private void CreateDefaultFlashUI()
    {
        GameObject canvasGo = new GameObject("ScreenFlashCanvas");
        canvasGo.transform.SetParent(transform);
        
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Đảm bảo đè lên trên mọi UI khác của game
        
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("FlashImage");
        imageGo.transform.SetParent(canvasGo.transform, false);
        
        flashImage = imageGo.AddComponent<Image>();
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashImage.raycastTarget = false; // Tránh chặn tia click của chuột khi đang chơi

        // Căn chỉnh RectTransform để Image phủ kín toàn màn hình
        RectTransform rect = flashImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (_currentAlpha > 0f)
        {
            // Giảm độ mờ dần theo thời gian fadeDuration
            _currentAlpha -= Time.deltaTime / fadeDuration;
            if (_currentAlpha < 0f) _currentAlpha = 0f;
            SetAlpha(_currentAlpha);
        }
    }

    /// <summary>
    /// Kích hoạt chớp màn hình với cường độ ban đầu chỉ định.
    /// </summary>
    /// <param name="intensity">Độ sáng ban đầu (0 đến 1)</param>
    public void TriggerFlash(float intensity = 0.8f)
    {
        _currentAlpha = Mathf.Clamp01(intensity);
        SetAlpha(_currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (flashImage != null)
        {
            Color col = flashImage.color;
            col.a = alpha;
            flashImage.color = col;
        }
    }
}
