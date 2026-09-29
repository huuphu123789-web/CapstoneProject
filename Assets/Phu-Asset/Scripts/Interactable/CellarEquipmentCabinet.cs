using System.Collections;
using UnityEngine;

/// <summary>
/// Chiếc tủ trong hầm Cellar chứa Đèn Pin và Súng.
/// Kế thừa Interactable (Phú-Asset) - Tương tác chuẩn phím [E].
/// Khi người chơi nhấn E:
/// 1. Mở khóa Đèn Pin (FlashlightController.EquipFlashlight()).
/// 2. Rút Súng ra trang bị trên tay (PlayerGun.DrawGun()).
/// 3. Cập nhật tiến độ nhiệm vụ trên TaskManager (đổi màu xanh [v]).
/// 4. Phát âm thanh nhặt đồ và ẩn các mô hình trưng bày trong tủ.
/// </summary>
public class CellarEquipmentCabinet : Interactable
{
    [Header("=== TRẠNG THÁI TỦ TRANG BỊ ===")]
    [Tooltip("Người chơi đã lấy trang bị từ tủ chưa?")]
    public bool hasTakenEquipment = false;

    [Header("=== CÂU NHẮC GIAO DIỆN [E] ===")]
    [Tooltip("Câu nhắc khi chưa lấy đồ")]
    public string takePrompt = "Take Flashlight & Gun";

    [Tooltip("Câu nhắc sau khi đã lấy đồ (để trống nếu muốn ẩn hẳn)")]
    public string emptyPrompt = "Cabinet is empty";

    [Header("=== MÔ HÌNH VẬT THỂ TRƯNG BÀY (TÙY CHỌN) ===")]
    [Tooltip("Mô hình đèn pin đặt trong tủ (sẽ biến mất khi nhặt)")]
    public GameObject displayFlashlightModel;

    [Tooltip("Mô hình súng đặt trong tủ (sẽ biến mất khi nhặt)")]
    public GameObject displayGunModel;

    [Tooltip("Tự động tạo mô hình 3D súng và đèn pin đặt trên mặt bàn tủ nếu chưa gán thủ công")]
    public bool autoCreateDisplayModels = true;

    [Header("=== HIỆU ỨNG & ÂM THANH ===")]
    [Tooltip("Âm thanh khi nhặt trang bị / mở tủ")]
    public AudioClip pickupSound;
    public AudioSource localAudioSource;

    [Tooltip("(Tùy chọn) Animator mở cánh tủ")]
    public Animator cabinetAnimator;
    public string openAnimTrigger = "Open";

    [Header("=== PREFAB SÚNG DỰ PHÒNG ===")]
    [Tooltip("Prefab khẩu súng để tự động gắn vào Camera nếu Scene chưa có")]
    public GameObject gunPrefab;

