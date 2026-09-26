using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiệu ứng chuyển cảnh mượt mà (Fade In / Fade Out) cho game.
/// Tự động tồn tại qua các Scene (DontDestroyOnLoad) và sử dụng đồng thời:
/// 1) Canvas Overlay + Image (đảm bảo hiển thị hoàn hảo trên Universal Render Pipeline / URP).
/// 2) OnGUI Texture (fallback dự phòng).
/// </summary>
public class SceneFader : MonoBehaviour
{
    private static SceneFader _instance;
    public static SceneFader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneFader");
                _instance = go.AddComponent<SceneFader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("=== CẤU HÌNH FADE ===")]
    [Tooltip("Thời gian tối dần khi rời scene (giây)")]
    public float fadeOutDuration = 1.0f;

    [Tooltip("Thời gian sáng dần khi vào scene mới (giây)")]
    public float fadeInDuration = 2.0f;

    [Tooltip("Thời gian chờ trong bóng tối trước khi bắt đầu sáng lên (giây)")]
    public float blackScreenHoldDuration = 0.3f;

    [Tooltip("Màu sắc màn hình che phủ")]
    public Color fadeColor = Color.black;

    private float _currentAlpha = 0f;
    private bool _isFading = false;
    private Texture2D _fadeTexture;

    // UI Canvas Overlay để hiển thị trên URP/Post-Processing
    private Canvas _fadeCanvas;
    private Image _fadeImage;

    public bool IsFading => _isFading;
    public float CurrentAlpha => _currentAlpha;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo Canvas & Image che màn hình
        SetupCanvasUI();

        // Tạo texture 1x1 màu trắng làm fallback OnGUI
        _fadeTexture = new Texture2D(1, 1);
        _fadeTexture.SetPixel(0, 0, Color.white);
        _fadeTexture.Apply();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupCanvasUI()
    {
        if (_fadeCanvas != null && _fadeImage != null) return;

        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform, false);

        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 32767; // Luôn nằm trên cùng mọi UI và HUD

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);

        _fadeImage = imgObj.AddComponent<Image>();
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, _currentAlpha);
        _fadeImage.raycastTarget = false; // Không chặn tương tác chuột khi alpha = 0

        RectTransform rect = _fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private void SetAlpha(float alpha)
    {
        _currentAlpha = Mathf.Clamp01(alpha);

        if (_fadeImage != null)
        {
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, _currentAlpha);
            // Chỉ bắt raycast khi màn hình đang mờ đen để chặn người chơi bấm lung tung
            _fadeImage.raycastTarget = (_currentAlpha > 0.05f);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Đảm bảo canvas vẫn nguyên vẹn sau khi load scene
        if (_fadeCanvas == null || _fadeImage == null)
        {
            SetupCanvasUI();
        }

        // Tự động kích hoạt Fade In nếu:
        // 1. Vừa được nạp qua FadeToScene (màn hình đang đen _currentAlpha > 0)
        // 2. Hoặc người chơi nhấn Play trực tiếp vào Night-1 trong Editor!
        bool isGameplayScene = !scene.name.ToLower().Contains("menu");
        if (_currentAlpha > 0.05f || isGameplayScene)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine(fadeInDuration));
        }
    }

    /// <summary>
    /// Chuyển scene có hiệu ứng Fade Out (tối dần) -> LoadScene -> Fade In (sáng dần khi vào scene mới)
    /// </summary>
    public void FadeToScene(int sceneBuildIndex, float outDuration = -1f, float inDuration = -1f)
    {
        if (_isFading) return;
        float outD = outDuration > 0f ? outDuration : fadeOutDuration;
        float inD = inDuration > 0f ? inDuration : fadeInDuration;
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadRoutine(sceneBuildIndex, null, outD, inD));
    }

    /// <summary>
    /// Chuyển scene theo tên có hiệu ứng Fade Out -> LoadScene -> Fade In
    /// </summary>
    public void FadeToScene(string sceneName, float outDuration = -1f, float inDuration = -1f)
    {
        if (_isFading) return;
        float outD = outDuration > 0f ? outDuration : fadeOutDuration;
        float inD = inDuration > 0f ? inDuration : fadeInDuration;
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadRoutine(-1, sceneName, outD, inD));
    }

    /// <summary>
    /// Hiệu ứng làm tối màn hình (Fade Out: alpha từ current -> 1)
    /// </summary>
    public IEnumerator FadeOut(float duration = -1f, Action onComplete = null)
    {
        float d = duration > 0f ? duration : fadeOutDuration;
        _isFading = true;

        float elapsed = 0f;
        float startAlpha = _currentAlpha;

        while (elapsed < d)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, 1f, elapsed / d));
            yield return null;
        }

        SetAlpha(1f);
        _isFading = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Hiệu ứng làm sáng màn hình (Fade In: alpha từ current -> 0)
    /// </summary>
    public IEnumerator FadeIn(float duration = -1f, Action onComplete = null)
    {
        float d = duration > 0f ? duration : fadeInDuration;
        _isFading = true;

        // Nếu bắt đầu vào scene từ trạng thái chưa đen, đặt đen kịt trước rồi sáng dần
        if (_currentAlpha < 0.95f)
        {
            SetAlpha(1f);
        }

        // Chờ 2 frames và một khoảng nghỉ nhỏ trong bóng tối để scene Night-1 render hoàn tất
        yield return null;
        yield return null;
        if (blackScreenHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackScreenHoldDuration);
        }

        float elapsed = 0f;
        float startAlpha = _currentAlpha;

        while (elapsed < d)
        {
            elapsed += Time.unscaledDeltaTime;
            // Dùng SmoothStep để quá trình mở mắt / sáng dần trở nên điện ảnh, cực kỳ mượt mà
            float t = Mathf.SmoothStep(0f, 1f, elapsed / d);
            SetAlpha(Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        SetAlpha(0f);
        _isFading = false;
        onComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        yield return FadeIn(duration);
    }

    private IEnumerator FadeAndLoadRoutine(int sceneIndex, string sceneName, float outDuration, float inDuration)
    {
        // 1. Tối màn hình
        yield return FadeOut(outDuration);

        // 2. Chờ một chút trong bóng tối
        yield return new WaitForSecondsRealtime(0.15f);

        // 3. Nạp Scene
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }

        // Sau khi nạp scene xong, OnSceneLoaded sẽ tự động chạy FadeIn(inDuration)
    }

    private void OnGUI()
    {
        if (_currentAlpha > 0.001f && (_fadeImage == null || !_fadeImage.gameObject.activeInHierarchy))
        {
            GUI.depth = -32767;
            Color prev = GUI.color;
            GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, _currentAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _fadeTexture, ScaleMode.StretchToFill);
            GUI.color = prev;
        }
    }
}
