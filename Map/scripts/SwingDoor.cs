using UnityEngine;

/// <summary>
/// Cửa xoay mở/đóng — tạo Cube làm cánh cửa, dùng Empty làm bản lề.
/// 
/// SETUP trong Unity:
/// 1. Tạo Empty GameObject tên "DoorPivot" → đặt ở VỊ TRÍ BẢN LỀ (mép cửa)
/// 2. Tạo Cube tên "Door" → kéo vào làm CON của DoorPivot
/// 3. Chỉnh Door position/scale cho khớp khung cửa (offset sang 1 bên so với pivot)
/// 4. Gắn script "SwingDoor" lên object "Door"
/// 5. Kéo "DoorPivot" vào field Pivot trong Inspector
/// 
/// Hierarchy:
///   Guard Booth
///     ├── DoorPivot  (Empty, vị trí = bản lề)
///     │     └── Door (Cube + BoxCollider + SwingDoor)
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwingDoor : MonoBehaviour, IInteractable
{
    [Header("=== Xoay Cửa ===")]
    [Tooltip("Kéo object DoorPivot (Empty ở bản lề) vào đây.")]
    [SerializeField] private Transform pivot;

    [Tooltip("Góc mở cửa (độ).")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("Tốc độ mở/đóng.")]
    [SerializeField] private float speed = 4f;

    [Header("=== Âm Thanh (Tùy Chọn) ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

    // ── IInteractable ──
    public string InteractPrompt => _isOpen ? "Đóng cửa [Chuột phải]" : "Mở cửa [Chuột phải]";

    // ── Internal ──
    private bool _isOpen;
    private float _currentAngle;
    private float _targetAngle;
    private Renderer _renderer;
    private Color _originalColor;
    private AudioSource _audioSource;

    private void Awake()
    {
        // Nếu chưa gán pivot, dùng parent
        if (pivot == null)
            pivot = transform.parent;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && (openSound != null || closeSound != null))
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (pivot == null) return;

        // Xoay mượt tới góc mục tiêu
        _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, speed * Time.deltaTime);
        pivot.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
    }

    public void Interact(GameObject interactor)
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f;

        // Phát âm thanh
        if (_audioSource != null)
        {
            AudioClip clip = _isOpen ? openSound : closeSound;
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        Debug.Log($"[Cửa] {(_isOpen ? "Mở" : "Đóng")} cửa!");
    }

    public void OnLookAt()
    {
        if (_renderer != null)
            _renderer.material.color = highlightColor;
    }

    public void OnLookAway()
    {
        if (_renderer != null)
            _renderer.material.color = _originalColor;
    }
}
