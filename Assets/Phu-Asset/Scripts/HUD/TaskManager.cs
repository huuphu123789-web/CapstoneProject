using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý danh sách nhiệm vụ (Quest List) & Hiệu ứng hù dọa tăng tiến cho Night-1.
/// Tự động hiển thị bảng nhiệm vụ trên màn hình và mở khóa giường ngủ khi hoàn thành.
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager instance;

    [Header("=== CẤU HÌNH SỐ LƯỢNG NHIỆM VỤ ===")]
    [Tooltip("Số điểm hàng rào cần tuần tra")]
    public int totalFencePoints = 6;

    [Tooltip("Số máy phát điện cần kiểm tra")]
    public int totalGenerators = 1;

    [Header("=== PHÍM TẮT TEST (DEBUG HOTKEY) ===")]
    [Tooltip("Phím tắt chính để hoàn thành ngay các việc ban đầu và chuyển sang kiểm soát bốt gác (Mặc định: Phím K)")]
    public KeyCode skipToGateInspectionKey = KeyCode.K;

    [Tooltip("Phím tắt phụ dự phòng (Mặc định: Phím F1)")]
    public KeyCode alternativeSkipKey = KeyCode.F1;

    [Tooltip("Tự động dịch chuyển người chơi đến bốt gác khi bấm phím tắt Test")]
    public bool teleportToBoothOnSkip = false;

    [Tooltip("Phím tắt dịch chuyển nhanh người chơi đến bốt gác kiểm tra NPC bất kỳ lúc nào (Mặc định: Phím L hoặc F2)")]
    public KeyCode teleportToGateKey = KeyCode.L;
    public KeyCode alternativeTeleportKey = KeyCode.F2;

    [Header("=== ĐIỂM DỊCH CHUYỂN BỐT GÁC (TỰ ĐẶT) ===")]
    [Tooltip("Tạo một Empty GameObject đặt tại vị trí đứng bên trong bốt gác và kéo vào đây")]
    public Transform playerBoothTeleportPoint;

    [Header("=== CHẾ ĐỘ TEST NHANH KHI BẮT ĐẦU ===")]
    [Tooltip("Tích vào đây nếu muốn hoàn thành ngay việc lặt vặt (hàng rào, máy phát điện, súng đèn) khi vào game để test ngay bốt gác NPC")]
    public bool quickTestGateInspection = false;

    [Header("=== GIAO DIỆN HIỂN THỊ (TASK HUD) ===")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị danh sách nhiệm vụ vào đây (nếu để trống script tự động tạo góc trái màn hình)")]
    public TextMeshProUGUI taskTextUI;

    [Header("=== ÂM THANH HOÀN THÀNH NHIỆM VỤ ===")]
    public AudioClip taskCompleteSound;

    // Tiến độ hiện tại
    [HideInInspector] public bool hasFlashlight = false;
    [HideInInspector] public bool hasGun = false;
    public bool hasEquipment => (hasFlashlight && hasGun);
    [HideInInspector] public int totalLeaves = 0; // Đã loại bỏ task quét lá
    [HideInInspector] public int leavesSwept = 0;
    [HideInInspector] public int fencePointsChecked = 0;
    [HideInInspector] public int generatorsChecked = 0;
    [HideInInspector] public bool gateInspectionDone = false;
    [HideInInspector] public bool gateInspectionTriggered = false;

    private Coroutine cameraShakeCoroutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        EnsureTaskUI();

        if (quickTestGateInspection)
        {
            SkipToGateInspection(teleportToBoothOnSkip);
        }
        else
        {
            UpdateTaskUI();
        }
    }

    void Update()
    {
        // Kiểm tra phím tắt debug để test nhanh
        CheckDebugHotkeys();

        if (taskTextUI == null) return;

        bool isPaused = (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
                     || (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused);

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        bool isMenu = currentScene.Contains("menu");

        bool shouldShow = !isPaused && !isMenu;
        if (taskTextUI.gameObject.activeSelf != shouldShow)
        {
            taskTextUI.gameObject.SetActive(shouldShow);
        }
    }

    private void CheckDebugHotkeys()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (currentScene.Contains("menu")) return;

        bool isPaused = (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
                     || (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused);
        if (isPaused) return;

        // Phím tắt: Hoàn thành các việc ban đầu -> Nhảy sang nhiệm vụ kiểm soát bốt gác (Mặc định: Phím K hoặc F1)
        bool skipPressed = Input.GetKeyDown(skipToGateInspectionKey) || Input.GetKeyDown(alternativeSkipKey);
        if (!skipPressed)
        {
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if ((skipToGateInspectionKey == KeyCode.K && kb.kKey.wasPressedThisFrame) ||
                        (alternativeSkipKey == KeyCode.F1 && kb.f1Key.wasPressedThisFrame))
                    {
                        skipPressed = true;
                    }
                }
            }
            catch { }
        }

        if (skipPressed)
        {
            SkipToGateInspection(teleportToBoothOnSkip);
        }

        // Phím tắt phụ: Dịch chuyển ngay đến bốt gác kiểm tra NPC (Mặc định: Phím L hoặc F2)
        bool tpPressed = Input.GetKeyDown(teleportToGateKey) || Input.GetKeyDown(alternativeTeleportKey);
        if (!tpPressed)
        {
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if ((teleportToGateKey == KeyCode.L && kb.lKey.wasPressedThisFrame) ||
                        (alternativeTeleportKey == KeyCode.F2 && kb.f2Key.wasPressedThisFrame))
                    {
                        tpPressed = true;
                    }
                }
            }
            catch { }
        }

        if (tpPressed)
        {
            TeleportToGateBooth();
        }
    }

    /// <summary>
    /// Phím tắt Test: Hoàn thành ngay các nhiệm vụ mở đầu (Trang bị súng/đèn, Tuần tra hàng rào, Kiểm tra máy phát điện)
    /// và mở khóa ngay nhiệm vụ Kiểm tra cổng gác (Gate Inspection).
    /// </summary>
    public void SkipToGateInspection(bool teleport = false)
    {
        Debug.Log("[TaskManager] >>> KÍCH HOẠT PHÍM TẮT: Hoàn thành các việc ban đầu -> Nhảy sang nhiệm vụ Kiểm Soát Bốt Gác!");

        // 1. Cập nhật trạng thái nhiệm vụ hoàn tất
        hasFlashlight = true;
        hasGun = true;
        fencePointsChecked = totalFencePoints;
        generatorsChecked = totalGenerators;

        // 2. Tự động trang bị Đèn pin cho người chơi
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController != null)
        {
            fController.EquipFlashlight();
        }

        // 3. Tự động trang bị Súng Pistol 92 cho người chơi và nạp sẵn đạn
        PlayerGun gun = FindObjectOfType<PlayerGun>(true);
        if (gun != null)
        {
            gun.gameObject.SetActive(true);
            gun.HasGun = true;
            if (PlayerGun.storedAmmo < 12) PlayerGun.storedAmmo = 12;
            gun.SetAmmo(Mathf.Max(gun.CurrentAmmo, PlayerGun.storedAmmo));
            gun.DrawGun();
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                GameObject gunPrefab = BedInteractable.LoadGunPrefab();
                if (gunPrefab != null)
                {
                    GameObject gunObj = Instantiate(gunPrefab, cam.transform);
                    gunObj.name = "gun";
                    gunObj.transform.localPosition = new Vector3(0.16f, -0.12f, 0.32f);
                    gunObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    gunObj.transform.localScale = Vector3.one * 0.28f;

                    gun = gunObj.GetComponentInChildren<PlayerGun>(true) ?? gunObj.AddComponent<PlayerGun>();
                    gun.DisableGunColliders();
                    gun.gameObject.SetActive(true);
                    gun.HasGun = true;
                    PlayerGun.storedAmmo = 12;
                    gun.SetAmmo(12);
                    gun.DrawGun();
                }
            }
        }

        // 4. Ẩn các vật phẩm trên tủ (đèn pin/súng) nếu chưa nhặt
        EquipmentPickup[] pickups = FindObjectsOfType<EquipmentPickup>(true);
        foreach (var p in pickups)
        {
            if (p.equipmentType == EquipmentPickupType.Flashlight || 
                p.equipmentType == EquipmentPickupType.Gun)
            {
                p.gameObject.SetActive(false);
            }
        }

        // 5. Ẩn các điểm tuần tra hàng rào và tắt collider
        FencePatrolZone[] zones = FindObjectsOfType<FencePatrolZone>();
        foreach (var z in zones)
        {
            z.gameObject.SetActive(false);
        }

        // 6. Bật hệ thống đèn hàng rào kết nối máy phát điện
        GeneratorInteractable gen = FindObjectOfType<GeneratorInteractable>();
        if (gen != null)
        {
            gen.TurnOnAllFenceLights(false);
        }

        // 7. Đồng bộ HUD
        if (PlayerHUDManager.instance != null)
        {
            if (gun != null) PlayerHUDManager.instance.UpdateAmmoUI(gun.CurrentAmmo, gun.MaxAmmo);
            PlayerHUDManager.instance.UpdateFlashlightUI();
            PlayerHUDManager.instance.UpdateControlsHintUI();
        }

        // 8. Phát âm thanh hoàn thành chuông ngân
        PlayTaskCompleteSound();

        // 9. Cập nhật bảng nhiệm vụ & Tự động kích hoạt nhiệm vụ kiểm soát bốt gác NPC
        UpdateTaskUI();

        // Đảm bảo bốt gác được khởi động
        if (NPCInspectionManager.instance != null && !NPCInspectionManager.instance.isInspectionActive)
        {
            NPCInspectionManager.instance.StartGateInspection();
        }

        // 10. Dịch chuyển đến bốt gác nếu được yêu cầu
        if (teleport)
        {
            TeleportToGateBooth();
        }
    }

    /// <summary>
    /// Dịch chuyển nhanh người chơi đến bốt gác kiểm tra NPC
    /// </summary>
    public void TeleportToGateBooth()
    {
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;
        bool foundDestination = false;

        // 1. Ưu tiên số 1: Điểm do người dùng tự đặt trong Inspector
        if (playerBoothTeleportPoint != null)
        {
            targetPos = playerBoothTeleportPoint.position;
            targetRot = playerBoothTeleportPoint.rotation;
            foundDestination = true;
        }
        else if (InspectionDeskInteractable.instance != null)
        {
            Transform desk = InspectionDeskInteractable.instance.transform;
            targetPos = desk.position - desk.forward * 1.2f;
            targetRot = Quaternion.LookRotation(desk.forward);
            foundDestination = true;
        }
        else if (NPCInspectionManager.instance != null && NPCInspectionManager.instance.inspectionPoint != null)
        {
            Transform insp = NPCInspectionManager.instance.inspectionPoint;
            targetPos = insp.position;
            targetRot = insp.rotation;
            foundDestination = true;
        }

        if (foundDestination)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform.root.gameObject;
            }

            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = targetPos;
                player.transform.rotation = targetRot;

                if (cc != null) cc.enabled = true;
                Debug.Log($"[TaskManager] Đã dịch chuyển người chơi đến Bốt Gác tại toạ độ {targetPos}!");
            }
        }
        else
        {
            Debug.LogWarning("[TaskManager] Chưa gán điểm dịch chuyển 'Player Booth Teleport Point' trong Inspector!");
        }
    }

    public void SetTaskUIVisible(bool visible)
    {
        if (taskTextUI != null)
        {
            taskTextUI.gameObject.SetActive(visible);
        }
    }

    // ================= XỬ LÝ NHIỆM VỤ MỞ ĐẦU: LẤY ĐÈN PIN & SÚNG TRONG HẦM (CELLAR) =================
    public void CollectFlashlight()
    {
        if (hasFlashlight) return;
        hasFlashlight = true;
        Debug.Log("[TaskManager] ĐÃ LẤY ĐÈN PIN!");

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    public void CollectGun()
    {
        if (hasGun) return;
        hasGun = true;
        Debug.Log("[TaskManager] ĐÃ LẤY SÚNG!");

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    public void CompleteEquipmentTask()
    {
        hasFlashlight = true;
        hasGun = true;
        Debug.Log("[TaskManager] ĐÃ LẤY TRANG BỊ: Đèn pin & Súng!");

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    // ================= XỬ LÝ NHIỆM VỤ 1: QUÉT 5 ĐỐNG LÁ =================
    public void CompleteLeafPile(int leafIndex)
    {
        leavesSwept++;
        Debug.Log($"[TaskManager] Đã quét đống lá: {leavesSwept}/{totalLeaves}");

        // Kích hoạt hù dọa tăng tiến theo thứ tự đống lá (1 -> 5)
        switch (leafIndex)
        {
            case 1:
                // Đống 1: Bình thường, tiếng lá xào xạc
                Debug.Log("[Horror] Đống lá 1: Quét nhẹ nhàng.");
                break;
            case 2:
                // Đống 2: Tiếng cành cây gãy khẽ sau lưng
                Debug.Log("[Horror] Đống lá 2: Cành cây gãy khẽ.");
                break;
            case 3:
                // Đống 3: Đèn pin chớp tắt 1 nhịp
                Debug.Log("[Horror] Đống lá 3: Đèn pin chớp tắt 1 nhịp.");
                FlickerFlashlight(1, 0.12f);
                break;
            case 4:
                // Đống 4: Tiếng thì thầm ma quái bên tai
                Debug.Log("[Horror] Đống lá 4: Tiếng thì thầm ma quái!");
                FlickerFlashlight(2, 0.1f);
                break;
            case 5:
            default:
                // Đống 5: Đèn pin chớp tắt liên tục + tiếng động lạ đập mạnh
                Debug.Log("[Horror] Đống lá 5: Đèn chớp liên hồi + tiếng đập mạnh!");
                FlickerFlashlight(4, 0.08f);
                break;
        }

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    // ================= XỬ LÝ NHIỆM VỤ 2: TUẦN TRA 6 ĐIỂM HÀNG RÀO =================
    public void CompleteFencePatrol(int fenceIndex)
    {
        fencePointsChecked++;
        Debug.Log($"[TaskManager] Đã tuần tra hàng rào: {fencePointsChecked}/{totalFencePoints}");

        // Kích hoạt hù dọa tăng tiến theo thứ tự điểm rào (1 -> 6)
        switch (fenceIndex)
        {
            case 1:
                // Điểm 1: Gió lạnh rít nhẹ
                Debug.Log("[Horror] Điểm rào 1: Yên ắng, gió rít.");
                break;
            case 2:
                // Điểm 2: Tiếng rung rào sắt ở xa
                Debug.Log("[Horror] Điểm rào 2: Tiếng rung rào sắt đằng xa.");
                break;
            case 3:
                // Điểm 3: Tiếng cào xước nhẹ vào lưới rào
                Debug.Log("[Horror] Điểm rào 3: Tiếng cào xước rào sắt.");
                break;
            case 4:
                // Điểm 4: Đèn pin chớp tắt và tối om trong 1 giây
                Debug.Log("[Horror] Điểm rào 4: Đèn pin tắt tối om 1 giây!");
                FlickerFlashlight(3, 0.12f, 1.0f);
                break;
            case 5:
                // Điểm 5: Tiếng cào rào sắt dữ dội + tiếng thở dốc
                Debug.Log("[Horror] Điểm rào 5: Cào rào dữ dội + tiếng thở dốc!");
                FlickerFlashlight(4, 0.08f);
                break;
            case 6:
            default:
                // Điểm 6: Đỉnh điểm bất ngờ! Camera Shake + tiếng gầm gừ + đèn chớp
                Debug.Log("[Horror] Điểm rào 6: Đỉnh điểm bất ngờ! Camera Shake + Jumpscare!");
                TriggerCameraShake(0.6f, 0.15f);
                FlickerFlashlight(5, 0.07f);
                break;
        }

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    // ================= XỬ LÝ NHIỆM VỤ 3: KIỂM TRA MÁY PHÁT ĐIỆN =================
    public void CompleteGenerator(int genIndex = 1)
    {
        generatorsChecked++;
        Debug.Log($"[TaskManager] Đã kiểm tra máy phát điện: {generatorsChecked}/{totalGenerators}");

        // Kích hoạt hiệu ứng hù dọa nhẹ khi mở/kiểm tra máy phát điện
        FlickerFlashlight(3, 0.08f);

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    public bool AreAllTasksCompleted()
    {
        return hasEquipment && (fencePointsChecked >= totalFencePoints) && (generatorsChecked >= totalGenerators) && gateInspectionDone;
    }

    public void CompleteGateInspection()
    {
        if (gateInspectionDone) return;
        gateInspectionDone = true;
        Debug.Log("[TaskManager] ĐÃ HOÀN THÀNH NHIỆM VỤ KIỂM TRA CỔNG GÁC! Mở khóa giường ngủ.");

        PlayTaskCompleteSound();
        UpdateTaskUI();
        CheckAllTasksDone();
    }

    private void CheckAllTasksDone()
    {
        if (AreAllTasksCompleted())
        {
            Debug.Log("[TaskManager] ĐÃ HOÀN THÀNH TẤT CẢ NHIỆM VỤ! Mở khóa giường ngủ.");
            PlayTaskCompleteSound();
        }
    }

    private void PlayTaskCompleteSound()
    {
        AudioClip clipToPlay = taskCompleteSound;
        if (clipToPlay == null)
        {
            clipToPlay = GenerateTaskCompleteChime();
        }

        if (clipToPlay != null)
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(clipToPlay);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clipToPlay, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }
    }

    /// <summary>
    /// Tự động tạo âm thanh chuông ngân hoàn thành nhiệm vụ trong trẻo chuẩn 44.1kHz
    /// </summary>
    private AudioClip GenerateTaskCompleteChime()
    {
        int sampleRate = 44100;
        float duration = 1.8f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Hợp âm ngân vang: E4, G#4, B4, E5, B5
        float[] freqs = { 329.63f, 415.30f, 493.88f, 659.25f, 987.77f };
        float[] startTimes = { 0.0f, 0.12f, 0.24f, 0.36f, 0.48f };
        float[] amps = { 0.45f, 0.55f, 0.65f, 0.85f, 0.40f };

        for (int n = 0; n < freqs.Length; n++)
        {
            float freq = freqs[n];
            int startIdx = (int)(startTimes[n] * sampleRate);
            int noteSamples = sampleCount - startIdx;

            for (int i = 0; i < noteSamples; i++)
            {
                int idx = startIdx + i;
                if (idx >= sampleCount) break;

                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 2.8f);
                float val = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f
                          + Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.25f * Mathf.Exp(-t * 5.0f)
                          + Mathf.Sin(2f * Mathf.PI * freq * 3.01f * t) * 0.1f * Mathf.Exp(-t * 7.0f);

                samples[idx] += val * env * amps[n];
            }
        }

        // Chuẩn hóa âm lượng (Normalize)
        float maxVal = 0.001f;
        for (int i = 0; i < sampleCount; i++)
        {
            if (Mathf.Abs(samples[i]) > maxVal) maxVal = Mathf.Abs(samples[i]);
        }
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = (samples[i] / maxVal) * 0.85f;
        }

        AudioClip clip = AudioClip.Create("TaskCompleteChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ================= CẬP NHẬT GIAO DIỆN TASK HUD =================
    public void UpdateTaskUI()
    {
        if (taskTextUI == null) return;

        bool equipDone = hasEquipment;
        bool fenceDone = (fencePointsChecked >= totalFencePoints);
        bool genDone = (generatorsChecked >= totalGenerators);
        bool choresDone = equipDone && fenceDone && genDone;
        bool allDone = choresDone && gateInspectionDone;

        // Tự động kích hoạt nhiệm vụ kiểm tra cổng gác khi xong các việc tuần tra & kiểm tra
        if (choresDone && !gateInspectionTriggered)
        {
            gateInspectionTriggered = true;
            if (NPCInspectionManager.instance != null)
            {
                NPCInspectionManager.instance.StartGateInspection();
            }
            else
            {
                NPCInspectionManager mgr = FindObjectOfType<NPCInspectionManager>();
                if (mgr != null)
                {
                    mgr.StartGateInspection();
                }
                else
                {
                    GameObject mgrGO = new GameObject("NPCInspectionManager");
                    mgr = mgrGO.AddComponent<NPCInspectionManager>();
                    mgr.StartGateInspection();
                }
            }
        }

        string equipStatus;
        if (hasFlashlight && hasGun)
        {
            equipStatus = "<color=#00FF88>[v] Take flashlight & gun from cabinet</color>";
        }
        else if (hasFlashlight && !hasGun)
        {
            equipStatus = "<color=#FFFF00>[ ] Take gun from drawer (Flashlight [v])</color>";
        }
        else if (!hasFlashlight && hasGun)
        {
            equipStatus = "<color=#FFFF00>[ ] Take flashlight from drawer (Gun [v])</color>";
        }
        else
        {
            equipStatus = "<color=#FFFF00>[ ] Take flashlight & gun from cabinet</color>";
        }

        string fenceStatus = fenceDone ? $"<color=#00FF88>[v] Patrol perimeter fence ({fencePointsChecked}/{totalFencePoints})</color>" : $"[ ] Patrol perimeter fence ({fencePointsChecked}/{totalFencePoints})";
        string genStatus = genDone ? $"<color=#00FF88>[v] Check generator ({generatorsChecked}/{totalGenerators})</color>" : $"[ ] Check generator ({generatorsChecked}/{totalGenerators})";
        
        string gateStatus = "";
        if (!choresDone)
        {
            gateStatus = "<color=#888888>[ ] Inspect incoming NPCs (Complete chores first)</color>";
        }
        else if (!gateInspectionDone)
        {
            gateStatus = "<color=#FFFF00>[ ] Go to the gate to inspect incoming NPCs</color>";
        }
        else
        {
            gateStatus = "<color=#00FF88>[v] Inspect incoming NPCs at the gate</color>";
        }

        string bedStatus = allDone 
            ? "<color=#FFFF00>[ ] Go to sleep in bedroom</color>" 
            : "<color=#888888>[ ] Go to sleep (Complete all tasks first)</color>";

        taskTextUI.text = $"<b>NIGHT 1 TASKS:</b>\n" +
                          $"{equipStatus}\n" +
                          $"{fenceStatus}\n" +
                          $"{genStatus}\n" +
                          $"{gateStatus}\n" +
                          $"{bedStatus}";
    }

    // ================= CÁC HIỆU ỨNG HÙ DỌA (HORROR EFFECTS) =================

    /// <summary>
    /// Làm đèn pin nhấp nháy chập chờn và có thể tắt tối om 1 lát
    /// </summary>
    public void FlickerFlashlight(int flickerCount, float speed, float blackoutDuration = 0f)
    {
        StartCoroutine(FlickerFlashlightRoutine(flickerCount, speed, blackoutDuration));
    }

    private IEnumerator FlickerFlashlightRoutine(int flickerCount, float speed, float blackoutDuration)
    {
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController == null) yield break;

        Light flLight = fController.GetComponentInChildren<Light>();
        if (flLight == null || !flLight.enabled) yield break;

        bool originalState = flLight.enabled;

        for (int i = 0; i < flickerCount; i++)
        {
            flLight.enabled = !flLight.enabled;
            yield return new WaitForSeconds(speed);
            flLight.enabled = !flLight.enabled;
            yield return new WaitForSeconds(speed);
        }

        if (blackoutDuration > 0f)
        {
            flLight.enabled = false;
            yield return new WaitForSeconds(blackoutDuration);
            flLight.enabled = originalState;
        }
        else
        {
            flLight.enabled = originalState;
        }
    }

    /// <summary>
    /// Rung lắc màn hình nhẹ tạo cảm giác giật mình (Camera Shake)
    /// </summary>
    public void TriggerCameraShake(float duration, float magnitude)
    {
        if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
        cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }

    // Tự động tìm hoặc tạo Task Text góc trên trái màn hình
    private void EnsureTaskUI()
    {
        if (taskTextUI != null)
        {
            taskTextUI.gameObject.SetActive(true);
            if (taskTextUI.transform.parent != null)
            {
                taskTextUI.transform.parent.gameObject.SetActive(true);
            }
            return;
        }

        // 1. Tìm xem bên trong Player hoặc các con có sẵn TextMeshProUGUI tên Task chưa
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            if (t.gameObject.name.ToLower().Contains("task"))
            {
                taskTextUI = t;
                taskTextUI.gameObject.SetActive(true);
                if (t.transform.parent != null)
                {
                    t.transform.parent.gameObject.SetActive(true);
                }
                return;
            }
        }

        // 2. Tìm Canvas con bên trong Player hoặc trong Scene
        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        // 3. Nếu chưa có Canvas nào thì tự tạo mới
        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("TaskCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            targetCanvas.gameObject.SetActive(true);
        }

        GameObject textGO = new GameObject("TaskTextUI");
        textGO.transform.SetParent(targetCanvas.transform, false);

        TextMeshProUGUI newTmp = textGO.AddComponent<TextMeshProUGUI>();
        newTmp.fontSize = 24;
        newTmp.color = Color.white;
        newTmp.alignment = TextAlignmentOptions.TopLeft;

        // Tự động nạp Font Roboto-Bold SDF hoặc mặc định
        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont == null) robotoFont = TMP_Settings.defaultFontAsset;
        if (robotoFont != null)
        {
            newTmp.font = robotoFont;
        }

        RectTransform rt = newTmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f);
        rt.sizeDelta = new Vector2(480f, 250f);

        taskTextUI = newTmp;
    }

    private void OnDrawGizmos()
    {
        if (playerBoothTeleportPoint != null)
        {
            // Điểm đặt chân (Vòng tròn Cyan)
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(playerBoothTeleportPoint.position, 0.35f);

            // Chiều cao mô phỏng người chơi (1.8m)
            Vector3 headPos = playerBoothTeleportPoint.position + Vector3.up * 1.8f;
            Gizmos.DrawLine(playerBoothTeleportPoint.position, headPos);
            Gizmos.DrawWireSphere(headPos, 0.22f);

            // Hướng mắt nhìn (Mũi tên Vàng hướng về phía trước)
            Gizmos.color = Color.yellow;
            Vector3 eyePos = playerBoothTeleportPoint.position + Vector3.up * 1.6f;
            Vector3 forwardTarget = eyePos + playerBoothTeleportPoint.forward * 1.2f;
            Gizmos.DrawLine(eyePos, forwardTarget);
            Gizmos.DrawWireSphere(forwardTarget, 0.08f);
        }
    }
}
