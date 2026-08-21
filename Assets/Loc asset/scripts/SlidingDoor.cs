using UnityEngine;

/// <summary>
/// Cửa trượt đơn giản — dùng khi cửa không tách riêng từ mesh gốc.
/// Tạo 1 Cube làm cánh cửa, gắn script này vào, chuột phải để mở/đóng.
/// 
/// SETUP:
/// 1. Tạo Cube, chỉnh kích thước & vị trí khớp với lỗ cửa của Guard Booth
/// 2. Gắn script "SlidingDoor" lên Cube đó
/// 3. Chỉnh Slide Direction & Slide Distance trong Inspector
/// 4. Đổi material cho giống gỗ (tùy chọn)
/// </summary>
[RequireComponent(typeof(Collider))]
public class SlidingDoor : MonoBehaviour, IInteractable
{
    [Header("=== Trượt Cửa ===")]
    [Tooltip("Hướng trượt khi mở (local space).")]
    [SerializeField] private Vector3 slideDirection = Vector3.right;

    [Tooltip("Khoảng cách trượt khi mở.")]
    [SerializeField] private float slideDistance = 1.2f;

    [Tooltip("Tốc độ mở/đóng.")]
    [SerializeField] private float slideSpeed = 3f;

    [Header("=== Âm Thanh (Tùy Chọn) ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

    // ── IInteractable ──
    public string InteractPrompt => _isOpen ? "Close Door [Right Click]" : "Open Door [Right Click]";

    // ── Internal ──
    private bool _isOpen;
    private Vector3 _closedPos;
    private Vector3 _openPos;
    private Vector3 _targetPos;
    private Renderer _renderer;
    private Color _originalColor;
    private AudioSource _audioSource;

    private void Awake()
    {
        _closedPos = transform.localPosition;
        _openPos = _closedPos + transform.TransformDirection(slideDirection.normalized) * slideDistance;
        // Dùng world direction thay vì local nếu cần:
        // _openPos = _closedPos + slideDirection.normalized * slideDistance;
        _targetPos = _closedPos;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && (openSound != null || closeSound != null))
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        // Di chuyển mượt tới vị trí mục tiêu
        if (Vector3.Distance(transform.localPosition, _targetPos) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                _targetPos,
                slideSpeed * Time.deltaTime
            );
        }
    }

    public void Interact(GameObject interactor)
    {
        _isOpen = !_isOpen;
        _targetPos = _isOpen ? _openPos : _closedPos;

        // Phát âm thanh
        if (_audioSource != null)
        {
            AudioClip clip = _isOpen ? openSound : closeSound;
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        Debug.Log($"[Cửa] {(_isOpen ? "Mở" : "Đóng")} cửa trượt!");
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

    /// <summary>
    /// Vẽ hướng trượt trong Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 from = transform.position;
        Vector3 dir = transform.TransformDirection(slideDirection.normalized) * slideDistance;
        Gizmos.DrawLine(from, from + dir);
        Gizmos.DrawWireSphere(from + dir, 0.1f);
    }
}
