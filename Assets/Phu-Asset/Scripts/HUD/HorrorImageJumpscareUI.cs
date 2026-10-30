using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị tấm hình kinh dị Jumpscare toàn màn hình.
/// Tự động sinh ra Canvas Overlay nếu chưa có sẵn trong Scene,
/// hiển thị tấm hình kinh dị, rung lắc màn hình và phát âm thanh thét kinh dị.
/// </summary>
public class HorrorImageJumpscareUI : MonoBehaviour
{
    private static HorrorImageJumpscareUI instance;

    [Header("=== THÔNG SỐ JUMPSCARE MẶC ĐỊNH ===")]
    public Sprite defaultScareSprite;
    public AudioClip defaultScareSound;

    private Canvas scareCanvas;
    private Image scareImage;
    private Image redFlashOverlay;
    private AudioSource audioSource;
    private Coroutine activeScareCoroutine;

    public static HorrorImageJumpscareUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HorrorImageJumpscareUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("HorrorImageJumpscareManager");
                    instance = go.AddComponent<HorrorImageJumpscareUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureUIBuilt();
    }

    private void EnsureUIBuilt()
    {
        if (scareCanvas != null) return;

        // 1. Tạo Canvas Overlay nằm trên cùng
        GameObject canvasObj = new GameObject("HorrorJumpscareCanvas");
        canvasObj.transform.SetParent(transform);
        scareCanvas = canvasObj.AddComponent<Canvas>();
        scareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scareCanvas.sortingOrder = 9999; // Luôn nằm trên tất cả UI khác

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 2. Tạo lớp viền đỏ chớp nháy (Red Flash)
        GameObject redObj = new GameObject("RedFlashOverlay");
        redObj.transform.SetParent(canvasObj.transform, false);
        redFlashOverlay = redObj.AddComponent<Image>();
        redFlashOverlay.color = new Color(0.8f, 0f, 0f, 0f);
        RectTransform redRect = redObj.GetComponent<RectTransform>();
        redRect.anchorMin = Vector2.zero;
        redRect.anchorMax = Vector2.one;
        redRect.sizeDelta = Vector2.zero;

        // 3. Tạo Image hiển thị tấm hình kinh dị
        GameObject imgObj = new GameObject("JumpscareImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        scareImage = imgObj.AddComponent<Image>();
        scareImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.sizeDelta = Vector2.zero;

        // 4. AudioSource phát tiếng thét
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D Sound toàn màn hình

        // Ẩn ban đầu
        canvasObj.SetActive(false);
    }

    /// <summary>
    /// Kích hoạt Jumpscare hình ảnh toàn màn hình
    /// </summary>
    public static void Show(Sprite sprite = null, AudioClip sound = null, float duration = 1.8f, System.Action onFinish = null)
    {
        Instance.TriggerScareInternal(sprite, sound, duration, onFinish);
    }

    public void TriggerScareInternal(Sprite sprite, AudioClip sound, float duration, System.Action onFinish)
    {
        EnsureUIBuilt();

        if (activeScareCoroutine != null)
        {
            StopCoroutine(activeScareCoroutine);
        }

        activeScareCoroutine = StartCoroutine(ScareRoutine(sprite, sound, duration, onFinish));
    }

    private IEnumerator ScareRoutine(Sprite sprite, AudioClip sound, float duration, System.Action onFinish)
    {
        // 1. Gán Sprite kinh dị
        Sprite targetSprite = sprite != null ? sprite : defaultScareSprite;
        if (targetSprite == null)
        {
#if UNITY_EDITOR
            targetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phu-Asset/Textures/Jumpscare/Horror_Jumpscare_Face.png");
#endif
        }

        if (targetSprite != null)
        {
            scareImage.sprite = targetSprite;
        }

        // 2. Phát âm thanh tiếng thét jumpscare
        AudioClip targetSound = sound != null ? sound : defaultScareSound;
        if (targetSound == null)
        {
#if UNITY_EDITOR
            targetSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/NPCVoice/Jumpscare_Scream_Horror.wav");
            if (targetSound == null)
            {
                targetSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Impacts & Hits/JumpScare.wav");
            }
#endif
        }

        if (targetSound != null)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(targetSound);
            else if (audioSource != null) audioSource.PlayOneShot(targetSound, 1.0f);
        }

        // 3. Rung lắc camera dữ dội
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(duration * 0.8f, 0.7f);
        }

        // 4. Bật Canvas hiển thị
        scareCanvas.gameObject.SetActive(true);
        scareImage.color = Color.white;

        RectTransform imgRect = scareImage.rectTransform;
        Vector2 origPos = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Rung lắc tấm ảnh giật cục trên màn hình
            float shakeX = Random.Range(-25f, 25f);
            float shakeY = Random.Range(-25f, 25f);
            imgRect.anchoredPosition = origPos + new Vector2(shakeX, shakeY);

            // Chớp nháy viền đỏ rùng rợn
            float flash = Mathf.PingPong(elapsed * 12f, 1f) * 0.45f;
            redFlashOverlay.color = new Color(0.85f, 0f, 0f, flash);

            yield return null;
        }

        // 5. Kết thúc: Ẩn Canvas
        scareCanvas.gameObject.SetActive(false);
        activeScareCoroutine = null;

        onFinish?.Invoke();
    }
}
