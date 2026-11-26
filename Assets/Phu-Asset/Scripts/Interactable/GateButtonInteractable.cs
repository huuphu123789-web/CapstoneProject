using System.Collections;
using UnityEngine;

/// <summary>
/// Nút bấm / công tắc vật lý tại bốt gác để quyết định CHO QUA (APPROVE) hoặc TỪ CHỐI (DENY).
/// Tự động bật/tắt GameObject con "On" / "Off" của công tắc và đèn báo đi kèm khi tương tác.
/// </summary>
public class GateButtonInteractable : Interactable
{
    public enum ButtonType { ApprovePass, RejectDeny }

    [Header("=== Loại Nút ===")]
    public ButtonType buttonType = ButtonType.ApprovePass;

    [Header("=== Công Tắc On / Off (Tự động tìm nếu để trống) ===")]
    [Tooltip("GameObject hiển thị trạng thái BẬT (On) của công tắc")]
    public GameObject switchOnObject;

    [Tooltip("GameObject hiển thị trạng thái TẮT (Off) của công tắc")]
    public GameObject switchOffObject;

    [Tooltip("GameObject đèn báo trạng thái BẬT (On) cạnh công tắc (nếu có)")]
    public GameObject lightOnObject;

    [Tooltip("GameObject đèn báo trạng thái TẮT (Off) cạnh công tắc (nếu có)")]
    public GameObject lightOffObject;

    [Tooltip("Thời gian (giây) tự gạt công tắc về lại Off sau khi bấm (đặt <= 0 nếu muốn bấm bật/tắt thủ công)")]
    public float autoResetToOffDelay = 1.5f;

    private bool isSwitchOn = false;
    private Coroutine resetRoutine;

    void Awake()
    {
        EnsureInteractCollider();
        AutoFindOnOffObjects();
        ApplyVisualState(false);
    }

    void Start()
    {
        // Gán câu chữ gợi ý hiển thị [E]
        if (buttonType == ButtonType.ApprovePass)
            promptMessage = "Approve Pass";
        else
            promptMessage = "Deny Entry";
    }

    private void EnsureInteractCollider()
    {
        if (GetComponent<Collider>() != null) return;

        // Đảm bảo vẫn bấm E được kể cả khi child "Off" bị ẩn và "On" hiện lên
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(0.03f, 0.03f, 0.03f);
        box.center = new Vector3(0f, 0.012f, 0f);
    }

    private void AutoFindOnOffObjects()
    {
        if (switchOnObject == null)
        {
            Transform onTf = transform.Find("On");
            if (onTf != null) switchOnObject = onTf.gameObject;
        }
        if (switchOffObject == null)
        {
            Transform offTf = transform.Find("Off");
            if (offTf != null) switchOffObject = offTf.gameObject;
        }

        if ((lightOnObject == null || lightOffObject == null) && transform.parent != null)
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                Transform sibling = transform.parent.GetChild(i);
                if (sibling == transform) continue;
                if (!sibling.name.ToLowerInvariant().Contains("light")) continue;

                Transform lOn = sibling.Find("On");
                Transform lOff = sibling.Find("Off");
                if (lightOnObject == null && lOn != null) lightOnObject = lOn.gameObject;
                if (lightOffObject == null && lOff != null) lightOffObject = lOff.gameObject;
                break;
            }
        }
    }

    public void ApplyVisualState(bool on)
    {
        isSwitchOn = on;
        if (switchOnObject != null) switchOnObject.SetActive(on);
        if (switchOffObject != null) switchOffObject.SetActive(!on);
        if (lightOnObject != null) lightOnObject.SetActive(on);
        if (lightOffObject != null) lightOffObject.SetActive(!on);
    }

    private IEnumerator ResetSwitchRoutine()
    {
        yield return new WaitForSeconds(autoResetToOffDelay);
        ApplyVisualState(false);
        resetRoutine = null;
    }

    public override void Interact()
    {
        base.Interact(); // Tự phát tiếng click button

        // Bật/Tắt trạng thái On / Off của công tắc và đèn báo
        if (autoResetToOffDelay > 0f)
        {
            ApplyVisualState(true);
            if (resetRoutine != null) StopCoroutine(resetRoutine);
            resetRoutine = StartCoroutine(ResetSwitchRoutine());
        }
        else
        {
            ApplyVisualState(!isSwitchOn);
        }

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