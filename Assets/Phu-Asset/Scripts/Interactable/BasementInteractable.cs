using UnityEngine;

/// <summary>
/// Script dùng riêng cho Cửa Hầm 2 Cánh (BasementDoor có 2 cánh Left và Right).
/// Kế thừa Interactable (Phú-Asset) - Bấm phím E sẽ tự động mở/đóng 2 cánh cùng lúc.
/// </summary>
public class BasementInteractable : Interactable
{
    public enum Axis { X, Y, Z }

    [Header("=== CÁC CÁNH CỬA ===")]
    [Tooltip("Kéo transform cánh cửa trái (Left) vào đây")]
    public Transform leftDoor;

    [Tooltip("Kéo transform cánh cửa phải (Right) vào đây")]
    public Transform rightDoor;

    [Header("=== GÓC MỞ CỬA ===")]
    [Tooltip("Trục xoay mở cửa (X hoặc Z cho nắp hầm lật, Y cho cửa 2 cánh mở ngang)")]
    public Axis rotationAxis = Axis.X;

    [Tooltip("Góc xoay mở cánh trái (thường là -90 hoặc 90)")]
    public float leftOpenAngle = -90f;

    [Tooltip("Góc xoay mở cánh phải (thường ngược dấu cánh trái, VD: 90)")]
    public float rightOpenAngle = 90f;

    [Tooltip("Tốc độ xoay mở cửa")]
    public float openSpeed = 3f;

    [Header("=== ÂM THANH MỞ CỬA ===")]
    public AudioSource localAudioSource;

    private bool isOpen = false;

    private Quaternion leftClosedRot;
    private Quaternion leftOpenRot;
    private Quaternion rightClosedRot;
    private Quaternion rightOpenRot;

    void Awake()
    {
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Start()
    {
        promptMessage = "Open Door";

        // Tự động tìm 2 cánh con có tên "Left" và "Right" nếu chưa kéo vào Inspector
        if (leftDoor == null && transform.Find("Left") != null)
            leftDoor = transform.Find("Left");

        if (rightDoor == null && transform.Find("Right") != null)
            rightDoor = transform.Find("Right");

        // Lưu góc xoay đóng ban đầu và tính góc mở
        if (leftDoor != null)
        {
            leftClosedRot = leftDoor.localRotation;
            leftOpenRot = leftClosedRot * GetRotationEuler(leftOpenAngle);
        }

        if (rightDoor != null)
        {
            rightClosedRot = rightDoor.localRotation;
            rightOpenRot = rightClosedRot * GetRotationEuler(rightOpenAngle);
        }
    }

    void Update()
    {
        // Xoay mượt mà cả 2 cánh cửa cùng lúc
        if (leftDoor != null)
        {
            Quaternion targetLeft = isOpen ? leftOpenRot : leftClosedRot;
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, targetLeft, Time.deltaTime * openSpeed);
        }

        if (rightDoor != null)
        {
            Quaternion targetRight = isOpen ? rightOpenRot : rightClosedRot;
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, targetRight, Time.deltaTime * openSpeed);
        }
    }

    public override void Interact()
    {
        isOpen = !isOpen;
        promptMessage = isOpen ? "Close Door" : "Open Door";

        PlayDoorSound();

        Debug.Log($"[BasementDoor] Cửa hầm đã {(isOpen ? "MỞ" : "ĐÓNG")}");
    }

    private Quaternion GetRotationEuler(float angle)
    {
        switch (rotationAxis)
        {
            case Axis.X: return Quaternion.Euler(angle, 0f, 0f);
            case Axis.Y: return Quaternion.Euler(0f, angle, 0f);
            case Axis.Z: return Quaternion.Euler(0f, 0f, angle);
            default:     return Quaternion.Euler(0f, angle, 0f);
        }
    }

    private void PlayDoorSound()
    {
        if (interactSound == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(interactSound);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.pitch = Random.Range(0.95f, 1.05f);
            localAudioSource.PlayOneShot(interactSound);
        }
    }
}
