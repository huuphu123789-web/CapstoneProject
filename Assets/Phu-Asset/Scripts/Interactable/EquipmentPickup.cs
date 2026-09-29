using System.Collections;
using UnityEngine;

/// <summary>
/// Loại vật phẩm trang bị có thể nhặt trực tiếp bằng phím E.
/// </summary>
public enum EquipmentPickupType
{
    Flashlight,
    Gun,
    Ammo
}

/// <summary>
/// Script gắn trực tiếp lên Model 3D của Đèn Pin hoặc Súng (trong ngăn kéo / trên mặt bàn tủ).
/// Kế thừa Interactable (Phú-Asset).
/// - Khi người chơi nhìn thẳng vào Model: Hiện "[E] - Take Flashlight" hoặc "[E] - Take Gun".
/// - Khi nhấn E: Tự động mở khóa trang bị cho Player, phát âm thanh, biến mất khỏi tủ và cập nhật Task HUD.
/// </summary>
public class EquipmentPickup : Interactable
{
    [Header("=== LOẠI TRANG BỊ ===")]
    [Tooltip("Chọn loại trang bị: Flashlight (Đèn pin), Gun (Súng), hoặc Ammo (Hộp đạn)")]
    public EquipmentPickupType equipmentType = EquipmentPickupType.Flashlight;

    [Tooltip("(Dành cho Ammo) Số viên đạn nhận được khi nhặt")]
    public int ammoCount = 3;

    [Header("=== ÂM THANH & HIỆU ỨNG ===")]
    [Tooltip("Âm thanh khi nhặt món đồ")]
    public AudioClip pickupSound;
    public AudioSource localAudioSource;

    [Header("=== PREFAB SÚNG DỰ PHÒNG ===")]
    [Tooltip("(Dành cho Gun) Prefab súng gắn vào Camera nếu Scene chưa có sẵn")]
    public GameObject gunPrefab;

    [Header("=== CẤU HÌNH VÙNG TƯƠNG TÁC (BOX COLLIDER) ===")]
    [Tooltip("Tự động tính Center và Size để BoxCollider ôm trọn chính xác Mesh (kể cả khi mesh bị lệch pivot con)")]
    public bool autoFitBoxCollider = true;

    void Awake()
    {
        // 1. Tự động kiểm tra và gắn BoxCollider khớp chuẩn xác với Renderers (kể cả khi mesh bị lệch pivot con)
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            Collider col = GetComponent<Collider>();
            if (col == null && GetComponentInChildren<Collider>() == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }
        }

        if (box != null)
        {
            if (autoFitBoxCollider)
            {
                FitBoxColliderToMesh(box);
            }
            box.isTrigger = false;
        }

