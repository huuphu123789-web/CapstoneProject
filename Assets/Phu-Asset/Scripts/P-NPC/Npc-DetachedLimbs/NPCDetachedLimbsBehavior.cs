using UnityEngine;

/// <summary>
/// Hành vi NPC Tách Rời Chi (Detached Limbs Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, cánh tay sẽ tách rời khỏi cơ thể và lơ lửng.
/// </summary>
public class NPCDetachedLimbsBehavior : NPCBaseBehavior
{
    [Header("=== Xương Tách Rời (Detached Limbs) ===")]
    [Tooltip("Kéo xương Cánh Tay (LeftArm / RightArm) hoặc Chân vào đây")]
    public Transform leftArmBone;
    public Transform rightArmBone;
    public float detachDistance = 0.4f; // Khoảng cách tách rời khỏi cơ thể
    public float floatSpeed = 4f;

    [Header("=== Âm Thanh Tách Rời ===")]
    public AudioClip detachSound;        // Tiếng rắc rắc thịt/xương tách ra

    private Vector3 origLeftArmPos;
    private Vector3 origRightArmPos;

    protected override void Start()
    {
        base.Start();
        if (leftArmBone != null) origLeftArmPos = leftArmBone.localPosition;
        if (rightArmBone != null) origRightArmPos = rightArmBone.localPosition;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        PlaySound(detachSound);
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT -> TÁCH VÀ LƠ LỬNG CÁNH TAY RA NGOÀI
        if (isInspecting)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * 0.15f;

            if (leftArmBone != null)
            {
                leftArmBone.localPosition = origLeftArmPos + new Vector3(-detachDistance, offsetY, 0f);
            }

            if (rightArmBone != null)
            {
                rightArmBone.localPosition = origRightArmPos + new Vector3(detachDistance, -offsetY, 0f);
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