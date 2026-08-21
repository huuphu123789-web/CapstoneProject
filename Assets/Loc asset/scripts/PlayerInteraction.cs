using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tương tác bằng chuột phải: Raycast từ camera, phát hiện vật thể có IInteractable.
/// Gắn script này cùng chỗ với PlayerMovement.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("=== Tương Tác ===")]
    [SerializeField] private float interactRange = 4f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("=== UI Crosshair ===")]
    [Tooltip("Nếu muốn đổi crosshair khi nhìn vào vật tương tác, gắn UI Image vào đây.")]
    [SerializeField] private GameObject interactPromptUI;

    // ── Internal ──
    private Camera _cam;
    private IInteractable _currentTarget;

    // ── Public ──
    /// <summary>Vật thể đang được nhắm tới (null nếu không có).</summary>
    public IInteractable CurrentTarget => _currentTarget;

    private void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null)
            Debug.LogError("[PlayerInteraction] Không tìm thấy Camera con!");
    }

    private void Update()
    {
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
        {
            if (_currentTarget != null) { _currentTarget.OnLookAway(); _currentTarget = null; }
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
        {
            if (_currentTarget != null) { _currentTarget.OnLookAway(); _currentTarget = null; }
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        DetectInteractable();
        HandleInteractInput();
    }

    /// <summary>
    /// Bắn Raycast từ giữa camera, tìm vật có IInteractable.
    /// </summary>
    private void DetectInteractable()
    {
        IInteractable detected = null;

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            // Tìm IInteractable trên object bị trúng (hoặc parent)
            detected = hit.collider.GetComponentInParent<IInteractable>();
        }

        // Xử lý highlight khi nhìn vào / nhìn đi
        if (detected != _currentTarget)
        {
            // Bỏ highlight cũ
            _currentTarget?.OnLookAway();

            // Highlight mới
            _currentTarget = detected;
            _currentTarget?.OnLookAt();
        }

        // Cập nhật UI prompt
        if (interactPromptUI != null)
            interactPromptUI.SetActive(_currentTarget != null);
    }

    /// <summary>
    /// Khi nhấn chuột phải và đang nhắm vào vật tương tác → gọi Interact().
    /// </summary>
    private void HandleInteractInput()
    {
        if (_currentTarget == null) return;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            _currentTarget.Interact(gameObject);
        }
    }

    /// <summary>
    /// Vẽ ray debug trong Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_cam.transform.position, _cam.transform.forward * interactRange);
    }
}
