using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý UI hiển thị lời thoại (dialogue) với TextMeshPro.
/// Tính năng: fade in/out, typewriter effect.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("=== Dialogue UI ===")]
    [Tooltip("Panel chứa toàn bộ dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;

    [Tooltip("TextMeshPro hiển thị lời thoại")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Tooltip("(Tuỳ chọn) TextMeshPro hiển thị tên người nói")]
    [SerializeField] private TextMeshProUGUI speakerNameText;

    [Header("=== Typewriter Effect ===")]
    [Tooltip("Bật hiệu ứng typewriter (hiện từng chữ)")]
    [SerializeField] private bool enableTypewriter = true;

    [Tooltip("Tốc độ typewriter (ký tự/giây)")]
    [SerializeField] private float typewriterSpeed = 40f;

    [Tooltip("(Tuỳ chọn) Âm thanh khi gõ từng chữ")]
    [SerializeField] private AudioClip typewriterSound;

    [Tooltip("Chỉ phát âm thanh mỗi N ký tự (tránh spam)")]
    [SerializeField] private int typewriterSoundEvery = 3;

    [Header("=== Animation ===")]
    [Tooltip("Thời gian fade in/out (giây)")]
    [SerializeField] private float fadeDuration = 0.3f;

    // ── Internal ──
    private CanvasGroup dialogueCanvasGroup;
    private Coroutine currentDialogueCoroutine;
    private Coroutine currentTypewriterCoroutine;
    private AudioSource audioSource;

    // ========== LIFECYCLE ==========

    private void Awake()
    {
        // Setup CanvasGroup cho dialogue panel
        dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (dialogueCanvasGroup == null)
            dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();

        // AudioSource cho typewriter sound
        if (typewriterSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // Ẩn dialogue panel lúc đầu
        HideImmediate();
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Hiển thị lời thoại với typewriter effect, sau đó tự ẩn.
    /// </summary>
    /// <param name="text">Nội dung lời thoại</param>
    /// <param name="duration">Thời gian hiển thị tổng cộng (giây)</param>
    /// <param name="speakerName">Tên người nói (tuỳ chọn)</param>
    public void ShowDialogue(string text, float duration, string speakerName = "")
    {
        // Dừng dialogue cũ nếu đang chạy
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);
        if (currentTypewriterCoroutine != null)
            StopCoroutine(currentTypewriterCoroutine);

        currentDialogueCoroutine = StartCoroutine(DialogueSequence(text, duration, speakerName));
    }

    /// <summary>
    /// Ẩn dialogue ngay lập tức.
    /// </summary>
    public void HideDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }
        if (currentTypewriterCoroutine != null)
        {
            StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = null;
        }

        StartCoroutine(FadeOut());
    }

    // ========== COROUTINES ==========

    private IEnumerator DialogueSequence(string text, float duration, string speakerName)
    {
        // Gán tên người nói
        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }

        // Reset text
        if (dialogueText != null)
            dialogueText.text = "";

        // Fade in panel
        yield return StartCoroutine(FadeIn());

        // Typewriter hoặc hiện ngay
        if (enableTypewriter && dialogueText != null)
        {
            currentTypewriterCoroutine = StartCoroutine(TypewriterEffect(text));
            yield return currentTypewriterCoroutine;
            currentTypewriterCoroutine = null;

            // Tính thời gian còn lại sau typewriter
            float typewriterTime = text.Length / typewriterSpeed;
            float remainingTime = duration - typewriterTime - fadeDuration;
            if (remainingTime > 0f)
                yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            // Hiện ngay toàn bộ
            if (dialogueText != null)
                dialogueText.text = text;

            float waitTime = duration - fadeDuration * 2f;
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        currentDialogueCoroutine = null;
    }

    /// <summary>
    /// Hiệu ứng typewriter - hiện từng ký tự một.
    /// Dùng TMP maxVisibleCharacters cho hiệu ứng mượt.
    /// </summary>
    private IEnumerator TypewriterEffect(string fullText)
    {
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalChars = fullText.Length;
        float delay = 1f / typewriterSpeed;
        int charCount = 0;

        for (int i = 0; i < totalChars; i++)
        {
            charCount++;
            dialogueText.maxVisibleCharacters = charCount;

            // Phát âm thanh gõ chữ
            if (typewriterSound != null && audioSource != null)
            {
                if (charCount % typewriterSoundEvery == 0 && fullText[i] != ' ')
                    audioSource.PlayOneShot(typewriterSound, 0.3f);
            }

            // Pause lâu hơn ở dấu câu
            char c = fullText[i];
            if (c == '.' || c == '!' || c == '?')
                yield return new WaitForSeconds(delay * 5f);
            else if (c == ',')
                yield return new WaitForSeconds(delay * 3f);
            else
                yield return new WaitForSeconds(delay);
        }

        dialogueText.maxVisibleCharacters = totalChars;
    }

    // ========== FADE ==========

    private IEnumerator FadeIn()
    {
        dialoguePanel.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            dialogueCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        dialogueCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            dialogueCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        dialogueCanvasGroup.alpha = 0f;
        dialoguePanel.SetActive(false);
    }

    private void HideImmediate()
    {
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 0f;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
