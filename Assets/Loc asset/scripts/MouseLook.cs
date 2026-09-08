using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Xoay camera FPS báº±ng chuá»™t. Con trá» bá»‹ khÃ³a khi chÆ¡i.
/// Gáº¯n script nÃ y vÃ o CÃ™NG GameObject vá»›i PlayerMovement (player root).
/// Camera pháº£i lÃ  con (child) cá»§a player.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("=== Äá»™ Nháº¡y ===")]
    [SerializeField] private float sensitivity = 2.5f;
    [SerializeField] private float smoothing = 1.5f;

    [Header("=== Giá»›i Háº¡n GÃ³c NhÃ¬n ===")]
    [SerializeField] private float minPitch = -85f;   // NhÃ¬n xuá»‘ng tá»‘i Ä‘a
    [SerializeField] private float maxPitch = 85f;     // NhÃ¬n lÃªn tá»‘i Ä‘a

    [Header("=== Tham Chiáº¿u ===")]
    [Tooltip("KÃ©o Camera vÃ o Ä‘Ã¢y. Náº¿u Ä‘á»ƒ trá»‘ng, tá»± tÃ¬m Camera con.")]
    [SerializeField] private Transform cameraTransform;

    // --- Internal ---
    private float _xRotation;      // Pitch (lÃªn/xuá»‘ng) - Ã¡p dá»¥ng cho Camera
    private float _yRotation;      // Yaw   (trÃ¡i/pháº£i) - Ã¡p dá»¥ng cho Player root
    private float _smoothX;
    private float _smoothY;

    private void Awake()
    {
        // Tá»± tÃ¬m Camera náº¿u chÆ°a gÃ¡n
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogError("[MouseLook] KhÃ´ng tÃ¬m tháº¥y Camera! HÃ£y gáº¯n Camera lÃ  con cá»§a Player.");
        }
    }

    private void Start()
    {
        LockCursor();

        // Khá»Ÿi táº¡o gÃ³c quay ban Ä‘áº§u theo Ä‘Ãºng hÆ°á»›ng cá»§a Player trong Scene (trÃ¡nh bá»‹ giáº­t hÆ°á»›ng)
        _yRotation = transform.eulerAngles.y;
        if (cameraTransform != null)
        {
            float pitch = cameraTransform.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            _xRotation = pitch;
        }
    }

    private void Update()
    {
        HandleCursorLock();
        HandleRotation();
    }

    /// <summary>
    /// Xá»­ lÃ½ xoay camera dá»±a trÃªn input chuá»™t.
    /// Player root xoay ngang (Yaw), Camera con xoay dá»c (Pitch).
    /// </summary>
    private void HandleRotation()
    {
        // Láº¥y input chuá»™t tá»« New Input System
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        float mouseX = mouseDelta.x * sensitivity * 0.1f;
        float mouseY = mouseDelta.y * sensitivity * 0.1f;

        // LÃ m mÆ°á»£t (interpolation)
        _smoothX = Mathf.Lerp(_smoothX, mouseX, 1f / smoothing);
        _smoothY = Mathf.Lerp(_smoothY, mouseY, 1f / smoothing);

        // Cá»™ng dá»“n gÃ³c xoay
        _yRotation += _smoothX;
        _xRotation -= _smoothY;

        // Clamp gÃ³c nhÃ¬n dá»c Ä‘á»ƒ khÃ´ng bá»‹ lá»™n ngÆ°á»£c
        _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

        // Ãp dá»¥ng xoay
        // Player root: chá»‰ xoay trá»¥c Y (quay trÃ¡i/pháº£i)
        transform.rotation = Quaternion.Euler(0f, _yRotation, 0f);

        // Camera: chá»‰ xoay trá»¥c X (nhÃ¬n lÃªn/xuá»‘ng)
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }

    /// <summary>
    /// Nháº¥n Escape Ä‘á»ƒ má»Ÿ khÃ³a chuá»™t, click Ä‘á»ƒ khÃ³a láº¡i.
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
    /// KhÃ³a con trá» chuá»™t vÃ o giá»¯a mÃ n hÃ¬nh.
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Má»Ÿ khÃ³a con trá» chuá»™t.
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Thay Ä‘á»•i Ä‘á»™ nháº¡y chuá»™t tá»« code hoáº·c UI Settings.
    /// </summary>
    public void SetSensitivity(float newSens)
    {
        sensitivity = Mathf.Clamp(newSens, 0.1f, 20f);
    }
}