using System.Collections;
using UnityEngine;

/// <summary>
/// Script dùng cho Đống Lá Ngoài Sân (Leaf Pile).
/// Kế thừa Interactable (Phú-Asset) - Nhấn phím E để quét lá và kích hoạt sự kiện hù dọa tăng tiến!
/// </summary>
public class LeafPileInteractable : Interactable
{
    [Header("=== THỨ TỰ ĐỐNG LÁ ===")]
    [Tooltip("Số thứ tự của đống lá (1 là đống đầu tiên, 2 là đống thứ hai có hù dọa)")]
    public int leafIndex = 1;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Tiếng chổi quét lá sột soạt")]
    public AudioClip sweepSound;

    [Tooltip("Tiếng ma quái / cành gãy / thì thầm (dành cho đống lá 2)")]
    public AudioClip spookySound;

    [Header("=== HIỆU ỨNG THỊ GIÁC ===")]
    [Tooltip("Tùy chọn: GameObject bóng ma hoặc vật thể xuất hiện thoáng qua rồi biến mất")]
    public GameObject spookyVisualObject;

    public AudioSource localAudioSource;

    private bool isSwept = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

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
        promptMessage = "Sweep Leaf";
        if (spookyVisualObject != null) spookyVisualObject.SetActive(false);
    }

    public override void Interact()
    {
        if (isSwept) return;

        isSwept = true;
        promptMessage = ""; // Ẩn gợi ý tương tác ngay lập tức

        // Khi bắt đầu quét, tắt trọng lực để đống lá không bị tụt xuống sàn lúc thu nhỏ
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Tắt toàn bộ Collider để Raycast không quét trúng nữa
        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (var c in allCols)
        {
            c.enabled = false;
        }

        StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        // 1. Phát tiếng chổi quét lá
        PlaySound(sweepSound != null ? sweepSound : interactSound);

        // 2. Hiệu ứng đống lá co nhỏ lại như bị quét sạch
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        float shrinkDuration = 0.5f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // 3. Tắt toàn bộ MeshRenderer để chắc chắn biến mất hoàn toàn
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (spookyVisualObject != null && (r.gameObject == spookyVisualObject || r.transform.IsChildOf(spookyVisualObject.transform)))
                continue;

            r.enabled = false;
        }

        // 4. Kích hoạt âm thanh ma quái hù dọa (đặc biệt ở đống lá thứ 2)
        if (spookySound != null)
        {
            yield return new WaitForSeconds(0.2f);
            PlaySound(spookySound);
        }

        // 5. Kích hoạt bóng ma lướt qua nếu có
        if (spookyVisualObject != null)
        {
            spookyVisualObject.SetActive(true);
            yield return new WaitForSeconds(1.2f);
            spookyVisualObject.SetActive(false);
        }

        // 6. Báo về TaskManager hoàn thành đống lá này
        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteLeafPile(leafIndex);
        }

        // 7. Tắt hẳn GameObject để dọn dẹp sạch sẽ
        gameObject.SetActive(false);
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
}
