using UnityEngine;
/// <summary>
/// Ví dụ vật thể tương tác: Nhặt vật phẩm khi chuột phải.
/// Gắn script này lên vật phẩm có Collider.
/// </summary>
public class InteractablePickup : MonoBehaviour, IInteractable
{
    [Header("=== Vật Phẩm ===")]
    [SerializeField] private string itemName = "Vật phẩm";
    [SerializeField] private int quantity = 1;
    [Header("=== Hiệu Ứng ===")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float rotateSpeed = 60f;
    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.7f, 1f);
    // ── IInteractable ──
    public string InteractPrompt => $"Nhặt {itemName} (x{quantity})";
    // ── Internal ──
    private Vector3 _startPos;
    private Renderer _renderer;
    private Color _originalColor;
    private bool _isHighlighted;
    private void Awake()
    {
        _startPos = transform.position;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }
    private void Update()
    {
        // Bay lên xuống và xoay liên tục
        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(_startPos.x, y, _startPos.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[Pickup] Nhặt được: {itemName} x{quantity}");
        // TODO: Thêm vào Inventory của player ở đây
        // var inventory = interactor.GetComponent<Inventory>();
        // inventory?.AddItem(itemName, quantity);
        // Hủy vật phẩm sau khi nhặt
        Destroy(gameObject);
    }
    public void OnLookAt()
    {
        if (_renderer != null && !_isHighlighted)
        {
            _renderer.material.color = highlightColor;
            _isHighlighted = true;
        }
    }
    public void OnLookAway()
    {
        if (_renderer != null && _isHighlighted)
        {
            _renderer.material.color = _originalColor;
            _isHighlighted = false;
        }
    }
}
