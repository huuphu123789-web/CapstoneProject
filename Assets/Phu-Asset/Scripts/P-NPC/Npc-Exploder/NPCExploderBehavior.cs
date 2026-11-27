using System.Collections;
using UnityEngine;

/// <summary>
/// Hành vi NPC Nổ Tung (Exploder / Boomer Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, cơ thể sẽ phồng to dần rồi phát nổ tung toé.
/// </summary>
public class NPCExploderBehavior : NPCBaseBehavior
{
    [Header("=== Cấu Hình Nổ Tung ===")]
    public float swellDuration = 2.5f;          // Thời gian phồng to
    public float maxSwellScale = 1.8f;           // Mức độ phồng to
    public GameObject explosionParticlePrefab;   // Particle Nổ

    [Header("=== Âm Thanh Nổ Tung & Bom Hẹn Giờ ===")]
    [Tooltip("Tiếng bíp bíp bom hẹn giờ phát ngay khi Boomer spawn ra (nếu để trống sẽ tự dùng Swell Sound)")]
    public AudioClip timerBeepSound;
    [Tooltip("Khoảng cách tối đa nghe thấy tiếng bíp bíp 3D (mét)")]
    public float beepMaxDistance = 28f;
    public AudioClip swellSound;                 // Tiếng phồng to/bíp bíp
    public AudioClip explosionSound;             // Tiếng NỔ TUNG

    [Header("=== Trạng Thái Nổ ===")]
    public bool hasExploded = false;

    private Vector3 originalScale;
    private AudioSource beepAudioSource;

    protected override void Start()
    {
        base.Start();
        originalScale = transform.localScale;
        StartTimerBeepLoop();
    }

    private void StartTimerBeepLoop()
    {
        AudioClip beepClip = timerBeepSound != null ? timerBeepSound : swellSound;
        if (beepClip == null) return;

        beepAudioSource = gameObject.AddComponent<AudioSource>();
        beepAudioSource.clip = beepClip;
        beepAudioSource.loop = true;
        beepAudioSource.spatialBlend = 1.0f;
        beepAudioSource.rolloffMode = AudioRolloffMode.Linear;
        beepAudioSource.minDistance = 2.5f;
        beepAudioSource.maxDistance = beepMaxDistance;
        beepAudioSource.dopplerLevel = 0f;
        beepAudioSource.pitch = 1.0f;
        beepAudioSource.volume = 0.9f;
        beepAudioSource.Play();
    }

    private void StopTimerBeepLoop()
    {
        if (beepAudioSource != null && beepAudioSource.isPlaying)
        {
            beepAudioSource.Stop();
        }
    }

    void OnDisable()
    {
        StopTimerBeepLoop();
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        if (hasExploded) return;
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        if (beepAudioSource == null)
        {
            StartTimerBeepLoop();
        }

        float elapsed = 0f;
        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / swellDuration;

            // Tăng dần tốc độ tiếng bíp bíp khi sắp nổ
            if (beepAudioSource != null)
            {
                beepAudioSource.pitch = Mathf.Lerp(1.0f, 1.65f, progress);
            }

            // Phồng to dần
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSwellScale, progress);
            transform.localPosition += Random.insideUnitSphere * (progress * 0.04f);
            yield return null;
        }

        // --- NỔ TUNG! ---
        hasExploded = true;
        StopTimerBeepLoop();

        // PHÁT TIẾNG NỔ AN TOÀN (Không bị tắt khi GameObject Ẩn)
        if (explosionSound != null)
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(explosionSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }
        }

        // Tạo Particle
        if (explosionParticlePrefab != null)
        {
            Instantiate(explosionParticlePrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        Debug.Log("💥 NPC ĐÃ NỔ TUNG!");

        // Báo ngay cho NPCInspectionManager để tự động tính là Deny, tắt log thoại/voice và gọi NPC tiếp theo
        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.OnNPCExploded();
        }

        // Ẩn NPC
        gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}