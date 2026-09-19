using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script dùng cho Giường ngủ (Bed).
/// Kế thừa Interactable (Phú-Asset) - Nhấn phím E để nằm lên giường, ngửa mặt nhìn lên trần nhà, màn hình từ từ tối dần và chuyển Scene!
/// </summary>
public class BedInteractable : Interactable
{
    [Header("=== VỊ TRÍ NẰM NGỦ TRÊN GIƯỜNG ===")]
    [Tooltip("Kéo điểm LieDownPoint trên giường vào đây")]
    public Transform lieDownPoint;

    [Header("=== GÓC NHÌN LÊN TRẦN NHÀ ===")]
    [Tooltip("Góc ngẩng đầu nhìn lên trần nhà (-60 đến -80 độ là nhìn lên trần)")]
    public float lookUpAngle = -60f;

    [Header("=== THỜI GIAN HIỆU ỨNG (CINEMATIC TIMING) ===")]
    [Tooltip("Thời gian Player di chuyển và ngả người nằm xuống giường (giây)")]
    public float lieDownDuration = 2.0f;

    [Tooltip("Thời gian nằm ngắm trần nhà TRƯỚC KHI màn hình bắt đầu tối (giây)")]
    public float waitBeforeFade = 2.0f;

    [Tooltip("Thời gian màn hình từ từ tối đen dần (Fade Duration)")]
    public float fadeDuration = 2.5f;

    [Tooltip("Thời gian chờ trong bóng tối trước khi nạp Scene mới")]
    public float waitBeforeLoad = 1.5f;

    [Header("=== CHUYỂN LEVEL ===")]
    [Tooltip("Tên Scene tiếp theo cần tải (VD: Night-2, Level2...)")]
    public string nextSceneName = "Night-2";

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh khi lên giường ngủ (tiếng chăn gối / thở dài...)")]
    public AudioClip sleepSound;
    public AudioSource localAudioSource;

    [Header("=== XUẤT PHÁT TẠI GIƯỜNG (GAME START) ===")]
    [Tooltip("Tự động đặt Player xuất phát tại cạnh giường khi vào game")]
    public bool spawnPlayerAtBedOnStart = true;

    [Tooltip("Hiệu ứng mở mắt / mờ sáng dần khi thức dậy")]
    public bool fadeInOnStart = true;
    public float fadeInDuration = 1.2f;

    [Tooltip("Điểm thức dậy cạnh giường (nếu có, để trống thì tự động tính bên hông giường nhìn vào phòng)")]
    public Transform wakeupPoint;

    private bool isSleeping = false;
    private float currentFadeAlpha = 0f; // Alpha màn hình đen (0 = trong suốt, 1 = đen kịt)
    private Camera mainCam;
    private Transform playerTransform;

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
        promptMessage = "Go to sleep";

        // Tự động tìm hoặc tạo lieDownPoint nếu chưa gán
        if (lieDownPoint == null)
        {
            Transform foundPoint = transform.Find("LieDownPoint");
            if (foundPoint != null)
            {
                lieDownPoint = foundPoint;
            }
            else
            {
                GameObject autoPoint = new GameObject("LieDownPoint");
                autoPoint.transform.SetParent(transform);
                // Vị trí nằm trên đệm giường tầng dưới
                autoPoint.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                autoPoint.transform.localRotation = Quaternion.identity;
                lieDownPoint = autoPoint.transform;
            }
        }

        // Tự động đảm bảo các model súng & đèn pin trong Cellar có script EquipmentPickup
        EnsureEquipmentPickupsSetup();

        // Tự động chuẩn bị sẵn Súng trên Camera ở trạng thái cất trong tủ
        EnsurePlayerGunSetup();

        // Đưa Player về cạnh giường thức dậy khi bắt đầu game
        if (spawnPlayerAtBedOnStart)
        {
            if (fadeInOnStart) currentFadeAlpha = 1f;
            StartCoroutine(SpawnPlayerRoutine());
        }
    }

    /// <summary>
    /// Hàm dùng chung để tải Prefab súng Pistol 92 (hỗ trợ cả Resources và AssetDatabase trong Editor)
    /// </summary>
    public static GameObject LoadGunPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("gun");
        if (prefab == null) prefab = Resources.Load<GameObject>("Pistol 92");
