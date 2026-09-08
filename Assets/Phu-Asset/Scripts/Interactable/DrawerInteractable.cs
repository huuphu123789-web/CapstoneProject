using UnityEngine;

/// <summary>
/// Script dùng cho Hộc Tủ / Ngăn Kéo (Drawer) hoặc Tủ Kéo.
/// Kế thừa Interactable (Phú-Asset) - Nhấn phím E để kéo ra / đẩy vào mượt mà!
/// </summary>
public class DrawerInteractable : Interactable
{
    public enum SlideAxis { Forward_Z, Right_X, Up_Y, Backward_NegativeZ, Left_NegativeX }

    [Header("=== CÀI ĐẶT TRƯỢT HỘC TỦ ===")]
    [Tooltip("Hướng kéo hộc tủ ra ngoài theo trục Local")]
    public SlideAxis slideAxis = SlideAxis.Forward_Z;

    [Tooltip("Khoảng cách kéo hộc tủ ra ngoài (mét, thường khoảng 0.3 - 0.5)")]
    public float slideDistance = 0.4f;

    [Tooltip("Tốc độ kéo trượt")]
    public float slideSpeed = 4f;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh khi mở hộc tủ (nếu để trống sẽ dùng interactSound)")]
    public AudioClip openSound;
    [Tooltip("Âm thanh khi đóng hộc tủ")]
    public AudioClip closeSound;
    public AudioSource localAudioSource;

    private bool isOpen = false;
    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;
    private Vector3 targetLocalPos;

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
        promptMessage = "Open Drawer";

        // Lưu vị trí đóng ban đầu (Local position)
        closedLocalPos = transform.localPosition;

        // Tính vị trí mở theo hướng trục được chọn
        Vector3 slideDir = GetSlideDirectionVector();
        openLocalPos = closedLocalPos + slideDir * slideDistance;

        targetLocalPos = closedLocalPos;
    }

    void Update()
    {
        // Di chuyển mượt mà tới vị trí mục tiêu
        if (Vector3.Distance(transform.localPosition, targetLocalPos) > 0.0005f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetLocalPos,
                Time.deltaTime * slideSpeed
            );
        }
    }

    public override void Interact()
    {
        isOpen = !isOpen;
        targetLocalPos = isOpen ? openLocalPos : closedLocalPos;
        promptMessage = isOpen ? "Close Drawer" : "Open Drawer";

        PlayDrawerSound(isOpen);

        Debug.Log($"[Drawer] {(isOpen ? "Mở" : "Đóng")} hộc tủ: {gameObject.name}");
    }

    private Vector3 GetSlideDirectionVector()
    {
        switch (slideAxis)
        {
            case SlideAxis.Forward_Z:           return Vector3.forward;
            case SlideAxis.Backward_NegativeZ:  return Vector3.back;
            case SlideAxis.Right_X:             return Vector3.right;
            case SlideAxis.Left_NegativeX:      return Vector3.left;
            case SlideAxis.Up_Y:                return Vector3.up;
            default:                            return Vector3.forward;
        }
    }

    private void PlayDrawerSound(bool opening)
    {
        AudioClip clipToPlay = opening ? (openSound != null ? openSound : interactSound) : (closeSound != null ? closeSound : interactSound);

        if (clipToPlay == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clipToPlay);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.pitch = Random.Range(0.95f, 1.05f);
            localAudioSource.PlayOneShot(clipToPlay);
        }
    }

    // Vẽ đường màu xanh trong Scene view để bạn dễ thấy trước hướng hộc tủ sẽ trượt
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 fromPos = transform.position;
        Vector3 dir = transform.TransformDirection(GetSlideDirectionVector()) * slideDistance;
        Gizmos.DrawLine(fromPos, fromPos + dir);
        Gizmos.DrawWireSphere(fromPos + dir, 0.05f);
    }
}
