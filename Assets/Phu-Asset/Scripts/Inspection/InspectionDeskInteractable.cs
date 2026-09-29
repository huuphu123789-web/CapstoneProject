using UnityEngine;

/// <summary>
/// Vật thể tập hồ sơ / giấy tờ đặt trên quầy bốt gác (Guard Booth Table/Counter).
/// Khi có NPC đang chờ kiểm tra, người chơi nhìn vào và bấm [E] để mở bảng soi giấy tờ CCCD & Surrender Confirm.
/// </summary>
public class InspectionDeskInteractable : Interactable
{
    public static InspectionDeskInteractable instance;

    [Header("=== HIỂN THỊ ===")]
    [Tooltip("Mô hình tập giấy tờ trên bàn (ẩn khi không có NPC nào cần kiểm tra)")]
    public GameObject documentVisualProp;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        promptMessage = "Inspect Documents";
    }

    void Start()
    {
        SetDocumentsAvailable(false);
    }

    /// <summary>
    /// Bật/Tắt khả năng tương tác và hiển thị tập hồ sơ trên bàn
    /// </summary>
    public void SetDocumentsAvailable(bool available)
    {
        promptMessage = available ? "Inspect Documents" : "";
        if (documentVisualProp != null)
        {
            documentVisualProp.SetActive(available);
        }
    }

    public override void Interact()
    {
        base.Interact();

        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.OpenCurrentNPCDocuments();
        }
    }
}
