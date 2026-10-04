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

    [Header("=== TEXT HIỂN THỊ ÂM LƯỢNG (TÙY CHỌN) ===")]
    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private TextMeshProUGUI musicValueText;

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

    [Header("=== CHẾ ĐỘ MÀN HÌNH (WINDOW MODE) ===")]
    [SerializeField] private TMP_Dropdown windowModeDropdown;

    [Header("=== ĐỒNG BỘ DỌC (V-SYNC) ===")]
    [SerializeField] private Toggle vsyncToggle;

    [Header("=== ĐỘ SÁNG MÀN HÌNH (BRIGHTNESS) ===")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessValueText;
    [SerializeField] private Image brightnessOverlay; // Panel đen phủ màn hình điều chỉnh độ sáng (tùy chọn)

    [Header("=== GÓC NHÌN CAMERA (FIELD OF VIEW) ===")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TextMeshProUGUI fovValueText;

    [Header("=== GIỚI HẠN KHUNG HÌNH (FPS LIMIT) ===")]
    [SerializeField] private TMP_Dropdown fpsLimitDropdown;

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
        SetupWindowModeDropdown();
        SetupVSyncToggle();
        SetupBrightnessSlider();
        SetupFOVSlider();
        SetupFPSLimitDropdown();
    }

    void OnEnable()
    {
        SetupAudioUI();
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
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
            masterSlider.value = savedMaster;
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            musicSlider.value = savedMusic;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        UpdateMasterMuteUI();
        UpdateSFXMuteUI();

        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(savedMaster * 100) + "%";
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(savedSFX * 100) + "%";
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(savedMusic * 100) + "%";

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
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(value * 100) + "%";
        if (!isMasterMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(value);
        }
    }

    public void OnSFXSliderChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(value * 100) + "%";
        if (!isSFXMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
    }

    public void OnMusicSliderChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(value * 100) + "%";
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

    // ================= 4. XỬ LÝ CHẾ ĐỘ MÀN HÌNH (WINDOW MODE) =================
    private void SetupWindowModeDropdown()
    {
        if (windowModeDropdown == null) return;

        List<string> modes = new List<string>() { "Toàn màn hình (Fullscreen)", "Không viền (Borderless)", "Cửa sổ (Windowed)" };
        windowModeDropdown.ClearOptions();
        windowModeDropdown.AddOptions(modes);

        int savedMode = PlayerPrefs.GetInt("WindowMode", 0);
        windowModeDropdown.value = savedMode;
        windowModeDropdown.RefreshShownValue();
        windowModeDropdown.onValueChanged.AddListener(SetWindowMode);
        SetWindowMode(savedMode);
    }

    public void SetWindowMode(int index)
    {
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (index == 0) mode = FullScreenMode.ExclusiveFullScreen;
        else if (index == 1) mode = FullScreenMode.FullScreenWindow;
        else if (index == 2) mode = FullScreenMode.Windowed;

        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("WindowMode", index);
    }

    // ================= 5. XỬ LÝ V-SYNC =================
    private void SetupVSyncToggle()
    {
        if (vsyncToggle == null) return;

        bool savedVSync = PlayerPrefs.GetInt("VSync", 1) == 1;
        vsyncToggle.isOn = savedVSync;
        vsyncToggle.onValueChanged.AddListener(SetVSync);
        SetVSync(savedVSync);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    // ================= 6. XỬ LÝ ĐỘ SÁNG (BRIGHTNESS) =================
    private void SetupBrightnessSlider()
    {
        if (brightnessSlider == null) return;

        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
        brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        brightnessSlider.value = savedBrightness;
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        OnBrightnessChanged(savedBrightness);
    }

    public void OnBrightnessChanged(float val)
    {
        PlayerPrefs.SetFloat("Brightness", val);
        if (brightnessValueText != null)
        {
            brightnessValueText.text = val.ToString("0.0");
        }

        if (brightnessOverlay != null)
        {
            float alpha = Mathf.Abs(val - 0.5f) * 1.5f;
            Color c = val < 0.5f ? Color.black : Color.white;
            c.a = Mathf.Clamp(alpha, 0f, 0.7f);
            brightnessOverlay.color = c;
        }

        RenderSettings.ambientLight = Color.white * val;
    }

    // ================= 7. XỬ LÝ GÓC NHÌN (FIELD OF VIEW) =================
    private void SetupFOVSlider()
    {
        if (fovSlider == null) return;

        float savedFOV = PlayerPrefs.GetFloat("FOV", 75f);
        fovSlider.onValueChanged.RemoveListener(OnFOVChanged);
        fovSlider.value = savedFOV;
        fovSlider.onValueChanged.AddListener(OnFOVChanged);
        OnFOVChanged(savedFOV);
    }

    public void OnFOVChanged(float val)
    {
        PlayerPrefs.SetFloat("FOV", val);
        if (fovValueText != null)
        {
            fovValueText.text = Mathf.RoundToInt(val).ToString();
        }

        if (Camera.main != null)
        {
            Camera.main.fieldOfView = val;
        }
    }

    // ================= 8. XỬ LÝ GIỚI HẠN FPS =================
    private void SetupFPSLimitDropdown()
    {
        if (fpsLimitDropdown == null) return;

        List<string> fpsOptions = new List<string>() { "Không giới hạn", "60 FPS", "120 FPS", "144 FPS" };
        fpsLimitDropdown.ClearOptions();
        fpsLimitDropdown.AddOptions(fpsOptions);

        int savedIndex = PlayerPrefs.GetInt("FPSLimitIndex", 0);
        fpsLimitDropdown.value = savedIndex;
        fpsLimitDropdown.RefreshShownValue();
        fpsLimitDropdown.onValueChanged.AddListener(SetFPSLimit);
        SetFPSLimit(savedIndex);
    }

    public void SetFPSLimit(int index)
    {
        int targetFPS = -1;
        switch (index)
        {
            case 1: targetFPS = 60; break;
            case 2: targetFPS = 120; break;
            case 3: targetFPS = 144; break;
            default: targetFPS = -1; break;
        }
        Application.targetFrameRate = targetFPS;
        PlayerPrefs.SetInt("FPSLimitIndex", index);
    }
}