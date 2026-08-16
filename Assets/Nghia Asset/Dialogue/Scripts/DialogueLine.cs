using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [Header("Thông tin nhân vật")]
    public string speakerName;

    [TextArea(2, 5)]
    public string dialogueText;
}