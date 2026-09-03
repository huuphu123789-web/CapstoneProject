using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Di chuyá»ƒn nhÃ¢n váº­t FPS: WASD, cháº¡y nhanh (Shift), nháº£y (Space), trá»ng lá»±c chuáº©n Unity.
/// Tá»± Ä‘á»™ng Ä‘á»“ng bá»™ va cháº¡m vá»›i CharacterController, chá»‘ng rÆ¡i xuyÃªn Ä‘áº¥t.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== Tá»‘c Äá»™ ===")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("=== Nháº£y vÃ  Trá»ng Lá»±c ===")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("=== Ngá»“i Xuá»‘ng ===")]
    [SerializeField] private float crouchCameraOffset = -0.6f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("=== Hiá»‡u á»¨ng ===")]
    [SerializeField] private float headBobFrequency = 8f;
    [SerializeField] private float headBobAmplitude = 0.04f;

    // --- Internal ---
    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isCrouching;
    private Transform _cameraTransform;
    private Vector3 _initialCameraLocalPos;
    private float _crouchYOffset;
    private float _headBobTimer;

    // --- Public Properties ---
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
    public float CurrentSpeed => _controller != null ? _controller.velocity.magnitude : 0f;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // Tá»± Ä‘á»™ng Ä‘áº·t chiá»u cao CharacterController thÃ nh 2m náº¿u Ä‘ang lÃ  1m (ngÄƒn lÃºn ná»­a ngÆ°á»i xuá»‘ng Ä‘áº¥t)
        if (_controller != null && _controller.height < 1.9f)
        {
            _controller.height = 2f;
            _controller.center = Vector3.zero;
        }

        // Tá»± Ä‘á»™ng táº¯t BoxCollider thá»«a trÃªn ngÆ°á»i Player náº¿u cÃ³ (trÃ¡nh xung Ä‘á»™t váº­t lÃ½ vá»›i CharacterController)
        BoxCollider extraBox = GetComponent<BoxCollider>();
        if (extraBox != null && !extraBox.isTrigger)
        {
            extraBox.enabled = false;
        }

        // TÃ¬m Camera con Ä‘á»ƒ xá»­ lÃ½ Ngá»“i vÃ  HeadBob
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            _cameraTransform = cam.transform;
            _initialCameraLocalPos = _cameraTransform.localPosition;
        }
    }

    private void Update()
    {
        CheckGround();
        HandleCrouch();
        HandleJump();
        ApplyMovementAndGravity();
        ApplyHeadBob();
    }

    /// <summary>
    /// Kiá»ƒm tra tiáº¿p Ä‘áº¥t káº¿t há»£p: CharacterController.isGrounded + Raycast Ä‘Ã¡y bÃ n chÃ¢n.
    /// </summary>
    private void CheckGround()
    {
        // 1. Kiá»ƒm tra báº±ng cÆ¡ cháº¿ chuáº©n cá»§a CharacterController
        if (_controller.isGrounded)
        {
            _isGrounded = true;
        }
        else
        {
            // 2. Dá»± phÃ²ng báº±ng Raycast tá»« Ä‘iá»ƒm chÃ¢n thá»±c táº¿ cá»§a nhÃ¢n váº­t
            Vector3 footPos = transform.position + _controller.center + Vector3.down * (_controller.height * 0.5f);
            RaycastHit hit;
            if (Physics.Raycast(footPos + Vector3.up * 0.15f, Vector3.down, out hit, groundCheckDistance + 0.35f, groundMask, QueryTriggerInteraction.Ignore))
            {
                // Bá» qua náº¿u raycast va trÃºng chÃ­nh Player hoáº·c con cá»§a Player
                if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                {
                    _isGrounded = true;
                }
                else
                {
                    _isGrounded = false;
                }
            }
            else
            {
                _isGrounded = false;
            }
        }

        // Khi Ä‘ang á»Ÿ trÃªn máº·t Ä‘áº¥t, giá»¯ má»™t lá»±c Ä‘Ã¨ nháº¹ xuá»‘ng (-2m/s) Ä‘á»ƒ controller luÃ´n bÃ¡m Ä‘áº¥t
        if (_isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
        }
    }

    /// <summary>
    /// Xá»­ lÃ½ di chuyá»ƒn WASD káº¿t há»£p trá»ng lá»±c trong 1 láº§n Move duy nháº¥t (trÃ¡nh giáº­t/xuyÃªn sÃ n).
    /// </summary>
    private void ApplyMovementAndGravity()
    {
        Keyboard kb = Keyboard.current;
        Vector3 moveDirection = Vector3.zero;

        if (kb != null)
        {
            float h = 0f;
            float v = 0f;

            if (kb.aKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed) h += 1f;
            if (kb.wKey.isPressed) v += 1f;
            if (kb.sKey.isPressed) v -= 1f;

            moveDirection = (transform.right * h + transform.forward * v).normalized;
        }

        float speed = _isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

        // Ãp dá»¥ng gia tá»‘c trá»ng lá»±c khi Ä‘ang á»Ÿ trÃªn khÃ´ng
        if (!_isGrounded)
        {
            _velocity.y += gravity * Time.deltaTime;
            // Giá»›i háº¡n tá»‘c Ä‘á»™ rÆ¡i tá»‘i Ä‘a (terminal velocity) Ä‘á»ƒ KHÃ”NG BAO GIá»œ bá»‹ rÆ¡i xuyÃªn qua collider (tunneling)
            _velocity.y = Mathf.Max(_velocity.y, -25f);
        }

        // Káº¿t há»£p di chuyá»ƒn pháº³ng vÃ  rÆ¡i dá»c
        Vector3 finalMove = (moveDirection * speed) + (Vector3.up * _velocity.y);
        _controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>
    /// Nháº£y khi nháº¥n Space vÃ  Ä‘ang Ä‘á»©ng trÃªn máº·t Ä‘áº¥t.
    /// </summary>
    private void HandleJump()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame && _isGrounded && !_isCrouching)
        {
            _velocity.y = jumpForce;
            _isGrounded = false;
        }
    }

    /// <summary>
    /// Ngá»“i xuá»‘ng / Ä‘á»©ng lÃªn báº±ng phÃ­m C hoáº·c Left Ctrl.
    /// </summary>
    private void HandleCrouch()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame)
                _isCrouching = !_isCrouching;
        }

        float targetOffset = _isCrouching ? crouchCameraOffset : 0f;
        _crouchYOffset = Mathf.Lerp(_crouchYOffset, targetOffset, crouchTransitionSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Hiá»‡u á»©ng láº¯c Ä‘áº§u (HeadBob) mÆ°á»£t mÃ  dá»±a trÃªn localPosition ban Ä‘áº§u cá»§a Camera.
    /// </summary>
    private void ApplyHeadBob()
    {
        if (_cameraTransform == null) return;

        float bobX = 0f;
        float bobY = 0f;

        if (_isGrounded && CurrentSpeed > 0.5f)
        {
            float freq = IsSprinting ? headBobFrequency * 1.3f : headBobFrequency;
            float amp = IsSprinting ? headBobAmplitude * 1.4f : headBobAmplitude;

            _headBobTimer += Time.deltaTime * freq;
            bobY = Mathf.Sin(_headBobTimer) * amp;
            bobX = Mathf.Cos(_headBobTimer * 0.5f) * amp * 0.5f;
        }
        else
        {
            _headBobTimer = 0f;
        }

        Vector3 targetLocalPos = new Vector3(
            _initialCameraLocalPos.x + bobX,
            _initialCameraLocalPos.y + _crouchYOffset + bobY,
            _initialCameraLocalPos.z
        );

        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, targetLocalPos, 12f * Time.deltaTime);
    }
}