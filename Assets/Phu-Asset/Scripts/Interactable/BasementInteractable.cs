using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Script dùng cho Cửa Hầm (Basement/Cellar Door) và Cửa Tủ 2 Cánh.
/// Kế thừa Interactable (Phú-Asset).
/// - Hỗ trợ chế độ Fade Scene & Dịch Chuyển Tức Thời (useFadeTeleport) giữa mặt đất và bên trong hầm Cellar.
/// - Không cần chạy hiệu ứng đóng/mở cánh cửa vật lý khi dùng chế độ dịch chuyển.
/// - Cho phép tự đặt vị trí xuất hiện (teleportTarget) tùy ý qua Inspector.
/// - Hỗ trợ Gizmos hiển thị trực quan vị trí hạ cánh trong Scene View.
/// - Tương thích ngược: Cửa tủ (C.T Door) giữ nguyên chế độ xoay cánh cửa vật lý bình thường.
/// </summary>
public class BasementInteractable : Interactable
{
    public enum Axis { X, Y, Z }

    [Header("=== CÁC CÁNH CỬA VẬT LÝ ===")]
    [Tooltip("Kéo transform cánh cửa trái (Left) vào đây")]
    public Transform leftDoor;

    [Tooltip("Kéo transform cánh cửa phải (Right) vào đây")]
    public Transform rightDoor;

    [Header("=== GÓC MỞ CỬA (KHI DÙNG CHẾ ĐỘ XOAY) ===")]
    [Tooltip("Trục xoay mở cửa (X hoặc Z cho nắp hầm lật, Y cho cửa 2 cánh mở ngang)")]
    public Axis rotationAxis = Axis.X;

    [Tooltip("Góc xoay mở cánh trái (thường là -90 hoặc 90)")]
    public float leftOpenAngle = -90f;

    [Tooltip("Góc xoay mở cánh phải (thường ngược dấu cánh trái, VD: 90)")]
    public float rightOpenAngle = 90f;

    [Tooltip("Tốc độ xoay mở cửa")]
    public float openSpeed = 3f;

    [Header("=== CHẾ ĐỘ CHUYỂN CẢNH & DỊCH CHUYỂN (FADE TELEPORT) ===")]
    [Tooltip("Bật chế độ Fade màn hình và dịch chuyển tức thời vào/ra khỏi hầm thay vì chỉ mở cánh cửa")]
    public bool useFadeTeleport = false;

    [Tooltip("Vị trí dịch chuyển đến. Kéo 1 Empty GameObject vào đây để tự đặt điểm xuất hiện & hướng nhìn theo ý muốn!")]
    public Transform teleportTarget;