#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Resources/gun.prefab");
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/Player/gun.prefab");
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/ASSETSTORE/Player/Pistol 92/Prefabs/Pistol 92 Black.prefab");
#endif
        return prefab;
    }

    private void EnsurePlayerGunSetup()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) cam = player.GetComponentInChildren<Camera>(true);
        }
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        PlayerGun existingGun = cam.GetComponentInChildren<PlayerGun>(true);
        if (existingGun != null)
        {
            // Kiểm tra nếu khẩu súng trên camera là súng cũ (không có mesh Cube.000 của Pistol 92) thì hủy ngay để thay mới
            if (existingGun.transform.Find("Cube.000") == null)
            {
                DestroyImmediate(existingGun.gameObject);
                existingGun = null;
            }
        }

        if (existingGun == null)
        {
            GameObject gunPrefab = LoadGunPrefab();
            if (gunPrefab != null)
            {
                GameObject gunObj = Instantiate(gunPrefab, cam.transform);
                gunObj.name = "gun";
                // Tọa độ & góc xoay chuẩn FPS cho Pistol 92 (nòng súng hướng thẳng về phía trước, hiển thị rõ góc phải)
                gunObj.transform.localPosition = new Vector3(0.16f, -0.12f, 0.32f);
                gunObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                gunObj.transform.localScale = Vector3.one * 0.28f;

                existingGun = gunObj.GetComponentInChildren<PlayerGun>(true);
                if (existingGun == null)
                {
                    existingGun = gunObj.AddComponent<PlayerGun>();
                }
                if (existingGun != null)
                {
                    existingGun.DisableGunColliders();
                    existingGun.HolsterGun();
                }
                Debug.Log("[BedInteractable] Đã chuẩn bị sẵn Súng Pistol 92 trên Camera người chơi (đang cất trong tủ)!");
            }
            else
            {
                Debug.LogError("[BedInteractable] Không tìm thấy Gun Prefab để khởi tạo trên Camera!");
            }
        }
    }

    private void EnsureEquipmentPickupsSetup()
    {
        Transform cellarRoot = (transform.parent != null && transform.parent.name == "Bed") ? transform.parent.parent : transform.parent;
        if (cellarRoot == null) return;

        // 1. Dọn dẹp sạch CellarEquipmentCabinet nếu còn sót trên Cellar hoặc Cabinet
        CellarEquipmentCabinet oldRootCabinet = cellarRoot.GetComponent<CellarEquipmentCabinet>();
        if (oldRootCabinet != null) Destroy(oldRootCabinet);

        Transform cabinetObj = cellarRoot.Find("Cabinet");
        if (cabinetObj != null)
        {
            CellarEquipmentCabinet oldCabinet = cabinetObj.GetComponent<CellarEquipmentCabinet>();
            if (oldCabinet != null) Destroy(oldCabinet);
        }

        // 2. Tự động tìm và gắn EquipmentPickup cho Display_flashlight và Display_gun / Pistol 92
        Transform[] allChildren = cellarRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            string lower = t.name.ToLower();
            if (lower == "display_flashlight" || (lower.Contains("flashlight") && !lower.Contains("player") && !lower.Contains("controller") && !lower.Contains("point light") && !lower.Contains("light")))
            {
                EquipmentPickup p = t.GetComponent<EquipmentPickup>();
                if (p == null)
                {
                    p = t.gameObject.AddComponent<EquipmentPickup>();
                    p.equipmentType = EquipmentPickupType.Flashlight;
                    p.promptMessage = "Take Flashlight";
                    Debug.Log($"[Bed] Đã tự động gắn EquipmentPickup (Flashlight) vào {t.name}!");
                }
                p.FitBoxColliderToMesh();
            }
            else if (lower == "display_gun" || lower.Contains("pistol") || (lower.Contains("gun") && !lower.Contains("player") && !lower.Contains("shot") && !lower.Contains("controller") && !lower.Contains("prefab")))
            {
                // Nếu vô tình có PlayerGun hoặc AudioSource trên vật thể trong tủ thì gỡ bỏ
                PlayerGun misplacedGun = t.GetComponent<PlayerGun>();
                if (misplacedGun != null) Destroy(misplacedGun);
                AudioSource misplacedAudio = t.GetComponent<AudioSource>();
                if (misplacedAudio != null) Destroy(misplacedAudio);

                EquipmentPickup p = t.GetComponent<EquipmentPickup>();
                if (p == null)
                {
                    p = t.gameObject.AddComponent<EquipmentPickup>();
                    p.equipmentType = EquipmentPickupType.Gun;
                    p.promptMessage = "Take Gun";
                    Debug.Log($"[Bed] Đã tự động gắn EquipmentPickup (Gun) vào {t.name}!");
                }
                p.FitBoxColliderToMesh();
            }
        }
    }

    private IEnumerator SpawnPlayerRoutine()
    {
        // Chờ 1 frame để tất cả GameObject và Player khởi tạo hoàn tất
        yield return null;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Transform bedParent = (transform.parent != null && transform.parent.name == "Bed") ? transform.parent : transform;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (wakeupPoint != null)
            {
                spawnPos = wakeupPoint.position;
                spawnRot = wakeupPoint.rotation;
            }
            else
            {
                Transform foundWakeup = bedParent.Find("BedWakeupPoint");
                if (foundWakeup != null)
                {
                    wakeupPoint = foundWakeup;
                    spawnPos = wakeupPoint.position;
                    spawnRot = wakeupPoint.rotation;
                }
                else
                {
                    GameObject pt = new GameObject("BedWakeupPoint");
                    pt.transform.SetParent(bedParent);
                    // Ở local của Bed: x = -1.2m (bên hông giường ra lòng phòng), y = 0.05m chạm sàn, z = 0m
                    pt.transform.localPosition = new Vector3(-1.2f, 0.05f, 0f);
                    // Quay mặt nhìn ra giữa phòng / hướng tủ trang bị
                    pt.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    wakeupPoint = pt.transform;
                    spawnPos = wakeupPoint.position;
                    spawnRot = wakeupPoint.rotation;
                }
            }

            player.transform.position = spawnPos;
            player.transform.rotation = spawnRot;

            // Đồng bộ Cinemachine Camera nếu có
            var panTilt = player.GetComponentInChildren<Unity.Cinemachine.CinemachinePanTilt>();
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = spawnRot.eulerAngles.y;
                panTilt.TiltAxis.Value = 0f;
            }

            var vcam = player.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null)
            {
                vcam.PreviousStateIsValid = false;
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = spawnPos + Vector3.up * 1.6f;
                cam.transform.rotation = spawnRot;
            }

            yield return null;
            if (cc != null) cc.enabled = true;

            Debug.Log($"[Bed] Đã đặt Player xuất phát tại giường ngủ: {spawnPos}");
        }

        // Hiệu ứng mở mắt từ từ sáng dần
        if (fadeInOnStart && currentFadeAlpha > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                currentFadeAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                yield return null;
            }
            currentFadeAlpha = 0f;
        }
    }

    void Update()
    {
        if (isSleeping) return;

        // Cập nhật câu nhắc nhở tùy theo tiến độ nhiệm vụ
        if (TaskManager.instance != null && !TaskManager.instance.AreAllTasksCompleted())
        {
            promptMessage = "Complete chores first!";
        }
        else
        {
            promptMessage = "Go to sleep";
        }
    }

    public override void Interact()
    {
        if (isSleeping) return;

        // Kiểm tra xem đã hoàn thành tất cả nhiệm vụ chưa
        if (TaskManager.instance != null && !TaskManager.instance.AreAllTasksCompleted())
        {
            Debug.Log("[Bed] Chưa thể đi ngủ! Bạn cần hoàn thành các nhiệm vụ ngoài sân trước.");
            return;
        }

        isSleeping = true;
        promptMessage = ""; // Ẩn gợi ý tương tác

        StartCoroutine(SleepAndTransitionRoutine());
    }

    private IEnumerator SleepAndTransitionRoutine()
    {
        Debug.Log("[Bed] Bắt đầu quá trình nằm ngủ...");

        mainCam = Camera.main;

        // 1. Tìm Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        if (player != null)
        {
            playerTransform = player.transform;
        }

        // 2. Tắt di chuyển chuột & bàn phím
        DisablePlayerInput();

        // 3. Phát âm thanh chăn gối
        PlaySleepSound();

        // 4. Ẩn HUD gameplay
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.ShowHUD(false);
        }

        // Vô hiệu hóa CharacterController để Player có thể dịch chuyển mượt mà
        CharacterController charController = (player != null) ? player.GetComponent<CharacterController>() : null;
        if (charController != null) charController.enabled = false;

        // 5. Tính toán vị trí Player và Camera
        Vector3 startPlayerPos = (playerTransform != null) ? playerTransform.position : transform.position;
        Quaternion startPlayerRot = (playerTransform != null) ? playerTransform.rotation : transform.rotation;

        Vector3 targetPlayerPos = lieDownPoint.position;
        Quaternion targetPlayerRot = lieDownPoint.rotation;

        // Vị trí Camera trên gối
        Vector3 startCamPos = (mainCam != null) ? mainCam.transform.position : startPlayerPos;
        Quaternion startCamRot = (mainCam != null) ? mainCam.transform.rotation : startPlayerRot;

        Vector3 targetCamPos = lieDownPoint.position + (Vector3.up * 0.2f);
        Quaternion targetCamRot = Quaternion.Euler(lookUpAngle, targetPlayerRot.eulerAngles.y, 0f);

        // 6. Di chuyển Player & Camera mượt mà về phía giường và ngửa lên trần nhà
        float elapsed = 0f;
        while (elapsed < lieDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lieDownDuration);

            // Di chuyển Player
            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, t);
                playerTransform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, t);
            }

            // Di chuyển & xoay Camera ngửa mặt lên trần
            if (mainCam != null)
            {
                mainCam.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
                mainCam.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
            }

            yield return null;
        }

        // Đảm bảo ở đúng vị trí cuối cùng
        if (playerTransform != null)
        {
            playerTransform.position = targetPlayerPos;
            playerTransform.rotation = targetPlayerRot;
        }
        if (mainCam != null)
        {
            mainCam.transform.position = targetCamPos;
            mainCam.transform.rotation = targetCamRot;
        }

        // 7. Nằm ngắm trần nhà trong khoảng thời gian đã cài đặt
        Debug.Log("[Bed] Đang nằm ngắm trần nhà...");
        float waitTimer = 0f;
        while (waitTimer < waitBeforeFade)
        {
            waitTimer += Time.deltaTime;
            if (mainCam != null)
            {
                mainCam.transform.position = targetCamPos;
                mainCam.transform.rotation = targetCamRot;
            }
            yield return null;
        }

        // 8. Bắt đầu mờ đen dần từ từ (Fade to Black: 0% -> 100%)
        Debug.Log("[Bed] Màn hình bắt đầu tối dần...");
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            currentFadeAlpha = Mathf.Clamp01(fadeElapsed / fadeDuration);

            if (mainCam != null)
            {
                mainCam.transform.position = targetCamPos;
                mainCam.transform.rotation = targetCamRot;
            }
            yield return null;
        }

        currentFadeAlpha = 1f;

        // 9. Tắt đèn pin sau khi màn hình đã đen hoàn toàn
        TurnOffFlashlight();

        // 10. Chờ trong bóng tối
        yield return new WaitForSeconds(waitBeforeLoad);

        // 11. Chuyển sang Scene tiếp theo
        Debug.Log($"[Bed] Tải Scene tiếp theo: {nextSceneName}");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[Bed] Chưa điền nextSceneName trong Inspector!");
        }
    }

    private void DisablePlayerInput()
    {
        // Tắt Player Movement
        PlayerController pController = FindObjectOfType<PlayerController>();
        if (pController != null) pController.enabled = false;

        // Tắt PlayerBodyRotator
        PlayerBodyRotator bRotator = FindObjectOfType<PlayerBodyRotator>();
        if (bRotator != null) bRotator.enabled = false;

        // Tắt Player Interact
        PlayerInteract pInteract = FindObjectOfType<PlayerInteract>();
        if (pInteract != null) pInteract.enabled = false;

        // Tắt CinemachineBrain trên Camera để giải phóng Camera tự do di chuyển & xoay
        if (mainCam != null)
        {
            MonoBehaviour brain = mainCam.GetComponent("CinemachineBrain") as MonoBehaviour;
            if (brain != null) brain.enabled = false;
        }
    }

    private void TurnOffFlashlight()
    {
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController != null)
        {
            Light flLight = fController.GetComponentInChildren<Light>();
            if (flLight != null) flLight.enabled = false;
            fController.enabled = false;
        }
    }

    private void PlaySleepSound()
    {
        AudioClip clip = (sleepSound != null) ? sleepSound : interactSound;
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

    // Vẽ màn hình mờ đen bằng OnGUI - 100% không bao giờ bị lỗi Canvas, luôn mượt và chuẩn xác
    void OnGUI()
    {
        if (currentFadeAlpha > 0f)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, currentFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (lieDownPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lieDownPoint.position, 0.25f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lieDownPoint.position + (Vector3.up * 0.2f), 0.15f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(lieDownPoint.position, lieDownPoint.forward * 0.8f);
        }
    }
}
