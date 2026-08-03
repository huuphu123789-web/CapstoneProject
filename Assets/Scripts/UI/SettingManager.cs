using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingManager : MonoBehaviour
{
   

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;  // Kéo ResolutionDropdown vào đây

    [Header("Graphics Quality")]
    [SerializeField] private TMP_Dropdown qualityDropdown;     // Kéo QualityDropdown vào đây

    private Resolution[] resolutions;

    void Start()
    {
        // ===== SETUP RESOLUTION DROPDOWN =====
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Hiển thị dạng "1920 x 1080 @ 60Hz"
            string option = resolutions[i].width + " x " + resolutions[i].height
                            + " @ " + resolutions[i].refreshRateRatio.value.ToString("0") + "Hz";
            options.Add(option);

            // Tìm resolution hiện tại
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // ===== SETUP QUALITY DROPDOWN =====
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        
    }

    // ============ CALLBACK METHODS ============

    /// <summary>
    /// Gọi khi kéo Volume Slider
    /// </summary>

    /// <summary>
    /// Gọi khi chọn Resolution từ Dropdown
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    /// <summary>
    /// Gọi khi chọn Graphics Quality từ Dropdown
    /// </summary>
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
}