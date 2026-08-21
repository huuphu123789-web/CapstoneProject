using UnityEngine;

/// <summary>
/// Cửa xoay mở/đóng kèm tay nắm cửa (handle) hoạt động.
/// 
/// SETUP trong Unity:
/// 1. Tạo Empty "DoorPivot" → đặt ở VỊ TRÍ BẢN LỀ (mép cửa)
/// 2. Tạo Cube "Door" → kéo vào làm CON của DoorPivot
/// 3. Tạo Empty "HandlePivot" → đặt ở vị trí tay nắm trên cánh cửa, làm CON của Door
/// 4. Tạo model tay nắm → làm CON của HandlePivot
/// 5. Gắn script "SwingDoor" lên object "Door"
/// 6. Kéo "DoorPivot" vào field Pivot, "HandlePivot" vào field Handle Pivot
/// 
/// Hierarchy:
///   DoorFrame
///     ├── DoorPivot  (Empty, vị trí = bản lề)
///     │     └── Door (Cube + BoxCollider + SwingDoor)
///     │           └── HandlePivot (Empty, vị trí tay nắm)
///     │                 └── HandleModel (Mesh tay nắm)
/// 
/// Hoạt động:
///   Bước 1: Nhấn chuột phải → Tay nắm xoay xuống
///   Bước 2: Sau khi tay nắm xoay xong → Cửa bắt đầu mở
///   Bước 3: Khi đóng → Cửa đóng trước, rồi tay nắm trả về
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwingDoor : MonoBehaviour, IInteractable
{
    [Header("=== Xoay Cửa ===")]
    [Tooltip("Kéo object DoorPivot (Empty ở bản lề) vào đây.")]
    [SerializeField] private Transform pivot;

    [Tooltip("Góc mở cửa (độ).")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("Tốc độ mở/đóng cửa.")]
    [SerializeField] private float doorSpeed = 4f;

    [Header("=== Tay Nắm Cửa ===")]
    [Tooltip("Kéo object HandlePivot (Empty ở vị trí tay nắm) vào đây. Để trống nếu không có tay nắm.")]
    [SerializeField] private Transform handlePivot;

    [Tooltip("Góc xoay tay nắm khi nhấn (độ). Mặc định xoay xuống 35 độ quanh trục Z.")]
    [SerializeField] private float handleAngle = 35f;

    [Tooltip("Trục xoay của tay nắm. Mặc định là trục Z (xoay xuống).")]
    [SerializeField] private HandleAxis handleAxis = HandleAxis.Z;

    [Tooltip("Tốc độ xoay tay nắm.")]
    [SerializeField] private float handleSpeed = 8f;

    [Header("=== Âm Thanh (Tùy Chọn) ===")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip handleSound;

    [Header("=== Highlight ===")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);

    // ── Enums ──
    public enum HandleAxis { X, Y, Z }

    // Trạng thái máy trạng thái (state machine)
    private enum DoorState
    {
        Closed,         // Cửa đóng, tay nắm ở vị trí gốc
        HandleOpening,  // Tay nắm đang xoay xuống (chuẩn bị mở cửa)
        DoorOpening,    // Cửa đang mở ra
        Open,           // Cửa đã mở hoàn toàn
        DoorClosing,    // Cửa đang đóng lại
        HandleClosing   // Tay nắm đang trả về vị trí gốc
    }

    // ── IInteractable ──
    public string InteractPrompt => _doorState == DoorState.Open ? "Close Door [Right Click]" : "Open Door [Right Click]";

    // ── Internal ──
    private DoorState _doorState = DoorState.Closed;
    private float _currentDoorAngle;
    private float _targetDoorAngle;
    private float _currentHandleAngle;
    private float _targetHandleAngle;
    private Renderer _renderer;
    private Color _originalColor;
    private AudioSource _audioSource;

    // Ngưỡng coi như đã đến góc mục tiêu
    private const float AngleThreshold = 0.5f;

    private void Awake()
    {
        // Nếu chưa gán pivot, dùng parent
        if (pivot == null)
            pivot = transform.parent;

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && (openSound != null || closeSound != null || handleSound != null))
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (pivot == null) return;

        switch (_doorState)
        {
            case DoorState.HandleOpening:
                UpdateHandle();
                // Khi tay nắm xoay xong → bắt đầu mở cửa
                if (IsAngleReached(_currentHandleAngle, _targetHandleAngle))
                {
                    _targetDoorAngle = openAngle;
                    _doorState = DoorState.DoorOpening;
                    PlaySound(openSound);
                }
                break;

            case DoorState.DoorOpening:
                UpdateDoor();
                // Khi cửa mở xong
                if (IsAngleReached(_currentDoorAngle, _targetDoorAngle))
                {
                    _doorState = DoorState.Open;
                    // Trả tay nắm về vị trí gốc sau khi cửa đã mở
                    _targetHandleAngle = 0f;
                }
                // Trả tay nắm về từ từ trong lúc cửa mở
                UpdateHandle();
                break;

            case DoorState.Open:
                // Trả tay nắm về nếu chưa xong
                UpdateHandle();
                break;

            case DoorState.DoorClosing:
                UpdateDoor();
                // Khi cửa đóng xong → trả tay nắm về
                if (IsAngleReached(_currentDoorAngle, _targetDoorAngle))
                {
                    _doorState = DoorState.HandleClosing;
                    _targetHandleAngle = 0f;
                }
                break;

            case DoorState.HandleClosing:
                UpdateHandle();
                // Khi tay nắm trả về xong
                if (IsAngleReached(_currentHandleAngle, _targetHandleAngle))
                {
                    _doorState = DoorState.Closed;
                }
                break;

            case DoorState.Closed:
                // Không làm gì
                break;
        }
    }

    public void Interact(GameObject interactor)
    {
        switch (_doorState)
        {
            case DoorState.Closed:
                // Bắt đầu mở: tay nắm xoay trước
                if (handlePivot != null)
                {
                    _targetHandleAngle = handleAngle;
                    _doorState = DoorState.HandleOpening;
                    PlaySound(handleSound);
                }
                else
                {
                    // Không có tay nắm → mở cửa luôn
                    _targetDoorAngle = openAngle;
                    _doorState = DoorState.DoorOpening;
                    PlaySound(openSound);
                }
                Debug.Log("[Cửa] Mở cửa!");
                break;

            case DoorState.Open:
                // Bắt đầu đóng: cửa đóng trước
                _targetDoorAngle = 0f;
                _doorState = DoorState.DoorClosing;
                PlaySound(closeSound);
                Debug.Log("[Cửa] Đóng cửa!");
                break;

            // Nếu đang trong quá trình chuyển đổi → bỏ qua input
            default:
                break;
        }
    }

    // ── Cập nhật góc cửa ──
    private void UpdateDoor()
    {
        _currentDoorAngle = Mathf.Lerp(_currentDoorAngle, _targetDoorAngle, doorSpeed * Time.deltaTime);
        pivot.localRotation = Quaternion.Euler(0f, _currentDoorAngle, 0f);
    }

    // ── Cập nhật góc tay nắm ──
    private void UpdateHandle()
    {
        if (handlePivot == null) return;

        _currentHandleAngle = Mathf.Lerp(_currentHandleAngle, _targetHandleAngle, handleSpeed * Time.deltaTime);

        Vector3 euler = Vector3.zero;
        switch (handleAxis)
        {
            case HandleAxis.X:
                euler = new Vector3(_currentHandleAngle, 0f, 0f);
                break;
            case HandleAxis.Y:
                euler = new Vector3(0f, _currentHandleAngle, 0f);
                break;
            case HandleAxis.Z:
                euler = new Vector3(0f, 0f, _currentHandleAngle);
                break;
        }
        handlePivot.localRotation = Quaternion.Euler(euler);
    }

    // ── Kiểm tra đã đến góc mục tiêu chưa ──
    private bool IsAngleReached(float current, float target)
    {
        return Mathf.Abs(current - target) < AngleThreshold;
    }

    // ── Phát âm thanh ──
    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
            _audioSource.PlayOneShot(clip);
    }

    // ── Highlight ──
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
