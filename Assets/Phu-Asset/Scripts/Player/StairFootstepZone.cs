using UnityEngine;

/// <summary>
/// Script dùng cho Cầu Thang (Hỗ trợ 100% MeshCollider, BoxCollider, hoặc Trigger).
/// Gắn script này trực tiếp lên GameObject Cầu Thang có MeshCollider.
/// Khi Player bước dẫm lên bậc thang, âm thanh bước chân sẽ tự động đổi sang tiếng cầu thang!
/// </summary>
public class StairFootstepZone : MonoBehaviour
{
    [Header("=== ÂM THANH BƯỚC CHÂN CẦU THANG ===")]
    [Tooltip("Kéo danh sách các file âm thanh bước chân trên cầu thang vào đây (VD: S_Wood_Mono_1, 2, 3...)")]
    public AudioClip[] stairFootstepSounds;

    [Header("=== TỐC ĐỘ BƯỚC CHÂN (TÙY CHỌN) ===")]
    [Tooltip("Tốc độ phát tiếng bước chân trên cầu thang (để -1 nếu muốn giữ nguyên tốc độ gốc của Player)")]
    public float customStepRate = -1f;

    // Hỗ trợ cả trường hợp dùng Trigger Collider
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null && stairFootstepSounds != null && stairFootstepSounds.Length > 0)
        {
            player.SetCustomFootsteps(stairFootstepSounds, customStepRate);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.ResetFootsteps();
        }
    }
}
