using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Khi bi ban trung 1 phat -> NPC bien mat ngay lap tuc.
/// Can script nay vao NPC va dat Tag = "NPC".
/// </summary>
public class NPCHealth : MonoBehaviour, IDamageable
{
    [Header("=== Hieu Ung Bien Mat ===")]
    [Tooltip("(Tuy chon) Particle Effect bung ra khi bien mat")]
    [SerializeField] private GameObject disappearEffectPrefab;

    [Tooltip("(Tuy chon) Am thanh khi NPC bien mat")]
    [SerializeField] private AudioClip disappearSound;

    [Header("=== Su Kien ===")]
    [Tooltip("Goi khi NPC bien mat (dung de trigger animation, update UI, ...)")]
    public UnityEvent onDisappear;

    private bool _isGone = false;
    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Duoc PlayerGun goi khi ban trung. Bien mat ngay lap tuc.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_isGone) return;
        _isGone = true;

        // Phat hieu ung particle neu co
        if (disappearEffectPrefab != null)
            Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);

        // Phat am thanh neu co
        if (disappearSound != null)
        {
            // Tao object am thanh rieng vi NPC se bi destroy ngay
            AudioSource.PlayClipAtPoint(disappearSound, transform.position);
        }

        onDisappear?.Invoke();

        // Bien mat ngay
        Destroy(gameObject);
    }
}
