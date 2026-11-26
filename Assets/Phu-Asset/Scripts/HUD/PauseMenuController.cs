using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController instance;

    [Header("=== PANEL CHINH ===")]
    [Tooltip("Kéo SettingPanel hoặc PauseMenuPanel vào đây")]
    public GameObject pauseMenuPanel;

    [Header("=== AM THANH (Tuy chon neu khong dung SettingManager) ===")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("=== RESOLUTION & QUALITY (TMP Dropdown) ===")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;

    [Header("=== HOẶC DROPDOWN THƯỜNG (Legacy Dropdown) ===")]
    public Dropdown legacyResolutionDropdown;
    public Dropdown legacyQualityDropdown;

    [Header("=== TEN SCENE ===")]
    [Tooltip("Ten scene Main Menu chinh xac")]
    public string mainMenuSceneName = "NewMainMenu";

    public bool isPaused = false;
    private Resolution[] resolutions;

    void Awake() 
    { 
        if (instance != null && instance != this)
        {
            // Tránh 2 PauseMenuController trùng nhau cùng xử lý phím ESC
            enabled = false;
            return;
        }
        instance = this; 
    }

    void Start()
    {
        EnsureEventSystem();

        // Tu dong tim SettingPanel neu de trong
        if (pauseMenuPanel == null)
        {
            SettingManager sm = FindAnyObjectByType<SettingManager>(FindObjectsInactive.Include);
            if (sm != null) pauseMenuPanel = sm.gameObject;
        }

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
       
        SetupResolutionDropdown();
        SetupQualityDropdown();
        LoadAndApplyAudioSettings();
    }

    void Update()
    {
        // Đang bấm chọn phím mới trong tab Controls -> không đóng Pause Menu khi bấm ESC
        if (KeybindManager.IsRebinding) return;

        bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escapePressed = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!escapePressed && Input.GetKeyDown(KeyCode.Escape))
        {
            escapePressed = true;
        }
#endif
        if (escapePressed)
        {
            if (isPaused) BackToGame();
            else OpenPauseMenu();
        }
    }

    /// <summary>
    /// Đảm bảo Scene luôn có EventSystem chuẩn (tương thích Input System) để bấm được mọi nút/slider trong Pause Menu
    /// </summary>
    public static void EnsureEventSystem()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            es = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        }

        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
            Debug.Log("[PauseMenuController] Đã tự động tạo EventSystem cho Scene để tương tác UI!");
        }
        else
        {
            if (!es.gameObject.activeInHierarchy) es.gameObject.SetActive(true);
            es.enabled = true;
#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                StandaloneInputModule oldMod = es.GetComponent<StandaloneInputModule>();
                if (oldMod != null) oldMod.enabled = false;
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Hiển thị gọn gàng: "1920 x 1080"
            string opt = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(opt);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        // Hỗ trợ TMP_Dropdown
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // Hỗ trợ Legacy UI Dropdown
        if (legacyResolutionDropdown != null)
        {
            legacyResolutionDropdown.ClearOptions();
            legacyResolutionDropdown.AddOptions(options);
            legacyResolutionDropdown.value = currentIndex;
            legacyResolutionDropdown.RefreshShownValue();
            legacyResolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
    }

    private void SetupQualityDropdown()
    {
        List<string> qualityNames = new List<string>(QualitySettings.names);
        int currentQuality = QualitySettings.GetQualityLevel();

        // Hỗ trợ TMP_Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(qualityNames);
            qualityDropdown.value = currentQuality;
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        // Hỗ trợ Legacy UI Dropdown
        if (legacyQualityDropdown != null)
        {
            legacyQualityDropdown.ClearOptions();
            legacyQualityDropdown.AddOptions(qualityNames);
            legacyQualityDropdown.value = currentQuality;
            legacyQualityDropdown.RefreshShownValue();
            legacyQualityDropdown.onValueChanged.AddListener(SetQuality);
        }
    }

    private void LoadAndApplyAudioSettings()
    {
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedSFX    = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float savedMusic  = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (savedMaster <= 0.01f) savedMaster = 1f;
        if (savedSFX <= 0.01f) savedSFX = 1f;
        if (savedMusic <= 0.01f) savedMusic = 1f;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterChanged);
            masterVolumeSlider.value = savedMaster;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXChanged);
            sfxVolumeSlider.value = savedSFX;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXChanged);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicVolumeSlider.value = savedMusic;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(savedMaster);
            AudioManager.instance.SetSFXVolume(savedSFX);
            AudioManager.instance.SetMusicVolume(savedMusic);
        }
    }

    public void OnMasterChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (AudioManager.instance != null) AudioManager.instance.SetMasterVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (AudioManager.instance != null) AudioManager.instance.SetSFXVolume(value);
    }

    public void OnMusicChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (AudioManager.instance != null) AudioManager.instance.SetMusicVolume(value);
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || index >= resolutions.Length) return;
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("QualityLevel", index);
    }

    /// <summary>Gán vào OnClick() nút "Quay Lại Game"</summary>
    public void BackToGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerLookEnabled(true);
        // Hiện lại HUD gameplay
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.isPaused = false;
            PlayerHUDManager.instance.ShowHUD(true);
        }
        if (TaskManager.instance != null)
        {
            TaskManager.instance.SetTaskUIVisible(true);
        }
    }

    /// <summary>Gán vào OnClick() nút "Lưu & Thoát Main Menu"</summary>
    public void SaveAndQuitToMainMenu()
    {
        if (masterVolumeSlider != null && masterVolumeSlider.value > 0.01f) PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (sfxVolumeSlider != null && sfxVolumeSlider.value > 0.01f)    PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        if (musicVolumeSlider != null && musicVolumeSlider.value > 0.01f)  PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

        // Lưu Quality nếu có
        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
        else if (legacyQualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", legacyQualityDropdown.value);

        PlayerPrefs.Save();

        if (TaskManager.instance != null)
        {
            TaskManager.instance.SetTaskUIVisible(false);
        }

        Time.timeScale = 1f;

        // Xóa player DontDestroyOnLoad nếu có để không bị đè menu
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.scene.name == "DontDestroyOnLoad") Destroy(player);

        string targetScene = GetValidMainMenuScene();
        SceneManager.LoadScene(targetScene);
    }

    private string GetValidMainMenuScene()
    {
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName)) return mainMenuSceneName;
        if (Application.CanStreamedLevelBeLoaded("NewMainMenu")) return "NewMainMenu";
        if (Application.CanStreamedLevelBeLoaded("P-MainMenu")) return "P-MainMenu";
        if (Application.CanStreamedLevelBeLoaded("MainMenu")) return "MainMenu";
        return mainMenuSceneName;
    }

    public void OpenPauseMenu()
    {
        EnsureEventSystem();
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerLookEnabled(false);
        // Ẩn HUD gameplay
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.isPaused = true;
            PlayerHUDManager.instance.ShowHUD(false);
        }
        if (TaskManager.instance != null)
        {
            TaskManager.instance.SetTaskUIVisible(false);
        }
    }

    private void SetPlayerLookEnabled(bool enabled)
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = enabled;

        PlayerBodyRotator bRotator = FindAnyObjectByType<PlayerBodyRotator>();
        if (bRotator != null) bRotator.enabled = enabled;
    }
}
