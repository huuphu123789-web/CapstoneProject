using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý đèn pin của người chơi.
/// Chỉ có thể bật/tắt bằng phím F sau khi người chơi đã nhặt được đèn pin (hasFlashlight = true).
/// Đã tối ưu hóa thông số chiếu sáng cho URP và sương mù Night 1 để chống hiện tượng chớp tắt, lúc ẩn lúc hiện.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("=== TRẠNG THÁI ĐÈN PIN ===")]
    [Tooltip("Người chơi đã sở hữu đèn pin chưa? (Nếu false thì bấm F không có tác dụng)")]
    public bool hasFlashlight = false;

    [Header("=== CẤU HÌNH PHẦN CỨNG ===")]
    [SerializeField] private Light flashlight;
    [SerializeField] private AudioClip onClip;
    private AudioSource localAudioSource;

    [Header("=== TỐI ƯU ÁNH SÁNG & SƯƠNG MÙ (CHỐNG CHỚP TẮT) ===")]
    [Tooltip("Tự động cấu hình ánh sáng chuẩn giúp đèn xuyên qua sương mù và không bị URP cúp/ẩn đèn")]
    [SerializeField] private bool autoApplyOptimalSettings = true;

    [Tooltip("Cường độ ánh sáng đèn pin (chuẩn 35 - 50 cho màn đêm có sương mù)")]
    [Range(5f, 100f)]
    public float intensity = 35f;

    [Tooltip("Tầm chiếu xa (m) - Khuyên dùng 35 - 45m để xuyên sương mù Night-1 dễ dàng")]
    [Range(10f, 80f)]
    public float range = 35f;

    [Tooltip("Góc tỏa rộng của chùm sáng")]
    [Range(20f, 90f)]
    public float spotAngle = 65f;

    [Tooltip("Góc tâm sáng (Inner Spot Angle)")]
    [Range(10f, 60f)]
    public float innerSpotAngle = 35f;

    [Tooltip("Màu ánh sáng đèn pin (màu vàng ấm rõ nét)")]
    public Color lightColor = new Color(1f, 0.88f, 0.35f, 1f);

    [Tooltip("Đổ bóng (None giúp mượt mà nhất và triệt tiêu 100% hiện tượng chớp tắt khi nhiều đèn xung quanh)")]
    public LightShadows shadows = LightShadows.None;

    public Light FlashlightLight => GetFlashlightLight();

    /// <summary>
    /// Tìm chính xác nguồn sáng của Đèn Pin trên toàn bộ cấu trúc Player (kể cả nhánh CinemachineCamera)
    /// </summary>
    public Light GetFlashlightLight()
    {
        if (flashlight != null)
        {
            ApplyOptimalLightSettings(flashlight);
            return flashlight;
        }

        // 1. Tìm trên chính GameObject này hoặc các con
        flashlight = GetComponentInChildren<Light>(true);
        if (flashlight != null)
        {
            ApplyOptimalLightSettings(flashlight);
            return flashlight;
        }

        // 2. Tìm trên Transform.root của Player (bao gồm nhánh CinemachineCamera)
        if (transform.root != null)
        {
            Light[] lights = transform.root.GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                if (l.gameObject.name.ToLower().Contains("flashlight") || 
                    (l.transform.parent != null && l.transform.parent.name.ToLower().Contains("flashlight")))
                {
                    flashlight = l;
                    ApplyOptimalLightSettings(flashlight);
                    return flashlight;
                }
            }
            if (lights.Length > 0)
            {
                flashlight = lights[0];
                ApplyOptimalLightSettings(flashlight);
                return flashlight;
            }
        }

        // 3. Tìm trên toàn bộ GameObject có tag "Player"
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            Light[] lights = p.GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                if (l.gameObject.name.ToLower().Contains("flashlight") || 
                    (l.transform.parent != null && l.transform.parent.name.ToLower().Contains("flashlight")))
                {
                    flashlight = l;
                    ApplyOptimalLightSettings(flashlight);
                    return flashlight;
                }
            }
            if (lights.Length > 0)
            {
                flashlight = lights[0];
                ApplyOptimalLightSettings(flashlight);
                return flashlight;
            }
        }

        // 4. Tìm kiếm bất kỳ Light nào có tên Flashlight trong Scene
        Light[] allSceneLights = FindObjectsOfType<Light>(true);
        foreach (var l in allSceneLights)
        {
            if (l.gameObject.name.ToLower().Contains("flashlight"))
            {
                flashlight = l;
                ApplyOptimalLightSettings(flashlight);
                return flashlight;
            }
        }

        return null;
    }

    /// <summary>
    /// Áp dụng các thông số ánh sáng tối ưu để đèn không bị cúp do giới hạn đèn của URP
    /// và chiếu sáng rõ nét trong sương mù của Night 1 mà không cần tăng intensity lên 500.
    /// </summary>
    public void ApplyOptimalLightSettings(Light targetLight)
    {
        if (targetLight == null || !autoApplyOptimalSettings) return;

        targetLight.type = LightType.Spot;
        // Đặt ForcePixel để Unity URP luôn render nguồn sáng này, không bao giờ bị cull/ẩn đi
        targetLight.renderMode = LightRenderMode.ForcePixel;
        targetLight.intensity = intensity;
        targetLight.range = range;
        targetLight.spotAngle = spotAngle;
        targetLight.innerSpotAngle = innerSpotAngle;
        targetLight.color = lightColor;
        targetLight.shadows = shadows;

        // Bỏ qua lớp Player để không tự che bóng vào thân nhân vật
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            targetLight.cullingMask &= ~(1 << playerLayer);
        }
    }

    void Awake()
    {
        // Tối đa hóa giới hạn đèn pixel để đèn pin không bao giờ làm tắt các nguồn sáng khác
        QualitySettings.pixelLightCount = 16;

        GetFlashlightLight();

        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null)
            localAudioSource = gameObject.AddComponent<AudioSource>();

        // Mặc định luôn tắt đèn pin khi mới vào game cho đến khi người chơi nhặt từ tủ
        bool isCollected = (TaskManager.instance != null && TaskManager.instance.hasFlashlight);
        hasFlashlight = isCollected;
        if (flashlight != null)
        {
            flashlight.enabled = isCollected;
        }
    }

    void Start()
    {
        GetFlashlightLight();

        // Kiểm tra lại một lần nữa ở Start để bảo đảm nếu chưa nhặt thì đèn luôn tắt
        bool isCollected = (TaskManager.instance != null && TaskManager.instance.hasFlashlight);
        hasFlashlight = isCollected;
        if (flashlight != null)
        {
            flashlight.enabled = isCollected;
        }

        // Đảm bảo đèn phòng hầm Cellar không bị cúp khi bật đèn pin
        EnsureCellarLightsNeverCulled();

        // Đồng bộ đèn pin với HUD Manager
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.flashlightLight = flashlight;
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }
    }

    /// <summary>
    /// Đảm bảo các nguồn sáng trong phòng hầm (Cellar) luôn được ưu tiên (ForcePixel)
    /// để Unity URP không bao giờ cúp đèn phòng hầm khi người chơi bật đèn pin.
    /// </summary>
    public void EnsureCellarLightsNeverCulled()
    {
        GameObject cellarObj = GameObject.Find("Cellar");
        if (cellarObj != null)
        {
            Light[] cellarLights = cellarObj.GetComponentsInChildren<Light>(true);
            foreach (var l in cellarLights)
            {
                if (l != null && l.renderMode != LightRenderMode.ForcePixel)
                {
                    l.renderMode = LightRenderMode.ForcePixel;
                }
            }
        }
    }

    void Update()
    {
        // Không cho bật/tắt đèn khi đang Pause
        if ((PauseMenuController.instance != null && PauseMenuController.instance.isPaused) ||
            (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused))
            return;

        // Tự động đồng bộ trạng thái sở hữu từ TaskManager
        if (!hasFlashlight && TaskManager.instance != null && TaskManager.instance.hasFlashlight)
        {
            hasFlashlight = true;
        }

        // Nhấn F để bật/tắt đèn pin (hỗ trợ cả Old Input Manager và New Input System)
        bool fPressed = Input.GetKeyDown(KeyCode.F);
        if (!fPressed)
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.fKey.wasPressedThisFrame)
                    fPressed = true;
            }
            catch { }
        }

        if (fPressed)
        {
            if (hasFlashlight || (TaskManager.instance != null && TaskManager.instance.hasFlashlight))
            {
                hasFlashlight = true;
                ToggleFlashlight();
            }
            else
            {
                Debug.Log("[FlashlightController] Chưa có đèn pin! Hãy lại tủ để nhặt hoặc nhấn phím G.");
            }
        }
    }

    /// <summary>
    /// Mở khóa và trang bị đèn pin khi người chơi nhặt từ tủ hoặc nhấn phím G
    /// </summary>
    public void EquipFlashlight(bool turnOnImmediately = true)
    {
        hasFlashlight = true;
        Light flLight = GetFlashlightLight();

        if (flLight != null)
        {
            ApplyOptimalLightSettings(flLight);
            flLight.gameObject.SetActive(true);
            if (flLight.transform.parent != null)
            {
                flLight.transform.parent.gameObject.SetActive(true);
            }
            flLight.enabled = turnOnImmediately;
        }

        Debug.Log($"[FlashlightController] Đã trang bị Đèn Pin! (Đèn đang: {(flLight != null && flLight.enabled ? "BẬT" : "TẮT")})");

        // Cập nhật giao diện HUD
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.flashlightLight = flLight;
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }
    }

    /// <summary>
    /// Bật hoặc tắt đèn pin và phát âm thanh công tắc
    /// </summary>
    public void ToggleFlashlight()
    {
        Light flLight = GetFlashlightLight();

        if (flLight != null)
        {
            ApplyOptimalLightSettings(flLight);
            EnsureCellarLightsNeverCulled();
            flLight.gameObject.SetActive(true);
            if (flLight.transform.parent != null)
            {
                flLight.transform.parent.gameObject.SetActive(true);
            }

            flLight.enabled = !flLight.enabled;
            Debug.Log($"[FlashlightController] Phím F: Đèn pin {(flLight.enabled ? "BẬT (ON)" : "TẮT (OFF)")}");
        }
        else
        {
            Debug.LogWarning("[FlashlightController] Không tìm thấy Light component trên Đèn Pin!");
        }

        // Phát âm thanh bật/tắt tuân thủ cài đặt âm thanh SFX
        if (onClip != null)
        {
            bool isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
            bool isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);

            // Chỉ phát nếu không bị Mute và Volume > 0
            if (!isSFXMuted && !isMasterMuted && sfxVol > 0f && masterVol > 0f)
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlaySFX(onClip);
                }
                else if (localAudioSource != null)
                {
                    localAudioSource.volume = sfxVol * masterVol;
                    localAudioSource.PlayOneShot(onClip);
                }
            }
        }

        // Cập nhật Icon Đèn Pin & Hướng dẫn phím trên HUD
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }
    }
}