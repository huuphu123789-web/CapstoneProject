using System.Collections;
using UnityEngine;

/// <summary>
/// Script điều khiển cánh cửa Hideout (hoặc bất kỳ cánh cửa nào).
/// Kế thừa Interactable (Phú-Asset) - Tương thích hoàn toàn với hệ thống bấm [E].
/// 
/// ĐẶC BIỆT: Tự động khắc phục lỗi tâm (Pivot/Origin) lệch của model 3D FBX
/// bằng cách tự tạo một bản lề (Hinge Pivot) tại mép BoxCollider lúc chạy game!
/// </summary>
public class HideoutDoorInteractable : Interactable
{
    public enum HingeMode
    {
        AutoHingeFromCollider, // Tự động tính vị trí mép cửa từ BoxCollider để làm bản lề
        CustomHinge,           // Dùng Empty GameObject bản lề kéo thả thủ công
        SelfPivot              // Dùng chính Transform của vật thể (nếu pivot 3D đã ở mép cửa sẵn)
    }

    public enum HingeSide
    {
        Left,                  // Bản lề mép trái
        Right                  // Bản lề mép phải
    }

    public enum RotateAxis
    {
        Y_Axis,                // Xoay quanh trục Y (mở ngang - chuẩn nhất cho cửa nhà)
        X_Axis,                // Xoay quanh trục X
        Z_Axis                 // Xoay quanh trục Z
    }

    [Header("=== CÀI ĐẶT BẢN LỀ (HINGE SETTINGS) ===")]
    [Tooltip("Chế độ bản lề: AutoHingeFromCollider tự khắc phục lỗi tâm FBX")]
    public HingeMode hingeMode = HingeMode.AutoHingeFromCollider;

    [Tooltip("Bản lề nằm ở mép trái hay mép phải của cánh cửa")]
    public HingeSide hingeSide = HingeSide.Left;

    [Tooltip("Nếu bạn tự tạo 1 Empty GameObject làm bản lề, kéo vào đây (không bắt buộc)")]
    public Transform customHinge;

    [Header("=== CÀI ĐẶT XOAY MỞ CỬA (ROTATION) ===")]
    [Tooltip("Trục xoay của cửa (Thường là Y_Axis để mở ngang)")]
    public RotateAxis rotateAxis = RotateAxis.Y_Axis;

    [Tooltip("Góc mở cửa (Độ). Thử 90 hoặc -90 để đổi chiều mở vào trong / ra ngoài")]
    public float openAngle = 90f;

    [Tooltip("Tốc độ mở / đóng cửa")]
    public float openSpeed = 3.5f;

    [Tooltip("Cửa có mở sẵn ngay khi bắt đầu game không?")]
    public bool startOpen = false;

    [Header("=== KHÓA CỬA (LOCK SYSTEM) ===")]
    [Tooltip("Cửa có đang bị khóa không?")]
    public bool isLocked = false;

    [Tooltip("Tên chìa khóa cần để mở (dành cho hệ thống chìa khóa nếu có)")]
    public string requiredKeyName = "";

    [Tooltip("Câu chữ thông báo khi cửa bị khóa")]
    public string lockedPrompt = "Locked";

    [Header("=== CÂU CHỮ GỢI Ý UI [E] ===")]
    [Tooltip("Gợi ý khi cửa đang đóng")]
    public string openPrompt = "Open Door";

    [Tooltip("Gợi ý khi cửa đang mở")]
    public string closePrompt = "Close Door";

    [Header("=== ÂM THANH (AUDIO) ===")]
    [Tooltip("Âm thanh khi mở cửa (nếu trống sẽ lấy interactSound)")]
    public AudioClip openSound;

    [Tooltip("Âm thanh khi đóng cửa (nếu trống sẽ lấy interactSound)")]
    public AudioClip closeSound;

    [Tooltip("Âm thanh lắc then cửa khi bị khóa")]
    public AudioClip lockedSound;

    [Tooltip("AudioSource phát âm thanh 3D tại cửa")]
    public AudioSource localAudioSource;

    // Trạng thái nội bộ
    private bool isOpen = false;
    private Transform actualHinge;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine resetPromptCoroutine;

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;

    void Awake()
    {
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
        }

