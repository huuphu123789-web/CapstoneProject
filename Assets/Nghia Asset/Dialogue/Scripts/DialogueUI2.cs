using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI2 : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;

    [Header("Typing Effect")]
    [SerializeField] private float textSpeed = 0.03f;

    private Coroutine typingCoroutine;

    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        Hide();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonPressed);
        }
    }

    public void Show()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    public void SetLine(DialogueLine line)
    {
        if (line == null)
            return;

        Show();

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;

        dialogueText.text = "";

        foreach (char character in text)
        {
            dialogueText.text += character;

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void CompleteCurrentLine(string fullText)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = fullText;

        isTyping = false;
    }

    private void OnContinueButtonPressed()
    {
        DialogueManager.Instance.NextDialogue();
    }
}