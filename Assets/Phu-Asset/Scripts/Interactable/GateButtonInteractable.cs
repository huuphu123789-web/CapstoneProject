using UnityEngine;

public class GateButtonInteractable : Interactable
{
    public enum ButtonType { ApprovePass, RejectDeny }

    [Header("=== Loại Nút ===")]
    public ButtonType buttonType = ButtonType.ApprovePass;

    void Start()
    {
        // Gán câu chữ gợi ý hiển thị [E]
        if (buttonType == ButtonType.ApprovePass)
            promptMessage = "CHO QUA (PASS)";
        else
            promptMessage = "TỪ CHỐI (DENY)";
    }

    // GHI ĐÈ HÀM INTERACT
    public override void Interact()
    {
        base.Interact(); // Tự phát tiếng click button

        if (buttonType == ButtonType.ApprovePass)
        {
            Debug.Log("✅ PLAYER BẤM NÚT CHO QUA!");
            // Gọi logic cho NPC đi qua cổng...
        }
        else
        {
            Debug.Log("❌ PLAYER BẤM NÚT TỪ CHỐI!");
            // Gọi logic đuổi NPC đi...
        }
    }
}