using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Vat the tuong tac: Dien thoai reo -> chuot phai nhac may -> phat voice + hien dialogue.
/// Implement IInteractable de hoat dong voi PlayerInteraction co san.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class InteractablePhone : MonoBehaviour, IInteractable
{
    [Header("=== Am thanh ===")]
    [Tooltip("Am thanh chuong dien thoai (loop)")]
    [SerializeField] private AudioClip ringSound;

    [Header("=== Du lieu cuoc goi (nhieu dong thoai) ===")]
    [Tooltip("Danh sach cac dong thoai")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("=== Tham chieu ===")]
    [Tooltip("Keo tha DialogueUI vao day (De trong = tu tim hoac tu tao UI tam thoi)")]
    [SerializeField] private DialogueUI dialogueUI;

    [Tooltip("(Tuy chon) Animator de trigger animation cam dien thoai")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Ten trigger trong Animator khi nhac may")]
    [SerializeField] private string pickupAnimTrigger = "PickupPhone";

    [Tooltip("Ten trigger trong Animator khi cup may")]
    [SerializeField] private string hangupAnimTrigger = "HangupPhone";

    [Header("=== Hieu ung rung lac ===")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeIntensity = 0.02f;
    [SerializeField] private float shakeSpeed = 30f;

    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Header("=== Tuy chon ===")]
    [Tooltip("Cho phep tuong tac lai sau khi da nhac may?")]
    [SerializeField] private bool allowReplay = false;

    // ── IInteractable ──
    public string InteractPrompt => isRinging ? "Nhac may" : (hasAnswered ? "" : "");

    // ── Internal ──
    private AudioSource audioSource;
    private bool isRinging = false;
    private bool hasAnswered = false;
    private bool isPlayingDialogue = false;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;
    private Coroutine dialogueSequenceCoroutine;
    private Renderer _renderer;
    private Color _originalColor;

    // ── Dynamic Subtitle UI Fallback (neu khong co DialogueUI) ──
    private GameObject _fallbackCanvasGo;
    private GameObject _fallbackPanelGo;
    private TextMeshProUGUI _fallbackText;
    private TextMeshProUGUI _fallbackSpeakerText;
    private CanvasGroup _fallbackCanvasGroup;
    private Coroutine _typewriterCo;

    // ========== DIALOGUE LINE DATA ==========

    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Ten nguoi noi")]
        public string speakerName = "";

        [Tooltip("Noi dung loi thoai")]
        [TextArea(2, 5)]
        public string text = "";

        [Tooltip("Voice clip cho dong thoai nay")]
        public AudioClip voiceClip;

        [Tooltip("Thoi gian hien thi (0 = tu tinh theo do dai am thanh)")]
        public float displayTime = 0f;
    }

    // ========== LIFECYCLE ==========

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        originalPosition = transform.localPosition;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    private void Start()
    {
        // Tu dong tim DialogueUI neu chua gan
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>() ?? FindObjectOfType<DialogueUI>();
        }

        // Bat dau reo chuong khi game bat dau
        StartRinging();
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalPosition;
        }
        StopRinging();
    }

    // ========== IInteractable ==========

    public void Interact(GameObject interactor)
    {
        if (!isRinging && (!allowReplay || isPlayingDialogue))
            return;

        AnswerPhone();
    }

    public void OnLookAt()
    {
        if (_renderer != null)
            _renderer.material.color = highlightColor;
    }

    public void OnLookAway()
    {
        if (_renderer != null)
            _renderer.material.color = _originalColor;
    }

    // ========== RINGING + SHAKE ==========

    public void StartRinging()
    {
        if (ringSound == null)
        {
            Debug.LogWarning("[InteractablePhone] Chua gan Ring Sound!", this);
            return;
        }

        audioSource.clip = ringSound;
        audioSource.loop = true;
        audioSource.Play();
        isRinging = true;
        hasAnswered = false;

        if (enableShake)
        {
            originalPosition = transform.localPosition;
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        Debug.Log("[InteractablePhone] Dien thoai dang reo...");
    }

    public void StopRinging()
    {
        if (isRinging)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isRinging = false;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
                transform.localPosition = originalPosition;
            }
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        while (true)
        {
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float offsetZ = Mathf.Cos(Time.time * shakeSpeed * 0.7f) * shakeIntensity * 0.5f;
            transform.localPosition = originalPosition + new Vector3(offsetX, 0f, offsetZ);
            yield return null;
        }
    }

    // ========== ANSWER PHONE ==========

    private void AnswerPhone()
    {
        if (hasAnswered && !allowReplay)
            return;

        hasAnswered = true;
        isPlayingDialogue = true;

        // 1. Dung chuong + rung
        StopRinging();

        // 2. Trigger animation nhac may
        if (playerAnimator != null && !string.IsNullOrEmpty(pickupAnimTrigger))
        {
            playerAnimator.SetTrigger(pickupAnimTrigger);
        }

        // 3. Bat dau chuoi dialogue lien tuc
        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            if (dialogueSequenceCoroutine != null)
                StopCoroutine(dialogueSequenceCoroutine);

            dialogueSequenceCoroutine = StartCoroutine(PlayDialogueSequence());
        }
        else
        {
            Debug.LogWarning("[InteractablePhone] Chua co dong thoai nao!", this);
            OnAllDialogueFinished();
        }
    }

    // ========== DIALOGUE SEQUENCE (PLAY CONTINUOUSLY WITH AUDIO) ==========

    private IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLine line = dialogueLines[i];

            // 1. Phat am thanh
            if (line.voiceClip != null)
            {
                audioSource.clip = line.voiceClip;
                audioSource.loop = false;
                audioSource.Play();
            }

            // 2. Tinh thoi gian hien thi toi uu
            float displayTime = line.displayTime;
            if (displayTime <= 0f && line.voiceClip != null)
            {
                displayTime = line.voiceClip.length;
            }
            else if (displayTime <= 0f)
            {
                displayTime = Mathf.Max(2.5f, line.text.Length * 0.06f);
            }

            // 3. Hien thi phu de (Dung DialogueUI co san hoac Fallback UI tu dong tao)
            if (dialogueUI != null)
            {
                dialogueUI.ShowDialogue(line.text, displayTime, line.speakerName);
            }
            else
            {
                // Phat bang UI tu dong
                ShowFallbackSubtitle(line.speakerName, line.text, displayTime);
            }

            Debug.Log($"[InteractablePhone] Line {i + 1}/{dialogueLines.Length}: {line.speakerName}: \"{line.text}\"");

            // 4. Cho cho am thanh va van ban chay het (dam bao phat het am thanh truoc khi qua cau tiep theo)
            float waitDuration = displayTime;
            if (line.voiceClip != null)
            {
                waitDuration = Mathf.Max(line.voiceClip.length, displayTime);
            }
            yield return new WaitForSeconds(waitDuration);

            // Cho them mot khoang nghi ngan giua cac cau
            if (i < dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(0.4f);
            }
        }

        dialogueSequenceCoroutine = null;
        OnAllDialogueFinished();
    }

    private void OnAllDialogueFinished()
    {
        isPlayingDialogue = false;

        // An fallback subtitle neu co
        HideFallbackSubtitle();

        // Trigger animation cup may
        if (playerAnimator != null && !string.IsNullOrEmpty(hangupAnimTrigger))
        {
            playerAnimator.SetTrigger(hangupAnimTrigger);
        }

        Debug.Log("[InteractablePhone] Cuoc goi ket thuc.");
    }

    // ========== FALLBACK SUBTITLE UI (TU DONG TAO NEU THIEU DIALOGUE UI) ==========

    private void ShowFallbackSubtitle(string speaker, string text, float duration)
    {
        if (_fallbackCanvasGo == null)
        {
            CreateFallbackUI();
        }

        _fallbackCanvasGo.SetActive(true);
        if (_fallbackSpeakerText != null) _fallbackSpeakerText.text = speaker;

        if (_typewriterCo != null) StopCoroutine(_typewriterCo);
        _typewriterCo = StartCoroutine(FallbackTypewriter(text));

        StartCoroutine(FadeFallbackUI(true));
        StartCoroutine(AutoHideFallback(duration));
    }

    private void HideFallbackSubtitle()
    {
        if (_fallbackCanvasGo != null && _fallbackCanvasGo.activeSelf)
        {
            StartCoroutine(FadeFallbackUI(false));
        }
    }

    private IEnumerator AutoHideFallback(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideFallbackSubtitle();
    }

    private IEnumerator FallbackTypewriter(string fullText)
    {
        if (_fallbackText == null) yield break;
        _fallbackText.text = fullText;
        _fallbackText.maxVisibleCharacters = 0;
        _fallbackText.ForceMeshUpdate();

        float delay = 1f / 35f; // Toc do typewriter
        for (int i = 0; i < fullText.Length; i++)
        {
            _fallbackText.maxVisibleCharacters = i + 1;
            char c = fullText[i];
            if (c == '.' || c == '!' || c == '?') yield return new WaitForSeconds(delay * 4f);
            else if (c == ',') yield return new WaitForSeconds(delay * 2f);
            else yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator FadeFallbackUI(bool fadeIn)
    {
        if (_fallbackCanvasGroup == null) yield break;
        float elapsed = 0f, dur = 0.25f;
        float start = fadeIn ? 0f : 1f;
        float end = fadeIn ? 1f : 0f;
        if (fadeIn) _fallbackPanelGo.SetActive(true);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            _fallbackCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / dur);
            yield return null;
        }
        _fallbackCanvasGroup.alpha = end;
        if (!fadeIn) _fallbackPanelGo.SetActive(false);
    }

    private void CreateFallbackUI()
    {
        _fallbackCanvasGo = new GameObject("InteractablePhone_FallbackCanvas");
        Canvas canvas = _fallbackCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler cs = _fallbackCanvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        _fallbackCanvasGo.AddComponent<GraphicRaycaster>();

        // Panel nen chu
        _fallbackPanelGo = new GameObject("Panel");
        _fallbackPanelGo.transform.SetParent(_fallbackCanvasGo.transform, false);
        Image bg = _fallbackPanelGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        _fallbackCanvasGroup = _fallbackPanelGo.AddComponent<CanvasGroup>();

        RectTransform panelRT = _fallbackPanelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.sizeDelta = new Vector2(0f, 120f);
        panelRT.anchoredPosition = new Vector2(0f, 40f); // Cach mep duoi mot chut

        // Ten nguoi noi
        GameObject spGo = new GameObject("Speaker");
        spGo.transform.SetParent(_fallbackPanelGo.transform, false);
        _fallbackSpeakerText = spGo.AddComponent<TextMeshProUGUI>();
        _fallbackSpeakerText.fontSize = 18f;
        _fallbackSpeakerText.fontStyle = FontStyles.Bold;
        _fallbackSpeakerText.color = new Color(1f, 0.82f, 0.25f);
        
        RectTransform spRT = spGo.GetComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0f, 1f);
        spRT.anchorMax = new Vector2(1f, 1f);
        spRT.pivot = new Vector2(0.5f, 0f);
        spRT.anchoredPosition = new Vector2(40f, 4f);
        spRT.sizeDelta = new Vector2(-80f, 24f);

        // Loi thoai
        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(_fallbackPanelGo.transform, false);
        _fallbackText = txtGo.AddComponent<TextMeshProUGUI>();
        _fallbackText.fontSize = 21f;
        _fallbackText.color = Color.white;
        _fallbackText.enableWordWrapping = true;

        RectTransform txtRT = txtGo.GetComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0f, 0f);
        txtRT.anchorMax = new Vector2(1f, 1f);
        txtRT.offsetMin = new Vector2(40f, 10f);
        txtRT.offsetMax = new Vector2(-40f, -6f);
    }
}