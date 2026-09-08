using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string npcName = "NPC";

    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TMP_Text interactionText;

    [Header("Input System")]
    [SerializeField] private InputActionReference interactAction;

    private bool playerInRange;

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (interactionText != null)
            interactionText.text = "E - Nói chuyện";
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!playerInRange)
            return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy DialogueManager trong Scene!");
            return;
        }

        if (DialogueManager.Instance.IsDialogueActive)
            return;

        StartDialogue();
    }

    private void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning(gameObject.name + " chưa có lời thoại!");
            return;
        }

        DialogueManager.Instance.StartDialogue(
            npcName,
            dialogueLines
        );

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (DialogueManager.Instance != null &&
            !DialogueManager.Instance.IsDialogueActive)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}