using UnityEngine;

/// <summary>
/// Hành vi NPC Xoay Đầu (Head Spin Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, đầu của NPC sẽ xoay tròn 360 độ kèm âm thanh rắc rắc.
/// </summary>
public class NPCHeadSpinBehavior : NPCBaseBehavior
{
    [Header("=== Cấu Hình Xoay Đầu ===")]
    public Transform headBone;        // Kéo xương Head vào đây
    public float spinSpeed = 360f;     // Tốc độ xoay (360 độ/giây)

    [Header("=== Âm Thanh Xoay Đầu ===")]
    public AudioClip headSpinSound;    // Tiếng rắc rắc xoay đầu

    private Quaternion originalHeadRot;
    private float currentSpinAngle = 0f;
    private float soundTimer;

    protected override void Start()
    {
        base.Start();
        if (headBone != null) originalHeadRot = headBone.localRotation;
    }

    protected override void Update()
    {
        base.Update();

        // KHI ĐANG KIỂM DUYỆT -> PHÁT TIẾNG XOAY ĐẦU
        if (isInspecting && headSpinSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(headSpinSound);
                soundTimer = 0.6f;
            }
        }
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT -> XOAY ĐẦU 360 ĐỘ
        if (isInspecting && headBone != null)
        {
            currentSpinAngle += Time.deltaTime * spinSpeed;
            headBone.localRotation = originalHeadRot * Quaternion.Euler(0f, currentSpinAngle, 0f);
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}