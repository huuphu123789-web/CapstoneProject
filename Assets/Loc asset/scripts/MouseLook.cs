using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Xoay camera FPS bằng chuột. Con trỏ bị khóa khi chơi.
/// Gắn script này vào CÙNG GameObject với PlayerMovement (player root).
/// Camera phải là con (child) của player.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("=== Độ Nhạy ===")]
    [SerializeField] private float sensitivity = 2.5f;
    [SerializeField] private float smoothing = 1.5f;

    [Header("=== Giới Hạn Góc Nhìn ===")]
    [SerializeField] private float minPitch = -85f;   // Nhìn xuống tối đa
    [SerializeField] private float maxPitch = 85f;     // Nhìn lên tối đa

    [Header("=== Tham Chiếu ===")]
    [Tooltip("Kéo Camera vào đây. Nếu để trống, tự tìm Camera con.")]
    [SerializeField] private Transform cameraTransform;

    // ── Internal ──
    private float _xRotation;      // Pitch (lên/xuống) — áp dụng cho Camera
    private float _yRotation;      // Yaw   (trái/phải) — áp dụng cho Player root
    private float _smoothX;
    private float _smoothY;

    private void Awake()
    {
        // Tự tìm Camera nếu chưa gán
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogError("[MouseLook] Không tìm thấy Camera! Hãy gắn Camera là con của Player.");
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        HandleCursorLock();
        HandleRotation();
    }

    /// <summary>
    /// Xử lý xoay camera dựa trên input chuột.
    /// Player root xoay ngang (Yaw), Camera con xoay dọc (Pitch).
    /// </summary>
    private void HandleRotation()
    {
        // Lấy input chuột từ New Input System
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        float mouseX = mouseDelta.x * sensitivity * 0.1f;
        float mouseY = mouseDelta.y * sensitivity * 0.1f;

        // Làm mượt (interpolation)
        _smoothX = Mathf.Lerp(_smoothX, mouseX, 1f / smoothing);
        _smoothY = Mathf.Lerp(_smoothY, mouseY, 1f / smoothing);

        // Cộng dồn góc xoay
        _yRotation += _smoothX;
        _xRotation -= _smoothY;

        // Clamp góc nhìn dọc để không bị lộn ngược
        _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

        // Áp dụng xoay
        // Player root: chỉ xoay trục Y (quay trái/phải)
        transform.rotation = Quaternion.Euler(0f, _yRotation, 0f);

        // Camera: chỉ xoay trục X (nhìn lên/xuống)
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }

    /// <summary>
    /// Nhấn Escape để mở khóa chuột, click để khóa lại.
    /// </summary>
    private void HandleCursorLock()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            UnlockCursor();

        if (Cursor.lockState == CursorLockMode.None && mouse != null && mouse.leftButton.wasPressedThisFrame)
            LockCursor();
    }

    /// <summary>
    /// Khóa con trỏ chuột vào giữa màn hình.
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Mở khóa con trỏ chuột.
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Thay đổi độ nhạy chuột từ code hoặc UI Settings.
    /// </summary>
    public void SetSensitivity(float newSens)
    {
        sensitivity = Mathf.Clamp(newSens, 0.1f, 20f);
    }
}
