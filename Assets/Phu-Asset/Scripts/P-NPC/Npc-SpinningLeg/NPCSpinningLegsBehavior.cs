using UnityEngine;

/// <summary>
/// Hành vi NPC Chân Xoay Chong Chóng (Spinning Legs Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, hai chân sẽ xoay tít như chong chóng.
/// </summary>
public class NPCSpinningLegsBehavior : NPCBaseBehavior
{
    [Header("=== Cấu Hình Chân Xoay Chong Chóng ===")]
    [Tooltip("Kéo xương Đùi/Chân trái (LeftUpLeg / LeftLeg) vào đây")]
    public Transform leftLegBone;
    [Tooltip("Kéo xương Đùi/Chân phải (RightUpLeg / RightLeg) vào đây")]
    public Transform rightLegBone;
    public float spinSpeed = 720f; // Tốc độ xoay (720 độ/giây = 2 vòng/s)

    [Header("=== Âm Thanh Xoay Chân ===")]
    public AudioClip legSpinSound; // Tiếng vồ vập / xoay rắc rắc

    private Quaternion origLeftLegRot;
    private Quaternion origRightLegRot;
    private float spinAngle = 0f;
    private float soundTimer;

    protected override void Awake()
    {
        base.Awake();
        if (leftLegBone != null) origLeftLegRot = leftLegBone.localRotation;
        if (rightLegBone != null) origRightLegRot = rightLegBone.localRotation;
    }

    public override void MoveToInspectionPoint()
    {
        base.MoveToInspectionPoint();
        // Trở lại vị trí chân bình thường khi bước đi
        if (leftLegBone != null) leftLegBone.localRotation = origLeftLegRot;
        if (rightLegBone != null) rightLegBone.localRotation = origRightLegRot;
    }

    protected override void Update()
    {
        base.Update();

        // Phát âm thanh xoay chân theo chu kỳ
        if (isInspecting && legSpinSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(legSpinSound);
                soundTimer = 0.5f;
            }
        }
    }

    // LATE UPDATE: XOAY 2 CHÂN NHƯ CHONG CHÓNG KHI ĐẾN VẠCH KIỂM DUYỆT
    void LateUpdate()
    {
        if (isInspecting)
        {
            spinAngle += Time.deltaTime * spinSpeed;

            // Chân trái xoay thuận chiều
            if (leftLegBone != null)
            {
                leftLegBone.localRotation = origLeftLegRot * Quaternion.Euler(spinAngle, 0f, 0f);
            }

            // Chân phải xoay ngược chiều tạo hiệu ứng chong chóng
            if (rightLegBone != null)
            {
                rightLegBone.localRotation = origRightLegRot * Quaternion.Euler(-spinAngle, 0f, 0f);
            }
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}