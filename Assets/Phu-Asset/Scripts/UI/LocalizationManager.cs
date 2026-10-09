using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight Localization System for CapStone:
/// Manages language switching (English / Vietnamese) across all scenes.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event Action<string> OnLanguageChanged;

    public string CurrentLanguage { get; private set; } = "English";

    private Dictionary<string, (string en, string vi)> dictionary = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
    {
        // Main Menu & Navigation
        { "Play", ("Play", "Chơi") },
        { "Continue", ("Continue", "Tiếp Tục") },
        { "Settings", ("Settings", "Cài Đặt") },
        { "Exit", ("Exit", "Thoát") },
        { "Back", ("Back", "Quay Lại") },

        // Setting Tabs
        { "Gameplay", ("Gameplay", "Lối Chơi") },
        { "Controls", ("Controls", "Điều Khiển") },
        { "Audio", ("Audio", "Âm Thanh") },
        { "Visuals", ("Visuals", "Hình Ảnh") },

        // Audio
        { "Master Volume", ("Master Volume", "Âm Lượng Tổng") },
        { "SFX", ("SFX", "Hiệu Ứng") },
        { "Music", ("Music", "Nhạc Nền") },

        // Visuals
        { "Resolution", ("Resolution", "Độ Phân Giải") },
        { "Quality", ("Quality", "Chất Lượng") },
        { "Window Mode", ("Window Mode", "Chế Độ Màn Hình") },
        { "Fullscreen", ("Fullscreen", "Toàn Màn Hình") },
        { "Borderless", ("Borderless", "Không Viền") },
        { "Windowed", ("Windowed", "Cửa Sổ") },
        { "V-Sync", ("V-Sync", "Đồng Bộ Dọc") },
        { "Brightness", ("Brightness", "Độ Sáng") },
        { "FOV", ("Field of View", "Góc Nhìn (FOV)") },
        { "FPS Limit", ("FPS Limit", "Giới Hạn FPS") },

        // Gameplay
        { "Jumpscares", ("Jumpscares", "Hù Dọa") },
        { "Head Bobbing", ("Head Bobbing", "Rung Lắc Đầu") },
        { "Interaction Hints", ("Interaction Hints", "Gợi Ý Tương Tác") },
        { "Sprint Mode", ("Sprint Mode", "Chế Độ Chạy") },
        { "Crosshair", ("Crosshair", "Tâm Ngắm") },
        { "Hold", ("Hold", "Giữ") },
        { "Toggle", ("Toggle", "Bật/Tắt") },
        { "On", ("On", "Bật") },
        { "Off", ("Off", "Tắt") },

        // System
        { "UI Scale", ("UI Scale", "Kích Thước UI") },
        { "Language", ("Language", "Ngôn Ngữ") },
        { "English", ("English", "Tiếng Anh") },
        { "Vietnamese", ("Vietnamese", "Tiếng Việt") }
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved language
        string savedLang = PlayerPrefs.GetString("Language", "English");
        CurrentLanguage = savedLang;
    }

    public void ChangeLanguage(string newLanguage)
    {
        CurrentLanguage = newLanguage;
        PlayerPrefs.SetString("Language", newLanguage);
        OnLanguageChanged?.Invoke(newLanguage);
    }

    /// <summary>
    /// Gets localized text by key. If key not found, returns fallback or key.
    /// </summary>
    public string GetText(string key, string fallback = null)
    {
        if (dictionary.TryGetValue(key, out var translation))
        {
            return CurrentLanguage == "Vietnamese" ? translation.vi : translation.en;
        }
        return fallback ?? key;
    }
}
