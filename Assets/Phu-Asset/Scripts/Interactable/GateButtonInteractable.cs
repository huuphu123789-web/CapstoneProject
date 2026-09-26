using UnityEngine;

/// <summary>
/// Nút bấm vật lý tại bốt gác để quyết định CHO QUA (APPROVE) hoặc TỪ CHỐI (DENY).
/// </summary>
public class GateButtonInteractable : Interactable
{
    public enum ButtonType { ApprovePass, RejectDeny }

    [Header("=== Loại Nút ===")]
    public ButtonType buttonType = ButtonType.ApprovePass;

    void Start()
    {
        // Gán câu chữ gợi ý hiển thị [E]
        if (buttonType == ButtonType.ApprovePass)
            promptMessage = "Approve Pass";
        else
            promptMessage = "Deny Entry";
    }

    public override void Interact()
    {
        base.Interact(); // Tự phát tiếng click button

        if (NPCInspectionManager.instance == null)
        {
            Debug.LogWarning("[GateButton] NPCInspectionManager chưa được khởi tạo trong Scene!");
            return;
        }

        if (buttonType == ButtonType.ApprovePass)
        {
            Debug.Log("✅ PLAYER BẤM NÚT CHO QUA (APPROVE)!");
            NPCInspectionManager.instance.ApproveCurrentNPC();
        }
        else
        {
            Debug.Log("❌ PLAYER BẤM NÚT TỪ CHỐI (DENY)!");
            NPCInspectionManager.instance.RejectCurrentNPC();
        }
    }
}