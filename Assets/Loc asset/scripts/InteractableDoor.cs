using UnityEngine;
/// <summary>
/// Ví dụ vật thể tương tác: Mở/đóng cửa khi chuột phải.
/// Gắn script này lên GameObject cửa (có Collider).
/// </summary>
public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("=== Cài Đặt Cửa ===")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private Transform pivot; // Trục xoay cửa (nếu khác transform gốc)
    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);
    // ── IInteractable ──
    public string InteractPrompt => _isOpen ? "Đóng cửa" : "Mở cửa";
    // ── Internal ──
    private bool _isOpen;
    private float _targetAngle;
    private float _currentAngle;
    private Renderer _renderer;
    private Color _originalColor;
    private Transform _pivotTransform;
    private void Awake()
    {
        _pivotTransform = pivot != null ? pivot : transform;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }
    private void Update()
    {
        // Xoay mượt tới góc mục tiêu
        _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, Time.deltaTime * openSpeed);
        _pivotTransform.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
    }
    public void Interact(GameObject interactor)
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f; 
        Debug.Log($"[Cửa] {(_isOpen ? "Mở" : "Đóng")} cửa!");
    }
    public void OnLookAt()
    {
        // Highlight khi nhìn vào
        if (_renderer != null)
            _renderer.material.color = highlightColor;
    }
    public void OnLookAway()
    {
        // Trả về màu gốc
        if (_renderer != null)
            _renderer.material.color = _originalColor;
    }
}
