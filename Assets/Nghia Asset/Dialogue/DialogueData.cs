using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Thông tin Dialogue")]
    public string dialogueID;

    [Header("Các câu thoại")]
    public DialogueLine[] lines;
}