        if (localAudioSource != null)
        {
            localAudioSource.spatialBlend = 0f; // Để 2D để tránh bị lệch tâm làm tắt tiếng
            localAudioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Tự động gán âm thanh dự phòng nếu có trường nào bị trống
        if (openSound == null && interactSound != null) openSound = interactSound;
        if (closeSound == null && interactSound != null) closeSound = interactSound;
        if (interactSound == null && openSound != null) interactSound = openSound;

        SetupHinge();
        UpdatePromptMessage();
    }

    /// <summary>
    /// Thiết lập bản lề (Hinge) cho cửa
    /// </summary>
    private void SetupHinge()
    {
        if (hingeMode == HingeMode.CustomHinge && customHinge != null)
        {
            actualHinge = customHinge;
        }
        else if (hingeMode == HingeMode.SelfPivot)
        {
            actualHinge = transform;
        }
        else // AutoHingeFromCollider
        {
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                // Tính vị trí mép bản lề trong không gian cục bộ của cửa
                Vector3 localHinge = CalculateLocalHinge(boxCol);
                Vector3 worldHingePos = transform.TransformPoint(localHinge);

                // Tạo GameObject Pivot rỗng làm bản lề thực tế
                GameObject pivotObj = new GameObject(gameObject.name + "_HingePivot");
                pivotObj.transform.position = worldHingePos;
                pivotObj.transform.rotation = transform.rotation;

                // Nếu cửa có Transform cha (như House), gán pivotObj làm con của cha đó
                if (transform.parent != null)
                {
                    pivotObj.transform.SetParent(transform.parent, true);
                }

                // Chuyển cánh cửa thành con của pivotObj mà không làm thay đổi vị trí thế giới
                transform.SetParent(pivotObj.transform, true);

                actualHinge = pivotObj.transform;
            }
            else
            {
                Debug.LogWarning($"[HideoutDoorInteractable] Không tìm thấy BoxCollider trên {gameObject.name}. Dùng Transform cửa làm pivot.");
                actualHinge = transform;
            }
        }

        // Lưu góc xoay đóng và mở
        closedRotation = actualHinge.localRotation;
        Vector3 axis = GetAxisVector(rotateAxis);
        openRotation = closedRotation * Quaternion.Euler(axis * openAngle);