    void Awake()
    {
        // 1. Tự động kiểm tra và gắn BoxCollider nếu tủ chưa có collider nào
        Collider col = GetComponent<Collider>();
        if (col == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            // Kích thước bao trọn cả 2 khối tủ (Cabinet Table & Cabinet Table 1)
            box.size = new Vector3(5.5f, 3.5f, 2.5f);
            box.center = new Vector3(1.2f, 1f, 0f);
            box.isTrigger = false;
        }

        // 2. Thiết lập AudioSource
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.spatialBlend = 1f; // Âm thanh 3D tại tủ
                localAudioSource.playOnAwake = false;
            }
        }
    }

    void Start()
    {
        promptMessage = hasTakenEquipment ? emptyPrompt : takePrompt;

        if (hasTakenEquipment)
        {
            if (displayFlashlightModel != null) displayFlashlightModel.SetActive(false);
            if (displayGunModel != null) displayGunModel.SetActive(false);
        }
        else if (autoCreateDisplayModels)
        {
            EnsureDisplayModels();
        }
    }

    /// <summary>
    /// Tự động sinh mô hình 3D Súng & Đèn Pin nằm ngay ngắn trên mặt bàn tủ
    /// </summary>
    private void EnsureDisplayModels()
    {
        // 1. Mô hình Súng
        if (displayGunModel == null)
        {
            Transform foundGun = transform.Find("Display_Gun");
            if (foundGun != null)
            {
                displayGunModel = foundGun.gameObject;
            }
            else
            {
                GameObject prefabToUse = gunPrefab != null ? gunPrefab : Resources.Load<GameObject>("gun");
                if (prefabToUse != null)
                {
                    GameObject gunObj = Instantiate(prefabToUse, transform);
                    gunObj.name = "Display_Gun";
                    // Đặt nằm trên mặt bàn tủ
                    gunObj.transform.localPosition = new Vector3(0.5f, 1.75f, 0f);
                    gunObj.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                    gunObj.transform.localScale = Vector3.one * 1.5f;

                    // Gỡ các component logic khỏi mô hình trưng bày (chỉ giữ lại Mesh)
                    PlayerGun pg = gunObj.GetComponent<PlayerGun>();
                    if (pg != null) Destroy(pg);
                    AudioSource aSrc = gunObj.GetComponent<AudioSource>();
                    if (aSrc != null) Destroy(aSrc);
                    Collider[] colliders = gunObj.GetComponentsInChildren<Collider>();
                    foreach (var c in colliders) Destroy(c);

                    gunObj.SetActive(true);
                    foreach (Transform child in gunObj.transform)
                    {
                        child.gameObject.SetActive(true);
                    }

                    displayGunModel = gunObj;
                    Debug.Log("[CellarEquipmentCabinet] Đã tự động tạo mô hình Súng trưng bày trên tủ!");
                }
            }
        }

        // 2. Mô hình Đèn Pin
        if (displayFlashlightModel == null)
        {
            Transform foundFlashlight = transform.Find("Display_Flashlight");
            if (foundFlashlight != null)
            {
                displayFlashlightModel = foundFlashlight.gameObject;
            }
            else
            {
                GameObject flObj = new GameObject("Display_Flashlight");
                flObj.transform.SetParent(transform);
                flObj.transform.localPosition = new Vector3(1.2f, 1.75f, 0f);
                flObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                flObj.transform.localScale = new Vector3(0.2f, 0.45f, 0.2f);

                // Thân đèn hình trụ
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Flashlight_Body";
                body.transform.SetParent(flObj.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                Collider bodyCol = body.GetComponent<Collider>();
                if (bodyCol != null) Destroy(bodyCol);

                // Đầu đèn
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = "Flashlight_Head";
                head.transform.SetParent(flObj.transform);
                head.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                head.transform.localRotation = Quaternion.identity;
                head.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
                Collider headCol = head.GetComponent<Collider>();
                if (headCol != null) Destroy(headCol);

                // Mặt kính
                GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lens.name = "Flashlight_Lens";
                lens.transform.SetParent(flObj.transform);
                lens.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                lens.transform.localRotation = Quaternion.identity;
                lens.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
                Collider lensCol = lens.GetComponent<Collider>();
                if (lensCol != null) Destroy(lensCol);

                // Sơn màu
                Renderer bodyRend = body.GetComponent<Renderer>();
                if (bodyRend != null) bodyRend.material.color = new Color(0.15f, 0.15f, 0.15f);
                Renderer headRend = head.GetComponent<Renderer>();
                if (headRend != null) headRend.material.color = new Color(0.1f, 0.1f, 0.1f);
                Renderer lensRend = lens.GetComponent<Renderer>();
                if (lensRend != null) lensRend.material.color = new Color(1f, 0.95f, 0.6f);

                displayFlashlightModel = flObj;
                Debug.Log("[CellarEquipmentCabinet] Đã tự động tạo mô hình Đèn Pin trưng bày trên tủ!");
            }
        }
    }

    public override void Interact()
    {
        if (hasTakenEquipment)
        {
            Debug.Log("[CellarEquipmentCabinet] Tủ đã hết trang bị.");
            return;
        }

        hasTakenEquipment = true;
        promptMessage = emptyPrompt;

        // 1. Mở khóa Đèn Pin
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) fController = player.GetComponentInChildren<FlashlightController>(true);
        }

        if (fController != null)
        {
            fController.EquipFlashlight(turnOnImmediately: false);
            Debug.Log("[CellarEquipmentCabinet] Đã mở khóa ĐÈN PIN cho người chơi (nhấn F để bật/tắt).");
        }
        else
        {
            Debug.LogWarning("[CellarEquipmentCabinet] Không tìm thấy FlashlightController trên Player!");
        }

        // 2. Rút Súng trên tay người chơi
        PlayerGun gun = null;

        // Tìm trong Scene (kể cả object đang bị ẩn/inactive)
#if UNITY_2023_1_OR_NEWER
        gun = FindAnyObjectByType<PlayerGun>(FindObjectsInactive.Include);
#else
        PlayerGun[] allGuns = Resources.FindObjectsOfTypeAll<PlayerGun>();
        foreach (var g in allGuns)
        {
            if (g.gameObject.scene.isLoaded)
            {
                gun = g;
                break;
            }
        }
#endif

        // Tìm trong cây phân cấp của Player hoặc Camera
        if (gun == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                gun = player.GetComponentInChildren<PlayerGun>(true);
            }
        }
        if (gun == null && Camera.main != null)
        {
            gun = Camera.main.GetComponentInChildren<PlayerGun>(true);
        }

        // Nếu Scene hoàn toàn chưa có Súng thì tự spawn prefab súng gắn vào Camera
        if (gun == null)
        {
            if (gunPrefab == null)
            {
                gunPrefab = Resources.Load<GameObject>("gun");
            }

            if (gunPrefab != null && Camera.main != null)
            {
                GameObject gunObj = Instantiate(gunPrefab, Camera.main.transform);
                gunObj.name = "gun";
                gunObj.transform.localPosition = new Vector3(0.12f, -0.026f, -0.021f);
                gunObj.transform.localRotation = Quaternion.identity;
                gun = gunObj.GetComponent<PlayerGun>();
                Debug.Log("[CellarEquipmentCabinet] Đã tự động tạo Súng từ gunPrefab gắn vào Camera!");
            }
        }

        if (gun != null)
        {
            gun.DrawGun();
            Debug.Log("[CellarEquipmentCabinet] Đã trang bị SÚNG lên tay người chơi.");
        }
        else
        {
            Debug.LogWarning("[CellarEquipmentCabinet] Không tìm thấy PlayerGun trong Scene!");
        }

        // 3. Ẩn mô hình trưng bày trong tủ
        if (displayFlashlightModel != null) displayFlashlightModel.SetActive(false);
        if (displayGunModel != null) displayGunModel.SetActive(false);

        // 4. Mở cánh tủ nếu có Animator
        if (cabinetAnimator != null && !string.IsNullOrEmpty(openAnimTrigger))
        {
            cabinetAnimator.SetTrigger(openAnimTrigger);
        }

        // 5. Phát âm thanh nhặt đồ
        PlayPickupSound();

        // 6. Báo cáo hoàn thành nhiệm vụ lấy trang bị về TaskManager
        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteEquipmentTask();
        }

        Debug.Log("[CellarEquipmentCabinet] Người chơi đã lấy ĐÈN PIN và SÚNG thành công!");
    }

    private void PlayPickupSound()
    {
        AudioClip clip = (pickupSound != null) ? pickupSound : interactSound;
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
}
