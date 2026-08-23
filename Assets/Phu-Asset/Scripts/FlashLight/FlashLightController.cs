using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    [SerializeField] private AudioClip onClip;
    private AudioSource localAudioSource;

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
        // Mặc định tắt đèn pin khi vừa sinh ra
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }

        // Đồng bộ đèn pin với HUD Manager
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.flashlightLight = flashlight;
            PlayerHUDManager.instance.UpdateFlashlightUI();
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

        // Nhấn F để bật/tắt đèn pin
        if (Input.GetKeyDown(KeyCode.F))
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

            // Cập nhật Icon Đèn Pin trên HUD
            if (PlayerHUDManager.instance != null)
            {
                PlayerHUDManager.instance.UpdateFlashlightUI();
            }
        }
    }
}