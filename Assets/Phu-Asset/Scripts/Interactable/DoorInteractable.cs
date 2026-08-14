using UnityEngine;

public class DoorInteractable : Interactable
{
    [Header("=== Kéo Hinge / Cánh Cửa Vào Đây ===")]
    [Tooltip("Kéo Booth Door Hinge vào đây")]
    public Transform doorHinge;

    [Header("=== Góc Mở Cửa (Trục Z) ===")]
    [Tooltip("Góc xoay mở cửa theo Trục Z (thử 90 hoặc -90 nếu muốn đổi hướng mở)")]
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

        // Lưu góc xoay đóng ban đầu
        closedRotation = doorHinge.localRotation;
        
        // Tính góc mở theo Trục Z
        openRotation = closedRotation * Quaternion.Euler(0f, 0f, openAngleZ);

        promptMessage = "Mở cửa";
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
        promptMessage = isOpen ? "Đóng cửa" : "Mở cửa";

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