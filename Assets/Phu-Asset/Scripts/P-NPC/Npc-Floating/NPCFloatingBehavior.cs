using UnityEngine;

/// <summary>
/// Hành vi NPC Lơ Lửng (Floating Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, cơ thể sẽ bay lơ lửng nhấp nhô trên không.
/// </summary>
public class NPCFloatingBehavior : NPCBaseBehavior
{
    [Header("=== Cấu Hình Lơ Lửng ===")]
    public float floatHeight = 0.6f;  // Chiều cao bay lên
    public float floatSpeed = 2.5f;   // Tốc độ nhấp nhô lơ lửng
    public AudioClip floatHumSound;   // Tiếng vi vu/u u lơ lửng

    private Vector3 originalLocalPos;
    private float soundTimer;

    protected override void Start()
    {
        base.Start();
        originalLocalPos = transform.localPosition;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        originalLocalPos = transform.localPosition;
    }

    protected override void Update()
    {
        base.Update();

        // KHI ĐANG KIỂM DUYỆT -> PHÁT TIẾNG LƠ LỬNG
        if (isInspecting && floatHumSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(floatHumSound);
                soundTimer = 1.5f;
            }
        }
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT -> BAY LƠ LỬNG LÊN TRỜI
        if (isInspecting)
        {
            float newY = Mathf.Sin(Time.time * floatSpeed) * 0.25f + floatHeight;
            transform.localPosition = originalLocalPos + new Vector3(0f, newY, 0f);
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}