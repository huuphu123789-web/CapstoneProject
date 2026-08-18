using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController instance;

    [Header("=== PANEL CHINH ===")]
    public GameObject pauseMenuPanel;

    [Header("=== AM THANH ===")]
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
    public string mainMenuSceneName = "MainMenu";

    public bool isPaused = false;
    private Resolution[] resolutions;

    void Awake() { instance = this; }

    void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
       
        SetupResolutionDropdown();
        SetupQualityDropdown();
        LoadAndApplyAudioSettings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) BackToGame();
            else OpenPauseMenu();
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

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = savedMaster;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = savedSFX;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXChanged);
        }
        if (musicVolumeSlider != null)
        {
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
    }

    /// <summary>Gán vào OnClick() nút "Lưu & Thoát Main Menu"</summary>
    public void SaveAndQuitToMainMenu()
    {
        if (masterVolumeSlider != null) PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (sfxVolumeSlider != null)    PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        if (musicVolumeSlider != null)  PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

        // Lưu Quality nếu có
        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
        else if (legacyQualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", legacyQualityDropdown.value);

        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenPauseMenu()
    {
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
    }

    private void SetPlayerLookEnabled(bool enabled)
    {
        PlayerBodyRotator bRotator = FindObjectOfType<PlayerBodyRotator>();
        if (bRotator != null) bRotator.enabled = enabled;
    }
}
