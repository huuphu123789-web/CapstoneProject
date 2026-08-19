using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Input")]
    [SerializeField] private InputActionReference nextDialogueAction;

    [Header("Typing")]
    [SerializeField] private float textSpeed = 0.03f;

    private string[] currentLines;
    private string currentSpeaker;

    private int currentIndex;

    private bool isTyping;
    private bool isDialogueActive;

    private float typingTimer;

    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (nextDialogueAction != null)
        {
            nextDialogueAction.action.performed += OnNextDialogue;
            nextDialogueAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (nextDialogueAction != null)
        {
            nextDialogueAction.action.performed -= OnNextDialogue;
            nextDialogueAction.action.Disable();
        }
    }

    public void StartDialogue(string speakerName, string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Dialogue không có nội dung!");
            return;
        }

        currentSpeaker = speakerName;
        currentLines = lines;
        currentIndex = 0;

        isDialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null)
            return;

        if (currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        if (speakerNameText != null)
            speakerNameText.text = currentSpeaker;

        dialogueText.text = "";

        typingTimer = 0f;
        isTyping = true;
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

        if (!isTyping)
            return;

        string fullText = currentLines[currentIndex];

        typingTimer += Time.deltaTime;

        int characterCount =
            Mathf.FloorToInt(typingTimer / textSpeed);

        characterCount =
            Mathf.Clamp(
                characterCount,
                0,
                fullText.Length
            );

        dialogueText.text =
            fullText.Substring(0, characterCount);

        // Đã đọc xong câu
        if (characterCount >= fullText.Length)
        {
            isTyping = false;

            // Nếu đây là câu cuối
            if (currentIndex >= currentLines.Length - 1)
            {
                EndDialogue();
            }
        }
    }

    private void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (!isDialogueActive)
            return;

        NextDialogue();
    }

    public void NextDialogue()
    {
        if (!isDialogueActive)
            return;

        // Nếu chữ đang chạy
        // -> hiện toàn bộ câu ngay lập tức
        if (isTyping)
        {
            dialogueText.text = currentLines[currentIndex];

            isTyping = false;

            // Nếu là câu cuối thì tắt luôn
            if (currentIndex >= currentLines.Length - 1)
            {
                EndDialogue();
            }

            return;
        }

        // Sang câu tiếp theo
        currentIndex++;

        // Hết thoại
        if (currentIndex >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void EndDialogue()
    {
        isDialogueActive = false;

        currentLines = null;
        currentSpeaker = "";
        currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}