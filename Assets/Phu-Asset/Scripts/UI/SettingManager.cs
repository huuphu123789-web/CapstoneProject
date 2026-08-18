using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Bảng Cài Đặt (Setting Panel) ở Main Menu.
/// Đồng bộ 100% dữ liệu PlayerPrefs & AudioManager với PauseMenuController trong Gameplay!
/// </summary>
public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    [Header("=== THANH KÉO ÂM THANH (SLIDERS) ===")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("=== NÚT MUTE MASTER (TÙY CHỌN) ===")]
    [SerializeField] private Image masterMuteImage;
    [SerializeField] private Sprite masterSoundOnSprite;
    [SerializeField] private Sprite masterSoundOffSprite;

    [Header("=== NÚT MUTE SFX (TÙY CHỌN) ===")]
    [SerializeField] private Image sfxMuteImage;
    [SerializeField] private Sprite sfxSoundOnSprite;
    [SerializeField] private Sprite sfxSoundOffSprite;

    [Header("=== DROPDOWN ĐỘ PHÂN GIẢI (TMP hoặc Dropdown thường) ===")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Dropdown legacyResolutionDropdown;

    [Header("=== DROPDOWN CHẤT LƯỢNG ĐỒ HỌA (TMP hoặc Dropdown thường) ===")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Dropdown legacyQualityDropdown;

    private Resolution[] resolutions;
    private bool isMasterMuted = false;
    private bool isSFXMuted = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SetupAudioUI();
        SetupResolutionDropdown();
        SetupQualityDropdown();
    }

    // ================= 1. XỬ LÝ ÂM THANH =================
    private void SetupAudioUI()
    {
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedSFX    = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float savedMusic  = PlayerPrefs.GetFloat("MusicVolume", 1f);

        isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        isSFXMuted    = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        if (masterSlider != null)
        {
            masterSlider.value = savedMaster;
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.value = savedMusic;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        UpdateMasterMuteUI();
        UpdateSFXMuteUI();

        // Áp dụng âm thanh vào AudioManager ngay lập tức
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(isMasterMuted ? 0f : savedMaster);
            AudioManager.instance.SetSFXVolume(isSFXMuted ? 0f : savedSFX);
            AudioManager.instance.SetMusicVolume(savedMusic);
        }
    }

    public void OnMasterSliderChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (!isMasterMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(value);
        }
    }

    public void OnSFXSliderChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (!isSFXMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
    }

    public void OnMusicSliderChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(value);
        }
    }

    public void ToggleMuteMaster()
    {
        isMasterMuted = !isMasterMuted;
        PlayerPrefs.SetInt("MasterMuted", isMasterMuted ? 1 : 0);

        float currentVol = masterSlider != null ? masterSlider.value : 1f;
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(isMasterMuted ? 0f : currentVol);
        }

        UpdateMasterMuteUI();
    }

    public void ToggleMuteSFX()
    {
        isSFXMuted = !isSFXMuted;
        PlayerPrefs.SetInt("SFXMuted", isSFXMuted ? 1 : 0);

        float currentVol = sfxSlider != null ? sfxSlider.value : 1f;
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(isSFXMuted ? 0f : currentVol);
        }

        UpdateSFXMuteUI();
    }

    private void UpdateMasterMuteUI()
    {
        if (masterMuteImage != null && masterSoundOnSprite != null && masterSoundOffSprite != null)
        {
            masterMuteImage.sprite = isMasterMuted ? masterSoundOffSprite : masterSoundOnSprite;
        }
    }

    private void UpdateSFXMuteUI()
    {
        if (sfxMuteImage != null && sfxSoundOnSprite != null && sfxSoundOffSprite != null)
        {
            sfxMuteImage.sprite = isSFXMuted ? sfxSoundOffSprite : sfxSoundOnSprite;
        }
    }

    // ================= 2. XỬ LÝ ĐỘ PHÂN GIẢI (RESOLUTION) =================
    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (legacyResolutionDropdown != null)
        {
            legacyResolutionDropdown.ClearOptions();
            legacyResolutionDropdown.AddOptions(options);
            legacyResolutionDropdown.value = currentResolutionIndex;
            legacyResolutionDropdown.RefreshShownValue();
            legacyResolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    // ================= 3. XỬ LÝ CHẤT LƯỢNG ĐỒ HỌA (QUALITY) =================
    private void SetupQualityDropdown()
    {
        List<string> qualityNames = new List<string>(QualitySettings.names);
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(savedQuality);

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(qualityNames);
            qualityDropdown.value = savedQuality;
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        if (legacyQualityDropdown != null)
        {
            legacyQualityDropdown.ClearOptions();
            legacyQualityDropdown.AddOptions(qualityNames);
            legacyQualityDropdown.value = savedQuality;
            legacyQualityDropdown.RefreshShownValue();
            legacyQualityDropdown.onValueChanged.AddListener(SetQuality);
        }
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }
}