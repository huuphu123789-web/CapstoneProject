using System.Collections;
using UnityEngine;

/// <summary>
/// Script dùng cho Điểm Tuần Tra Hàng Rào (Fence Patrol Checkpoint).
/// Kế thừa Interactable (Phú-Asset) - Hỗ trợ cả đi vào vùng Trigger hoặc bấm E để kiểm tra hàng rào.
/// Kích hoạt hù dọa tăng tiến theo thứ tự điểm (1, 2, 3).
/// </summary>
public class FencePatrolZone : Interactable
{
    public enum TriggerMode { WalkIntoZone, PressEToInspect }

    [Header("=== CẤU HÌNH ĐIỂM TUẦN TRA ===")]
    [Tooltip("Số thứ tự điểm tuần tra (1: rung rào nhẹ, 2: cào rào & tắt đèn, 3: camera shake & jumpscare)")]
    public int fenceIndex = 1;

    [Tooltip("Cách kích hoạt: Đi vào vùng (Trigger) hoặc Bấm E để kiểm tra")]
    public TriggerMode triggerMode = TriggerMode.WalkIntoZone;

    [Header("=== ÂM THANH HÙ DỌA ===")]
    [Tooltip("Âm thanh riêng cho điểm rào này (tiếng rung rào, tiếng cào xước kim loại, tiếng gầm gừ...)")]
    public AudioClip fenceSound;
    public AudioSource localAudioSource;

    [Header("=== BÓNG MA / VẬT THỂ XUẤT HIỆN ===")]
    [Tooltip("Tùy chọn: GameObject bóng đen lướt qua hàng rào rồi biến mất")]
    public GameObject spookyVisualObject;

    private bool isTriggered = false;

    void Awake()
    {
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Start()
    {
        promptMessage = (triggerMode == TriggerMode.PressEToInspect) ? "Inspect Fence" : "";
        if (spookyVisualObject != null) spookyVisualObject.SetActive(false);
    }

    // Kích hoạt khi đi vào vùng Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || triggerMode != TriggerMode.WalkIntoZone) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            TriggerPatrolPoint();
        }
    }

    // Kích hoạt khi bấm E
    public override void Interact()
    {
        if (isTriggered || triggerMode != TriggerMode.PressEToInspect) return;

        TriggerPatrolPoint();
    }

    private void TriggerPatrolPoint()
    {
        isTriggered = true;
        promptMessage = ""; // Ẩn gợi ý tương tác

        // Tắt Collider ngay lập tức
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        // 1. Phát âm thanh hù dọa của hàng rào
        PlaySound(fenceSound != null ? fenceSound : interactSound);

        // 2. Kích hoạt bóng ma / mắt đỏ lướt qua rào nếu có
        if (spookyVisualObject != null)
        {
            spookyVisualObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            spookyVisualObject.SetActive(false);
        }

        // 3. Báo về TaskManager
        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteFencePatrol(fenceIndex);
        }

        // 4. Vô hiệu hóa Collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
