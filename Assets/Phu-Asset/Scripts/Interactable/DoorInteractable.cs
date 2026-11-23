using UnityEngine;

public class DoorInteractable : Interactable
{
    public enum RotationAxis { X, Y, Z }

    [Header("=== Kéo Hinge / Pivot Cánh Cửa Vào Đây ===")]
    [Tooltip("Kéo Booth Door Hinge hoặc DoorPivot vào đây")]
    public Transform doorHinge;

    [Header("=== Góc Mở Cửa ===")]
    [Tooltip("Trục xoay mở cửa (Y cho DoorPivot thẳng đứng, Z cho model Blender xoay trục Z)")]
    public RotationAxis rotationAxis = RotationAxis.Z;

    [Tooltip("Góc xoay mở cửa (thử 90 hoặc -90 nếu muốn đổi hướng mở)")]
    public float openAngleZ = 90f;
    public float openSpeed = 4f;

    [Header("=== Âm Thanh ===")]
    public AudioSource localAudioSource;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

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
        if (doorHinge == null) doorHinge = transform;

        // Nếu DoorPivot nằm ngang hàng (chưa là cha của Door), tự động gắn Door làm con của DoorPivot
        if (doorHinge != transform && !transform.IsChildOf(doorHinge))
        {
            transform.SetParent(doorHinge, true);
        }

        // Lưu góc xoay đóng ban đầu
        closedRotation = doorHinge.localRotation;

        // Tính góc mở theo trục đã chọn
        Vector3 eulerOffset = Vector3.zero;
        switch (rotationAxis)
        {
            case RotationAxis.X: eulerOffset = new Vector3(openAngleZ, 0f, 0f); break;
            case RotationAxis.Y: eulerOffset = new Vector3(0f, openAngleZ, 0f); break;
            default:             eulerOffset = new Vector3(0f, 0f, openAngleZ); break;
        }
        openRotation = closedRotation * Quaternion.Euler(eulerOffset);

        promptMessage = "Open Door";
    }

    void Update()
    {
        if (doorHinge == null) return;

        // Xoay mượt theo trục Z
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, Time.deltaTime * openSpeed);
    }

    public override void Interact()
    {
        PlayDoorSound();

        isOpen = !isOpen;
        promptMessage = isOpen ? "Close Door" : "Open Door";

        Debug.Log($"[Door] Cửa đã {(isOpen ? "MỞ" : "ĐÓNG")} (Góc Z: {(isOpen ? openAngleZ : 0)})");
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