    [Tooltip("Độ lệch vị trí tùy chỉnh khi dịch chuyển (X, Y, Z)")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Khoảng cách đứng phía trước cửa đối ứng khi tự động tính vị trí (mét)")]
    public float autoFrontDistance = 1.5f;

    [Tooltip("Thời gian tối dần khi bắt đầu chuyển cảnh (giây)")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Thời gian dừng trong màn hình đen (giây)")]
    public float blackHoldDuration = 0.2f;

    [Tooltip("Thời gian sáng dần khi vào vị trí mới (giây)")]
    public float fadeInDuration = 0.6f;

    [Tooltip("Âm thanh khi bước qua cửa / chuyển cảnh")]
    public AudioClip transitionSound;

    [Header("=== ÂM THANH MỞ CỬA VẬT LÝ ===")]
    public AudioSource localAudioSource;

    private bool isOpen = false;
    private bool isTransitioning = false;
    private bool isExitDoor = false;

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
        // Nhận diện xem cửa này là Cửa Ra (trong hầm) hay Cửa Vào (ngoài sân)
        isExitDoor = IsExitCellarDoor();

        // Tự động kích hoạt useFadeTeleport cho cửa hầm nếu chưa gán thủ công trong Inspector
        string objName = gameObject.name.ToLower();
        bool isCabinetDoor = objName.Contains("c.t") || objName.Contains("cabinet");

        if (!isCabinetDoor && !useFadeTeleport && (objName.Contains("cellar") || objName.Contains("basement") || isExitDoor))
        {
            useFadeTeleport = true;
        }

        if (useFadeTeleport)
        {
            // Thiết lập prompt tiếng Anh theo hướng vào/ra
            if (string.IsNullOrEmpty(promptMessage) || promptMessage == "Interact" || promptMessage == "Open Door" || promptMessage == "Close Door")
            {
                promptMessage = isExitDoor ? "Exit Cellar" : "Enter Cellar";
            }
        }
        else
        {
            if (string.IsNullOrEmpty(promptMessage) || promptMessage == "Interact")
            {
                promptMessage = "Open Door";
            }
        }

        // Tự động tìm 2 cánh con có tên "Left" và "Right" nếu chưa kéo vào Inspector
        if (leftDoor == null && transform.Find("Left") != null)
            leftDoor = transform.Find("Left");

        if (rightDoor == null && transform.Find("Right") != null)
            rightDoor = transform.Find("Right");

        // Lưu góc xoay ban đầu
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
        // Khi dùng chế độ fade teleport, không cần xoay cánh cửa vật lý
        if (useFadeTeleport) return;

        // Xoay mượt mà cả 2 cánh cửa cùng lúc cho cửa tủ hoặc cửa vật lý thông thường
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
        // Nếu dùng chế độ fade & teleport
        if (useFadeTeleport)
        {
            if (isTransitioning) return;
            StartCoroutine(FadeTeleportRoutine());
            return;
        }

        // Chế độ mở/đóng cửa vật lý truyền thống (cho tủ, hòm, v.v.)
        isOpen = !isOpen;
        promptMessage = isOpen ? "Close Door" : "Open Door";

        PlayDoorSound();
        Debug.Log($"[BasementDoor] Cửa đã {(isOpen ? "MỞ" : "ĐÓNG")}: {gameObject.name}");
    }

    private IEnumerator FadeTeleportRoutine()
    {
        isTransitioning = true;
        string originalPrompt = promptMessage;
        promptMessage = ""; // Ẩn gợi ý tương tác tạm thời khi đang chuyển cảnh

        // 1. Âm thanh tương tác
        PlayTransitionSound();

        // 2. Tìm Player
        PlayerController player = FindObjectOfType<PlayerController>();
        PlayerBodyRotator bodyRotator = FindObjectOfType<PlayerBodyRotator>();

        // 3. Tính toán vị trí & góc nhìn đích
        Vector3 destPos;
        Quaternion destRot;
        GetTeleportDestination(out destPos, out destRot);

        // Khóa di chuyển & xoay chuột tạm thời để tránh người chơi bấm di chuyển trong lúc tối
        if (player != null) player.enabled = false;
        if (bodyRotator != null) bodyRotator.enabled = false;

        // 4. Màn hình tối dần (Fade Out)
        if (SceneFader.Instance != null)
        {
            yield return StartCoroutine(SceneFader.Instance.FadeOut(fadeOutDuration));
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // 5. Dịch chuyển Player đến vị trí mới (nâng nhẹ chân Y + 0.1m để tránh kẹt sàn)
        if (player != null)
        {
            player.TeleportTo(destPos, destRot);
            Debug.Log($"[BasementDoor] Đã dịch chuyển người chơi tới: {destPos}, hướng nhìn: {destRot.eulerAngles.y}°");
        }

        // 6. Dừng lại một khoảnh khắc trong bóng tối
        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        // Mở lại điều khiển người chơi
        if (player != null) player.enabled = true;
        if (bodyRotator != null) bodyRotator.enabled = true;

        // 7. Màn hình sáng dần (Fade In)
        if (SceneFader.Instance != null)
        {
            yield return StartCoroutine(SceneFader.Instance.FadeIn(fadeInDuration));
        }
        else
        {
            yield return new WaitForSeconds(fadeInDuration);
        }

        promptMessage = originalPrompt;
        isTransitioning = false;
    }

    /// <summary>
    /// Tính toán điểm đích và hướng nhìn khi người chơi dịch chuyển
    /// </summary>
    public void GetTeleportDestination(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        // 1. ƯU TIÊN SỐ 1: Điểm dịch chuyển do người dùng kéo vào ô Teleport Target trong Inspector
        if (teleportTarget != null)
        {
            targetPosition = teleportTarget.position + spawnOffset + Vector3.up * 0.1f;
            targetRotation = teleportTarget.rotation;
            return;
        }

        // 2. Tìm tự động đối tượng SpawnPoint nếu người dùng đã tạo trong Scene
        string searchSpawnTag = isExitDoor ? "OutsideSpawnPoint" : "CellarSpawnPoint";
        GameObject customSpawn = GameObject.Find(searchSpawnTag);
        if (customSpawn == null && !isExitDoor) customSpawn = GameObject.Find("CellarSpawn");
        if (customSpawn == null && isExitDoor) customSpawn = GameObject.Find("OutsideSpawn");

        if (customSpawn != null)
        {
            targetPosition = customSpawn.transform.position + spawnOffset + Vector3.up * 0.1f;
            targetRotation = customSpawn.transform.rotation;
            return;
        }

        // 3. Tự động tính toán an toàn dựa trên cửa đối ứng (counterpart door)
        BasementInteractable counterpart = FindCounterpartDoor();
        if (counterpart != null)
        {
            if (isExitDoor)
            {
                // Từ trong hầm bước ra ngoài sân:
                // Đối ứng là GetInCellarDoor ngoài sân.
                // Đứng phía trước cửa ngoài sân trên mặt cỏ (+Z), hướng về sân
                targetPosition = counterpart.transform.position + new Vector3(0f, 0.15f, autoFrontDistance) + spawnOffset;
                targetRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                // Từ ngoài sân bước vào hầm Cellar:
                // Đối ứng là GetOutCellarDoor trong hầm.
                // Đứng ở khu vực chân thang/cửa hầm, mặt hướng vào trung tâm phòng (tránh hoàn toàn giường)
                Vector3 flatForward = Vector3.ProjectOnPlane(counterpart.transform.forward, Vector3.up).normalized;
                if (flatForward == Vector3.zero) flatForward = counterpart.transform.forward;

                targetPosition = counterpart.transform.position + flatForward * autoFrontDistance + Vector3.up * 0.15f + spawnOffset;
                targetRotation = Quaternion.Euler(0f, counterpart.transform.eulerAngles.y, 0f);
            }
            return;
        }

        // 4. Fallback dự phòng: Vị trí trung tâm sàn hầm (xa giường)
        if (!isExitDoor)
        {
            GameObject cellar = GameObject.Find("Cellar");
            if (cellar != null)
            {
                // Vị trí mở ở sàn hầm gần cửa thang, cách xa giường
                targetPosition = cellar.transform.TransformPoint(new Vector3(-3.0f, 0.15f, -1.5f)) + spawnOffset;
                targetRotation = cellar.transform.rotation;
                return;
            }
        }

        targetPosition = transform.position + transform.forward * autoFrontDistance + Vector3.up * 0.15f + spawnOffset;
        targetRotation = transform.rotation;
    }

    public bool IsExitCellarDoor()
    {
        string n = gameObject.name.ToLower();
        // Loại trừ tuyệt đối cửa tủ đồ (C.T Door)
        if (n.Contains("c.t") || n.Contains("cabinet")) return false;

        if (n.Contains("getout") || n.Contains("exit")) return true;
        if (n.Contains("getin")) return false;

        // Nếu là con của Cellar nhưng mang tên cửa hầm
        if (n.Contains("cellar") && n.Contains("out")) return true;

        if (transform.position.y < -5f && (n.Contains("cellar") || n.Contains("door"))) return true;

        return false;
    }

    private BasementInteractable FindCounterpartDoor()
    {
        BasementInteractable[] allDoors = FindObjectsOfType<BasementInteractable>();
        bool myIsExit = IsExitCellarDoor();

        foreach (var door in allDoors)
        {
            if (door == this) continue;
            string dName = door.gameObject.name.ToLower();

            // Loại trừ hoàn toàn cửa tủ đồ (C.T Door) hoặc đồ đạc nội thất
            if (dName.Contains("c.t") || dName.Contains("cabinet")) continue;
            if (door.transform.parent != null && door.transform.parent.name.ToLower().Contains("cabinet")) continue;

            // Phải là cửa hầm đích thực
            if (!dName.Contains("cellar") && !dName.Contains("basement") && !dName.Contains("getin") && !dName.Contains("getout")) continue;

            if (door.IsExitCellarDoor() != myIsExit)
            {
                return door;
            }
        }

        return null;
    }

    private void PlayTransitionSound()
    {
        AudioClip clip = (transitionSound != null) ? transitionSound : interactSound;
        if (clip == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.PlayOneShot(clip);
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
            localAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            localAudioSource.PlayOneShot(interactSound);
        }
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

    void OnDrawGizmosSelected()
    {
        // Hiển thị trực quan điểm dịch chuyển và hướng nhìn trong Unity Scene View khi click vào cửa
        string objName = gameObject.name.ToLower();
        if (objName.Contains("c.t") || objName.Contains("cabinet")) return;

        Vector3 destPos;
        Quaternion destRot;
        GetTeleportDestination(out destPos, out destRot);

        // Vẽ quả cầu màu Cyan tại điểm đáp của Player
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(destPos, 0.4f);
        Gizmos.DrawLine(destPos, destPos + Vector3.up * 1.8f); // Chiều cao cơ thể người chơi

        // Vẽ mũi tên chỉ hướng nhìn của Player khi vừa đáp
        Vector3 lookDir = destRot * Vector3.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(destPos + Vector3.up * 1.5f, lookDir * 1.2f);
    }
}
