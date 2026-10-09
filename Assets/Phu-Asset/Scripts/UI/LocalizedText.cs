using UnityEngine;
using TMPro;

/// <summary>
/// Attach to any TextMeshProUGUI to automatically update text when Language changes.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Translation key in LocalizationManager dictionary (e.g. 'Play', 'Settings', 'Quality')")]
    public string textKey;

    [Tooltip("Custom English text (leave empty to use original text)")]
    public string englishText;

    [Tooltip("Custom Vietnamese text (leave empty to use dictionary)")]
    public string vietnameseText;

    private TextMeshProUGUI tmpText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(englishText) && tmpText != null)
        {
            englishText = tmpText.text;
        }
        if (string.IsNullOrEmpty(textKey) && tmpText != null)
        {
            textKey = tmpText.text;
        }
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText(LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : PlayerPrefs.GetString("Language", "English"));
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText(string language)
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
        if (tmpText == null) return;

        if (language == "Vietnamese")
        {
            if (!string.IsNullOrEmpty(vietnameseText))
            {
                tmpText.text = vietnameseText;
            }
            else if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(textKey))
            {
                tmpText.text = LocalizationManager.Instance.GetText(textKey, englishText);
            }
        }
        else
        {
            tmpText.text = !string.IsNullOrEmpty(englishText) ? englishText : textKey;
        }
    }
}
