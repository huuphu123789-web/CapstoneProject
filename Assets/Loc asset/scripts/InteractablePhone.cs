using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Vật thể tương tác: Điện thoại reo → chuột phải nhấc máy → phát voice + hiện dialogue.
/// Implement IInteractable để hoạt động với PlayerInteraction có sẵn.
/// Gắn script này lên GameObject điện thoại (có Collider).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class InteractablePhone : MonoBehaviour, IInteractable
{
    [Header("=== Âm thanh ===")]
    [Tooltip("Âm thanh chuông điện thoại (loop)")]
    [SerializeField] private AudioClip ringSound;

    [Header("=== Dữ liệu cuộc gọi (nhiều dòng thoại) ===")]
    [Tooltip("Danh sách các dòng thoại")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("=== Tham chiếu ===")]
    [Tooltip("Kéo thả DialogueUI vào đây")]
    [SerializeField] private DialogueUI dialogueUI;

    [Tooltip("(Tuỳ chọn) Animator của nhân vật để trigger animation cầm điện thoại")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Tên trigger trong Animator khi nhấc máy")]
    [SerializeField] private string pickupAnimTrigger = "PickupPhone";

    [Tooltip("Tên trigger trong Animator khi cúp máy")]
    [SerializeField] private string hangupAnimTrigger = "HangupPhone";

    [Header("=== Hiệu ứng rung lắc ===")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeIntensity = 0.02f;
    [SerializeField] private float shakeSpeed = 30f;

    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Header("=== Tuỳ chọn ===")]
    [Tooltip("Cho phép tương tác lại sau khi đã nhấc máy?")]
    [SerializeField] private bool allowReplay = false;

    // ── IInteractable ──
    public string InteractPrompt => isRinging ? "Nhấc máy" : (hasAnswered ? "" : "");

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

    // ========== DIALOGUE LINE DATA ==========

    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Tên người nói (để trống nếu không cần)")]
        public string speakerName = "";

        [Tooltip("Nội dung lời thoại")]
        [TextArea(2, 5)]
        public string text = "";

        [Tooltip("Voice clip cho dòng thoại này")]
        public AudioClip voiceClip;

        [Tooltip("Thời gian hiển thị (0 = tự tính theo voice clip)")]
        public float displayTime = 0f;
    }

    // ========== LIFECYCLE ==========

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.localPosition;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    private void Start()
    {
        // Bắt đầu reo chuông
        StartRinging();
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalPosition;
        }
    }

    // ========== IInteractable ==========

    /// <summary>
    /// Được gọi khi người chơi nhấn chuột phải vào điện thoại (qua PlayerInteraction).
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!isRinging && (!allowReplay || isPlayingDialogue))
            return;

        AnswerPhone();
    }

    /// <summary>
    /// Highlight khi người chơi nhìn vào.
    /// </summary>
    public void OnLookAt()
    {
        if (_renderer != null)
            _renderer.material.color = highlightColor;
    }

    /// <summary>
    /// Bỏ highlight khi nhìn đi.
    /// </summary>
    public void OnLookAway()
    {
        if (_renderer != null)
            _renderer.material.color = _originalColor;
    }

    // ========== RINGING + SHAKE ==========

    /// <summary>
    /// Bắt đầu reo chuông điện thoại.
    /// </summary>
    public void StartRinging()
    {
        if (ringSound == null)
        {
            Debug.LogWarning("[InteractablePhone] Chưa gán Ring Sound!", this);
            return;
        }

        audioSource.clip = ringSound;
        audioSource.loop = true;
        audioSource.Play();
        isRinging = true;
        hasAnswered = false;

        // Bắt đầu rung lắc
        if (enableShake)
        {
            originalPosition = transform.localPosition;
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        Debug.Log("[InteractablePhone] Điện thoại đang reo...");
    }

    /// <summary>
    /// Dừng chuông và rung.
    /// </summary>
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

    /// <summary>
    /// Coroutine rung lắc điện thoại.
    /// </summary>
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

    /// <summary>
    /// Nhấc máy: dừng reo, phát animation, bắt đầu chuỗi dialogue.
    /// </summary>
    private void AnswerPhone()
    {
        if (hasAnswered && !allowReplay)
            return;

        hasAnswered = true;
        isPlayingDialogue = true;

        // 1. Dừng chuông + rung
        StopRinging();

        // 2. Trigger animation nhấc máy
        if (playerAnimator != null && !string.IsNullOrEmpty(pickupAnimTrigger))
        {
            playerAnimator.SetTrigger(pickupAnimTrigger);
            Debug.Log($"[InteractablePhone] Trigger animation: {pickupAnimTrigger}");
        }

        // 3. Bắt đầu chuỗi dialogue
        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            if (dialogueSequenceCoroutine != null)
                StopCoroutine(dialogueSequenceCoroutine);

            dialogueSequenceCoroutine = StartCoroutine(PlayDialogueSequence());
        }
        else
        {
            Debug.LogWarning("[InteractablePhone] Chưa có dòng thoại nào!", this);
            OnAllDialogueFinished();
        }
    }

    // ========== DIALOGUE SEQUENCE ==========

    /// <summary>
    /// Phát lần lượt từng dòng thoại.
    /// </summary>
    private IEnumerator PlayDialogueSequence()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLine line = dialogueLines[i];

            // Phát voice clip
            if (line.voiceClip != null)
            {
                audioSource.clip = line.voiceClip;
                audioSource.loop = false;
                audioSource.Play();
            }

            // Tính thời gian hiển thị
            float displayTime = line.displayTime;
            if (displayTime <= 0f && line.voiceClip != null)
            {
                displayTime = line.voiceClip.length + 0.5f;
            }
            else if (displayTime <= 0f)
            {
                displayTime = Mathf.Max(2f, line.text.Length * 0.05f);
            }

            // Hiện dialogue trên UI (với typewriter)
            if (dialogueUI != null)
            {
                dialogueUI.ShowDialogue(line.text, displayTime, line.speakerName);
            }

            Debug.Log($"[InteractablePhone] Dòng {i + 1}/{dialogueLines.Length}: \"{line.text}\"");

            // Chờ hết thời gian hiển thị
            yield return new WaitForSeconds(displayTime);

            // Chờ thêm chút giữa các dòng
            if (i < dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        dialogueSequenceCoroutine = null;
        OnAllDialogueFinished();
    }

    /// <summary>
    /// Gọi khi tất cả dòng thoại đã phát xong.
    /// </summary>
    private void OnAllDialogueFinished()
    {
        isPlayingDialogue = false;

        // Trigger animation cúp máy
        if (playerAnimator != null && !string.IsNullOrEmpty(hangupAnimTrigger))
        {
            playerAnimator.SetTrigger(hangupAnimTrigger);
        }

        Debug.Log("[InteractablePhone] Cuộc gọi kết thúc.");
    }
}