        // 2. Thiết lập AudioSource
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.spatialBlend = 1f; // Âm thanh 3D tại vị trí vật phẩm
                localAudioSource.playOnAwake = false;
            }
        }
    }

    [ContextMenu("Fit BoxCollider to Mesh")]
    public void ManualFitBoxCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        FitBoxColliderToMesh(box);
    }

    void Reset()
    {
        promptMessage = (equipmentType == EquipmentPickupType.Flashlight) ? "Take Flashlight" : (equipmentType == EquipmentPickupType.Gun ? "Take Gun" : "Take Ammo");
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) FitBoxColliderToMesh(box);
    }

    void OnValidate()
    {
        if (!autoFitBoxCollider) return;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            FitBoxColliderToMesh(box);
        }
    }

    void Start()
    {
        // Đảm bảo BoxCollider luôn ôm trọn đúng vị trí mesh thực tế
        if (autoFitBoxCollider)
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                FitBoxColliderToMesh(box);
            }
        }

        // Thiết lập câu nhắc nhở tương tác theo loại đồ
        if (string.IsNullOrEmpty(promptMessage) || promptMessage == "Interact")
        {
            promptMessage = (equipmentType == EquipmentPickupType.Flashlight) ? "Take Flashlight" : (equipmentType == EquipmentPickupType.Gun ? "Take Gun" : "Take Ammo");
        }
    }

    /// <summary>
    /// Tự động căn chỉnh BoxCollider bao bọc chuẩn xác toàn bộ Mesh thực tế,
    /// tự tính toán Center và Size dựa trên Renderers để không bao giờ bị lệch pivot.
    /// </summary>
    public void FitBoxColliderToMesh(BoxCollider box = null)
    {
        if (box == null) box = GetComponent<BoxCollider>();
        if (box == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Chuyển tâm Bounds từ World Space sang Local Space của GameObject chứa BoxCollider
            box.center = transform.InverseTransformPoint(worldBounds.center);

            // Chuyển kích thước từ World Space sang Local Space theo lossyScale
            Vector3 worldSize = worldBounds.size;
            Vector3 lossy = transform.lossyScale;
            float sx = (lossy.x != 0) ? Mathf.Abs(worldSize.x / lossy.x) : 0.4f;
            float sy = (lossy.y != 0) ? Mathf.Abs(worldSize.y / lossy.y) : 0.3f;
            float sz = (lossy.z != 0) ? Mathf.Abs(worldSize.z / lossy.z) : 0.4f;

            // Đệm thêm 25% (padding) để người chơi dễ ngắm trúng trong bóng tối
            box.size = new Vector3(
                Mathf.Max(sx * 1.25f, 0.25f),
                Mathf.Max(sy * 1.25f, 0.2f),
                Mathf.Max(sz * 1.25f, 0.25f)
            );
        }
        else
        {
            box.size = new Vector3(0.5f, 0.4f, 0.5f);
            box.center = Vector3.zero;
        }
    }

    public override void Interact()
    {
        PlayPickupSound();

        if (equipmentType == EquipmentPickupType.Flashlight)
        {
            // 1. Mở khóa Đèn Pin cho người chơi
            FlashlightController fController = FindObjectOfType<FlashlightController>();
            if (fController == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) fController = player.GetComponentInChildren<FlashlightController>(true);
            }

            if (fController != null)
            {
                fController.EquipFlashlight(turnOnImmediately: false);
                Debug.Log("[EquipmentPickup] Đã nhặt ĐÈN PIN! Nhấn phím F để bật/tắt.");
            }
            else
            {
                Debug.LogWarning("[EquipmentPickup] Không tìm thấy FlashlightController trên Player!");
            }

            // 2. Báo cáo tiến độ về TaskManager
            if (TaskManager.instance != null)
            {
                TaskManager.instance.CollectFlashlight();
            }
        }
        else if (equipmentType == EquipmentPickupType.Gun)
        {
            // 1. Rút Súng trên Camera người chơi
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) cam = player.GetComponentInChildren<Camera>(true);
            }
            if (cam == null) cam = FindObjectOfType<Camera>();

            PlayerGun gun = null;
            if (cam != null)
            {
                gun = cam.GetComponentInChildren<PlayerGun>(true);
            }

            // Kiểm tra nếu khẩu súng hiện tại là súng cũ (không có mesh Cube.000 của Pistol 92) thì hủy ngay để thay mới
            if (gun != null && gun.transform.Find("Cube.000") == null)
            {
                DestroyImmediate(gun.gameObject);
                gun = null;
            }

            // Nếu trên Camera chưa có súng thì tạo ngay khẩu Pistol 92 gắn vào Camera
            if (gun == null && cam != null)
            {
                if (gunPrefab == null)
                {
                    gunPrefab = BedInteractable.LoadGunPrefab();
                }

                if (gunPrefab != null)
                {
                    GameObject gunObj = Instantiate(gunPrefab, cam.transform);
                    gunObj.name = "gun";
                    // Tọa độ & góc xoay chuẩn FPS cho Pistol 92
                    gunObj.transform.localPosition = new Vector3(0.16f, -0.12f, 0.32f);
                    gunObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    gunObj.transform.localScale = Vector3.one * 0.28f;

                    gun = gunObj.GetComponentInChildren<PlayerGun>(true);
                    if (gun == null)
                    {
                        gun = gunObj.AddComponent<PlayerGun>();
                    }
                    if (gun != null)
                    {
                        gun.DisableGunColliders();
                    }
                    Debug.Log("[EquipmentPickup] Đã tự động tạo Súng Pistol 92 gắn vào Camera!");
                }
            }

            if (gun != null)
            {
                gun.gameObject.SetActive(true);
                gun.HasGun = true;

                // Nếu người chơi đã nhặt đạn trước đó, bảo toàn số đạn đã nhặt!
                // Chỉ đặt về 0 nếu người chơi thực sự chưa nhặt viên đạn nào.
                int currentAvailableAmmo = Mathf.Max(gun.CurrentAmmo, PlayerGun.storedAmmo);
                gun.SetAmmo(currentAvailableAmmo);

                gun.DrawGun();
                Debug.Log($"[EquipmentPickup] Đã nhặt và trang bị SÚNG PISTOL 92 lên tay! (Số đạn hiện có: {gun.CurrentAmmo}/{gun.MaxAmmo})");
            }
            else
            {
                Debug.LogError("[EquipmentPickup] Không tìm thấy hoặc không tạo được PlayerGun trên Camera!");
            }

            // 2. Báo cáo tiến độ về TaskManager
            if (TaskManager.instance != null)
            {
                TaskManager.instance.CollectGun();
            }
        }
        else if (equipmentType == EquipmentPickupType.Ammo)
        {
            // Nhặt đạn cho súng
            PlayerGun gun = FindObjectOfType<PlayerGun>();
            if (gun == null)
            {
                Camera cam = Camera.main;
                if (cam != null) gun = cam.GetComponentInChildren<PlayerGun>(true);
            }

            if (gun != null)
            {
                gun.AddAmmo(ammoCount);
            }
            else
            {
                PlayerGun.storedAmmo = Mathf.Clamp(PlayerGun.storedAmmo + ammoCount, 0, 6);
            }

            int total = (gun != null) ? gun.CurrentAmmo : PlayerGun.storedAmmo;
            Debug.Log($"[EquipmentPickup] Đã nhặt thêm {ammoCount} viên đạn! (Tổng đạn trong túi/súng: {total})");
        }

        // 3. Ẩn mô hình này đi (đã nhặt vào người)
        gameObject.SetActive(false);
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
