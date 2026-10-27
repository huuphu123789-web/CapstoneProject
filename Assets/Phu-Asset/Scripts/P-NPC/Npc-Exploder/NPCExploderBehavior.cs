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

    [Header("=== Âm Thanh Nổ Tung ===")]
    public AudioClip swellSound;                 // Tiếng phồng to/rít
    public AudioClip explosionSound;             // Tiếng NỔ TUNG

    [Header("=== Trạng Thái Nổ ===")]
    public bool hasExploded = false;

    private Vector3 originalScale;

    protected override void Start()
    {
        base.Start();
        originalScale = transform.localScale;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        if (hasExploded) return;
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        PlaySound(swellSound);

        float elapsed = 0f;
        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / swellDuration;

            // Phồng to dần
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSwellScale, progress);
            transform.localPosition += Random.insideUnitSphere * (progress * 0.04f);
            yield return null;
        }

        // --- NỔ TUNG! ---
        hasExploded = true;

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