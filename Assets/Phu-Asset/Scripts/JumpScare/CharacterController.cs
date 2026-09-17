using UnityEngine;
using UnityEngine.InputSystem; // B? dòng này n?u dùng Input c?

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    // Thêm bi?n này ?? ki?m soát vi?c cho phép di chuy?n hay không
    public bool canMove = true;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 0.1f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // V?n áp d?ng tr?ng l?c gi? Player trên sàn
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // CH? cho phép xoay chu?t & di chuy?n khi canMove = true
        if (canMove)
        {
            HandleLook();
            HandleMove();
        }

        // Áp d?ng tr?ng l?c liên t?c ?? KHÔNG b? r?i l?t sàn
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        if (Mouse.current == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMove()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move.normalized * moveSpeed * Time.deltaTime);
    }
}