        // Áp dụng trạng thái ban đầu nếu startOpen = true
        if (startOpen)
        {
            isOpen = true;
            actualHinge.localRotation = openRotation;
        }
    }

    void Update()
    {
        if (actualHinge == null) return;

        // Xoay mượt mà tới góc mục tiêu
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        if (Quaternion.Angle(actualHinge.localRotation, targetRot) > 0.05f)
        {
            actualHinge.localRotation = Quaternion.Slerp(actualHinge.localRotation, targetRot, Time.deltaTime * openSpeed);
        }
        else
        {
            actualHinge.localRotation = targetRot;
        }
    }

    /// <summary>
    /// Gọi khi người chơi nhìn vào cửa và nhấn phím E
    /// </summary>
    public override void Interact()
    {
        // 1. Nếu cửa đang bị khóa
        if (isLocked)
        {
            PlaySound(lockedSound);
            Debug.Log($"[HideoutDoor] Cửa đang bị KHÓA! {gameObject.name}");

            // Hiển thị tạm thời câu chữ bị khóa
            promptMessage = lockedPrompt;
            if (resetPromptCoroutine != null) StopCoroutine(resetPromptCoroutine);
            resetPromptCoroutine = StartCoroutine(ResetPromptAfterDelay(1.5f));
            return;
        }

        // 2. Đảo trạng thái Mở / Đóng
        isOpen = !isOpen;
        UpdatePromptMessage();

        // 3. Phát âm thanh tương ứng
        PlaySound(isOpen ? openSound : closeSound);

        Debug.Log($"[HideoutDoor] Cửa đã {(isOpen ? "MỞ" : "ĐÓNG")}: {gameObject.name}");
    }

    /// <summary>
    /// Mở khóa cửa (có thể gọi từ Script chìa khóa hoặc sự kiện)
    /// </summary>
    public void Unlock()
    {
        isLocked = false;
        UpdatePromptMessage();
        Debug.Log($"[HideoutDoor] Đã mở khóa: {gameObject.name}");
    }

    /// <summary>
    /// Khóa cửa lại
    /// </summary>
    public void Lock()
    {
        isLocked = true;
        UpdatePromptMessage();
        Debug.Log($"[HideoutDoor] Đã khóa cửa: {gameObject.name}");
    }

    public void ToggleLock()
    {
        if (isLocked) Unlock();
        else Lock();
    }

    public void OpenDoor()
    {
        if (!isOpen && !isLocked)
        {
            isOpen = true;
            UpdatePromptMessage();
            PlaySound(openSound);
        }
    }

    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            UpdatePromptMessage();
            PlaySound(closeSound);
        }
    }

    private void UpdatePromptMessage()
    {
        if (isLocked)
        {
            promptMessage = lockedPrompt;
        }
        else
        {
            promptMessage = isOpen ? closePrompt : openPrompt;
        }
    }

    private IEnumerator ResetPromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdatePromptMessage();
    }

    private void PlaySound(AudioClip clip)
    {
        AudioClip clipToPlay = clip != null ? clip : interactSound;
        if (clipToPlay == null)
        {
            Debug.LogWarning($"[HideoutDoorInteractable] Không tìm thấy AudioClip để phát trên {gameObject.name}! Hãy kiểm tra xem đã gán Open Sound / Close Sound / Interact Sound chưa.");
            return;
        }

        // Ưu tiên 1: Luôn phát qua AudioManager toàn cục (như Drawer và Button) để đảm bảo 100% người chơi nghe rõ
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clipToPlay);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.pitch = Random.Range(0.95f, 1.05f);
            localAudioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clipToPlay, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        Debug.Log($"[HideoutDoorInteractable] Đã phát âm thanh cửa: {clipToPlay.name}");
    }

    private Vector3 GetAxisVector(RotateAxis axis)
    {
        switch (axis)
        {
            case RotateAxis.X_Axis: return Vector3.right;
            case RotateAxis.Y_Axis: return Vector3.up;
            case RotateAxis.Z_Axis: return Vector3.forward;
            default: return Vector3.up;
        }
    }

    /// <summary>
    /// Tính toán vị trí mép bản lề cục bộ dựa trên BoxCollider
    /// </summary>
    private Vector3 CalculateLocalHinge(BoxCollider col)
    {
        Vector3 hinge = col.center;

        // Nếu kích thước X lớn hơn hoặc bằng Z -> Chiều rộng cửa chạy dọc theo trục X
        if (col.size.x >= col.size.z)
        {
            float halfWidth = col.size.x * 0.5f;
            hinge.x += (hingeSide == HingeSide.Left) ? -halfWidth : halfWidth;
        }
        else // Chiều rộng cửa chạy dọc theo trục Z
        {
            float halfWidth = col.size.z * 0.5f;
            hinge.z += (hingeSide == HingeSide.Left) ? -halfWidth : halfWidth;
        }

        return hinge;
    }

    /// <summary>
    /// Tính vị trí mép đối diện của cửa để vẽ vòng cung mô phỏng trong Scene view
    /// </summary>
    private Vector3 CalculateOppositeEdge(BoxCollider col)
    {
        Vector3 edge = col.center;

        if (col.size.x >= col.size.z)
        {
            float halfWidth = col.size.x * 0.5f;
            edge.x += (hingeSide == HingeSide.Left) ? halfWidth : -halfWidth;
        }
        else
        {
            float halfWidth = col.size.z * 0.5f;
            edge.z += (hingeSide == HingeSide.Left) ? halfWidth : -halfWidth;
        }

        return edge;
    }

    // Hiển thị trực quan vị trí bản lề và hướng mở cửa ngay trong Unity Scene View khi bạn chọn vật thể
    private void OnDrawGizmosSelected()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Vector3 localHinge = CalculateLocalHinge(col);
        Vector3 worldHinge = transform.TransformPoint(localHinge);

        Vector3 axisWorld = transform.TransformDirection(GetAxisVector(rotateAxis));

        // 1. Điểm bản lề (Màu vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(worldHinge, 0.06f);

        // 2. Trục xoay bản lề (Đường màu xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(worldHinge - axisWorld * 0.4f, worldHinge + axisWorld * 0.4f);

        // 3. Vòng cung mở cửa (Màu xanh lơ Cyan)
        Vector3 localOpposite = CalculateOppositeEdge(col);
        Vector3 worldOpposite = transform.TransformPoint(localOpposite);

        Gizmos.color = Color.cyan;
        Vector3 prevPoint = worldOpposite;
        int segments = 16;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Quaternion rot = Quaternion.AngleAxis(openAngle * t, axisWorld);
            Vector3 nextPoint = worldHinge + rot * (worldOpposite - worldHinge);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Điểm dừng của mép cửa khi mở hết
        Gizmos.DrawWireSphere(prevPoint, 0.04f);
    }
}
