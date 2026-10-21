using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Di chuyển nhân vật FPS cơ bản: WASD, chạy nhanh (Shift), nhảy (Space), trọng lực.
/// Gắn script này vào GameObject có CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== Tốc Độ ===")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("=== Nhảy & Trọng Lực ===")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("=== Ngồi Xuống ===")]
    [SerializeField] private float crouchCameraOffset = -0.6f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("=== Hiệu Ứng ===")]
    [SerializeField] private float headBobFrequency = 8f;
    [SerializeField] private float headBobAmplitude = 0.04f;

    // ── Internal ──
    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isCrouching;
    private float _cameraBaseY;
    private float _targetCamY;
    private float _headBobTimer;
    private Transform _cameraHolder;

    // ── Public Properties ──
    public bool IsGrounded => _isGrounded;
    public bool IsSprinting
    {
        get
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.leftShiftKey.isPressed && _isGrounded && !_isCrouching;
        }
    }
    public bool IsCrouching => _isCrouching;
    public float CurrentSpeed => _controller.velocity.magnitude;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // Tìm camera con (nếu có) để làm head bob
        _cameraHolder = GetComponentInChildren<Camera>()?.transform;
    }

    private void Update()
    {
        CheckGround();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        ApplyHeadBob();
    }

    /// <summary>
    /// Kiểm tra chạm đất bằng SphereCast từ chân nhân vật.
    /// </summary>
    private void CheckGround()
    {
        Vector3 sphereOrigin = transform.position + Vector3.down * (_controller.height / 2f - _controller.radius + 0.05f);
        _isGrounded = Physics.SphereCast(
            sphereOrigin, _controller.radius * 0.9f,
            Vector3.down, out _, groundCheckDistance, groundMask
        );

        // Reset velocity khi chạm đất
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    /// <summary>
    /// Di chuyển WASD dựa theo hướng nhìn của nhân vật.
    /// </summary>
    private void HandleMovement()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f;
        float v = 0f;

        if (kb.aKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed) h += 1f;
        if (kb.wKey.isPressed) v += 1f;
        if (kb.sKey.isPressed) v -= 1f;

        Vector3 direction = (transform.right * h + transform.forward * v).normalized;

        float speed = _isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

        _controller.Move(direction * speed * Time.deltaTime);
    }

    /// <summary>
    /// Nhảy khi nhấn Space và đang đứng trên mặt đất.
    /// </summary>
    private void HandleJump()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame && _isGrounded && !_isCrouching)
        {
            _velocity.y = jumpForce;
        }
    }

    /// <summary>
    /// Áp dụng trọng lực mỗi frame.
    /// </summary>
    private void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// Ngồi xuống / đứng lên bằng phím C (hoặc Left Ctrl).
    /// Chỉ hạ/nâng camera, không thay đổi CharacterController.
    /// </summary>
    private void HandleCrouch()
    {
        if (_cameraHolder == null) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Lưu vị trí Y gốc của camera lần đầu
        if (_cameraBaseY == 0f)
            _cameraBaseY = _cameraHolder.localPosition.y;

        if (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame)
            _isCrouching = !_isCrouching;

        // Tính vị trí Y mục tiêu
        _targetCamY = _isCrouching ? _cameraBaseY + crouchCameraOffset : _cameraBaseY;

        // Hạ/nâng camera mượt
        Vector3 pos = _cameraHolder.localPosition;
        pos.y = Mathf.Lerp(pos.y, _targetCamY, crouchTransitionSpeed * Time.deltaTime);
        _cameraHolder.localPosition = pos;
    }

    /// <summary>
    /// Hiệu ứng lắc đầu khi đi bộ.
    /// </summary>
    private void ApplyHeadBob()
    {
        if (_cameraHolder == null) return;

        if (_isGrounded && CurrentSpeed > 0.5f)
        {
            float freq = IsSprinting ? headBobFrequency * 1.3f : headBobFrequency;
            float amp = IsSprinting ? headBobAmplitude * 1.4f : headBobAmplitude;

            _headBobTimer += Time.deltaTime * freq;
            float bobY = Mathf.Sin(_headBobTimer) * amp;
            float bobX = Mathf.Sin(_headBobTimer * 0.5f) * amp * 0.5f;

            Vector3 localPos = _cameraHolder.localPosition;
            _cameraHolder.localPosition = new Vector3(bobX, localPos.y + (bobY - localPos.y) * 0.1f, localPos.z);
        }
        else
        {
            _headBobTimer = 0f;
            Vector3 localPos = _cameraHolder.localPosition;
            _cameraHolder.localPosition = Vector3.Lerp(localPos, new Vector3(0, localPos.y, localPos.z), 5f * Time.deltaTime);
        }
    }
}
