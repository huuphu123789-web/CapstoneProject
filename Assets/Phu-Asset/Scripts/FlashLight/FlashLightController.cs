using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý đèn pin của người chơi.
/// Chỉ có thể bật/tắt bằng phím F sau khi người chơi đã nhặt được đèn pin (hasFlashlight = true).
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

    public Light FlashlightLight => flashlight;

    void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();

        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null)
            localAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Mặc định tắt nguồn sáng khi vừa sinh ra
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }

        // Đồng bộ đèn pin với HUD Manager
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.flashlightLight = flashlight;
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }
    }

    void Update()
    {
        // Vô hiệu hóa phím F hoàn toàn khi đang ở MainMenu
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu" || currentScene.ToLower().Contains("menu"))
            return;

        // Không cho bật/tắt đèn khi đang Pause
        if ((PauseMenuController.instance != null && PauseMenuController.instance.isPaused) ||
            (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused))
            return;

        // Nếu chưa nhặt được đèn pin từ tủ trong cellar thì không cho bật
        if (!hasFlashlight)
            return;

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
            ToggleFlashlight();
        }
    }

    /// <summary>
    /// Mở khóa và trang bị đèn pin khi người chơi nhặt từ tủ
    /// </summary>
    public void EquipFlashlight(bool turnOnImmediately = false)
    {
        hasFlashlight = true;
        Debug.Log("[FlashlightController] Người chơi đã nhặt được Đèn Pin!");

        if (flashlight != null)
        {
            flashlight.enabled = turnOnImmediately;
        }

        // Cập nhật giao diện HUD
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.flashlightLight = flashlight;
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }
    }

    /// <summary>
    /// Bật hoặc tắt đèn pin và phát âm thanh công tắc
    /// </summary>
    public void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
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