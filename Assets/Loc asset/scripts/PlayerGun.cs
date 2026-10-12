using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quan ly hanh vi ban sung cua nhan vat.
/// Chi khai hoa khi Raycast trung NPC co Tag "NPC".
/// Ban xong tu dong cất súng (an GameObject sung di).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerGun : MonoBehaviour
{
    [Header("=== Cau Hinh Ban ===")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float shootRange = 50f;

    [Header("=== Hieu Ung Dau Nong Sung (Muzzle) ===")]
    [Tooltip("Diem dat dau nong sung (Muzzle Point). Neu de trong, script se tu dong tao tai dau nong.")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Light muzzleLight;
    [SerializeField] private float muzzleFlashDuration = 0.05f;
    [Tooltip("Prefab tia lua dau nong sung (Muzzle Flash Prefab). Keo tha Prefab Particle vao day de tao lua dau nong khi ban.")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [Tooltip("Particle tia lua dau nong truc tiep neu co (tuy chon)")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;
    [Tooltip("Toa do cuc bo cua dau nong sung (PB Pistol kem nong giam thanh: X=0, Y=0.114, Z=0.275)")]
    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 0.114f, 0.275f);

    [Header("=== Am Thanh Sung ===")]
    [SerializeField] private AudioClip gunshotClip;

    [Header("=== Cuong Do Hieu Ung ===")]
    [Range(0f, 1f)] [SerializeField] private float flashIntensity = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float tinnitusIntensity = 0.7f;

    [Header("=== Che Do Ban ===")]
    [Tooltip("Tick = Chi ban duoc khi dang ngam trung NPC. Khong tick = Cho phep ban tu do (bam chuot trai ban bat ky luc nao)")]
    [SerializeField] private bool requireAimingAtNPC = false;

    [Header("=== Cat Sung Sau Khi Ban ===")]
    [Tooltip("Tu dong cat sung sau khi ban trung NPC")]
    [SerializeField] private bool holsterAfterShoot = false;

    [Tooltip("Thoi gian cho (giay) truoc khi cat sung (de hieu ung chop/u tai chay xong)")]
    [SerializeField] private float holsterDelay = 0.4f;

    [Tooltip("(Tuy chon) Animator cua sung neu co animation. De trong = tu tim tren chinh GameObject nay hoac con.")]
    [SerializeField] private Animator gunAnimator;

    [Tooltip("Ten Trigger trong Animator de chay animation khi ban sung")]
    [SerializeField] private string shootAnimTrigger = "Shoot";

    [Tooltip("Ten Trigger trong Animator de chay animation cat sung")]
    [SerializeField] private string holsterAnimTrigger = "Holster";

    [Tooltip("Ten Trigger trong Animator de chay animation rut sung")]
    [SerializeField] private string drawAnimTrigger = "Draw";

    private bool HasParameter(Animator anim, string paramName)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    [Tooltip("(Tuy chon) GameObject rieng lam model sung. De trong = tu dung chinh gameObject nay.")]
    [SerializeField] private GameObject gunModelObject;

    [Header("=== Do Giat Sung (Recoil) ===")]
    [Tooltip("Tick = Dung Recoil code muot ma (khuyen dung de sung khong bi giat lech/bien dang). Khong tick = Dung Animator")]
    [SerializeField] private bool useProceduralRecoil = true;
    [Tooltip("Do nay giat nguoc ve sau (truc doc)")]
    [SerializeField] private float recoilKickBack = 0.025f;
    [Tooltip("Goc ngua nong sung len (do)")]
    [SerializeField] private float recoilKickUp = 2.5f;
    [Tooltip("Thoi gian nay sung giat (giay)")]
    [SerializeField] private float recoilDuration = 0.12f;

    [Header("=== Cau Hinh Dan (Ammo) ===")]
    [Tooltip("So vien dan toi da trong 1 bang / 1 man choi")]
    [SerializeField] private int maxAmmo = 6;
    [Tooltip("So vien dan hien co (khi chua nhat hop dan thi bang 0)")]
    [SerializeField] private int currentAmmo = 0;

    /// <summary>
    /// Lưu trữ số đạn dự trữ người chơi đã nhặt (ngay cả khi chưa nhặt súng)
    /// </summary>
    public static int storedAmmo = 0;

    [Tooltip("Am thanh khi het dan (bop co tach tach)")]
    [SerializeField] private AudioClip emptyGunClip;

    [Header("=== Cau Hinh NPC ===")]
    [SerializeField] private string npcTag = "NPC";
    [SerializeField] private bool debugAiming = false;

    [Tooltip("Tick = sung dang cat trong tu luc bat dau game. Khong tick = cam sung ngay tu dau.")]
    [SerializeField] private bool startHolstered = true;

    [Header("=== Sở Hữu Súng ===")]
    [Tooltip("Người chơi đã sở hữu súng chưa. Khi đã sở hữu thì có thể dùng phím 1 hoặc lăn chuột để rút/cất súng.")]
    [SerializeField] private bool hasGun = false;

    public bool HasGun
    {
        get => hasGun || _hasBeenExplicitlyDrawn || (TaskManager.instance != null && TaskManager.instance.hasGun);
        set
        {
            hasGun = value;
            if (value) _hasBeenExplicitlyDrawn = true;
        }
    }

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    // ── Noi bo ──
    private AudioSource _audioSource;
    private float _nextFireTime = 0f;
    private float _nextToggleTime = 0f;
    private Coroutine _muzzleFlashCoroutine;
    private Coroutine _recoilCoroutine;
    private Vector3 _defaultLocalPos = new Vector3(0.16f, -0.12f, 0.32f);
    private Quaternion _defaultLocalRot = Quaternion.Euler(0f, -90f, 0f);
    private Camera _cam;
    private bool _isHolstered = false;
    private bool _hasBeenExplicitlyDrawn = false;

    public bool IsAimingAtNPC { get; private set; }
    public bool IsHolstered => _isHolstered;

    public static PlayerGun Instance { get; private set; }

    /// <summary>
    /// Tìm chính xác khẩu súng trên Camera / Player thay vì súng nằm trong tủ trưng bày
    /// </summary>
    public static PlayerGun GetPlayerGun()
    {
        if (Instance != null) return Instance;

        // 1. Tìm trên toàn bộ GameObject có tag "Player" hoặc tên chứa "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            PlayerGun g = p.GetComponentInChildren<PlayerGun>(true);
            if (g != null)
            {
                Instance = g;
                return g;
            }
        }

        // 2. Tìm trên Camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            PlayerGun g = cam.GetComponentInChildren<PlayerGun>(true);
            if (g != null)
            {
                Instance = g;
                return g;
            }
        }

        // 3. Quét tất cả PlayerGun trong scene và ưu tiên cái nằm dưới Player hoặc Camera
        PlayerGun[] allGuns = FindObjectsOfType<PlayerGun>(true);
        foreach (var g in allGuns)
        {
            if (g.transform.root.CompareTag("Player") || 
                g.transform.root.name.ToLower().Contains("player") || 
                g.GetComponentInParent<Camera>() != null ||
                g.GetComponentInParent<PlayerController>() != null)
            {
                Instance = g;
                return g;
            }
        }

        if (allGuns.Length > 0)
        {
            Instance = allGuns[0];
            return allGuns[0];
        }

        return null;
    }

    private void Awake()
    {
        bool isPlayerGun = transform.root.CompareTag("Player") 
                        || transform.root.name.ToLower().Contains("player")
                        || GetComponentInParent<PlayerController>() != null 
                        || GetComponentInParent<Camera>() != null
                        || gameObject.name.ToLower().Contains("gun");
        if (isPlayerGun)
        {
            Instance = this;
        }

        maxAmmo = 6;     // Đảm bảo băng đạn luôn là 6 viên
        if (storedAmmo > 0)
        {
            currentAmmo = Mathf.Clamp(storedAmmo, 0, maxAmmo);
        }
        else
        {
            currentAmmo = 0;
            storedAmmo = 0;
        }

        if (transform.parent != null)
        {
            _defaultLocalPos = transform.localPosition;
            _defaultLocalRot = transform.localRotation;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;

        // Vô hiệu hóa toàn bộ Collider trên súng (tránh va quẹt với cửa/tường làm nhân vật bị đẩy ngược)
        DisableGunColliders();
    }

    /// <summary>
    /// Vô hiệu hóa hoàn toàn collider trên súng cầm tay để tránh va chạm vật lý đẩy người chơi khi đi qua cửa
    /// </summary>
    public void DisableGunColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Tắt script AutoMeshCollider nếu có
        var autoCols = GetComponentsInChildren<AutoMeshCollider>(true);
        foreach (var ac in autoCols)
        {
            ac.enabled = false;
        }

        // Chuyển sang Layer Ignore Raycast để không cản trở raycast tương tác
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer != -1)
        {
            SetLayerRecursively(gameObject, ignoreLayer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void Start()
    {
        _cam = GetComponentInParent<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (transform.parent != null && transform.localPosition.magnitude > 0.001f)
        {
            _defaultLocalPos = transform.localPosition;
            _defaultLocalRot = transform.localRotation;
        }

        // Đảm bảo collider luôn bị tắt
        DisableGunColliders();

        // Thiết lập vị trí đầu nòng súng chính xác
        SetupMuzzle();

        // Mặc định luôn ẩn súng lúc bắt đầu game nếu chưa nhận trang bị súng
        if (!hasGun && (TaskManager.instance == null || !TaskManager.instance.hasGun))
        {
            HolsterGun();
        }
        else if (startHolstered && !_hasBeenExplicitlyDrawn)
        {
            HolsterGun();
        }

        if (gunshotClip == null)
        {
            gunshotClip = Resources.Load<AudioClip>("tieng_sung_ban_1_phat");
            if (gunshotClip == null)
                gunshotClip = Resources.Load<AudioClip>("TaskSound/tieng_sung_ban_1_phat-www_tiengdong_com");
            if (gunshotClip == null)
                gunshotClip = CreateProceduralGunshot();
        }
        _audioSource.clip = gunshotClip;

        // Đảm bảo tìm Animator của súng nếu chưa được gán
        if (gunAnimator == null)
        {
            gunAnimator = GetComponent<Animator>();
            if (gunAnimator == null) gunAnimator = GetComponentInChildren<Animator>(true);
        }

        // Cập nhật giao diện số đạn
        if (PlayerHUDManager.instance != null && !_isHolstered)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }
    }

    /// <summary>
    /// Tự động tính toán vị trí đầu nòng súng chính xác dựa trên Mesh nòng súng thực tế (Cylinder.006 hoặc renderers)
    /// </summary>
    public Vector3 CalculateMuzzleLocalPosition(out Quaternion localRotation)
    {
        localRotation = Quaternion.identity;

        // Ưu tiên nòng súng của Pistol 92 (Cylinder.006)
        Transform cylinder = transform.Find("Cylinder.006");
        if (cylinder != null)
        {
            Renderer cr = cylinder.GetComponent<Renderer>();
            if (cr != null)
            {
                Bounds b = cr.bounds;
                Vector3 pMin = transform.InverseTransformPoint(b.min);
                Vector3 pMax = transform.InverseTransformPoint(b.max);
                Vector3 pCenter = transform.InverseTransformPoint(b.center);

                // Trong Pistol 92, nòng súng hướng theo trục +X cục bộ
                float maxX = Mathf.Max(pMin.x, pMax.x);
                localRotation = Quaternion.Euler(0f, 90f, 0f); // Xoay để trục Z của particle/light bắn thẳng ra từ +X
                return new Vector3(maxX + 0.02f, pCenter.y, pCenter.z);
            }
        }

        // Tự động quét toàn bộ Renderer nếu không phải Pistol 92
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            Vector3 pMin = transform.InverseTransformPoint(b.min);
            Vector3 pMax = transform.InverseTransformPoint(b.max);
            Vector3 pCenter = transform.InverseTransformPoint(b.center);

            float xSpan = Mathf.Abs(pMax.x - pMin.x);
            float zSpan = Mathf.Abs(pMax.z - pMin.z);

            if (xSpan > zSpan)
            {
                float maxX = Mathf.Max(pMin.x, pMax.x);
                localRotation = Quaternion.Euler(0f, 90f, 0f);
                return new Vector3(maxX + 0.02f, pCenter.y, pCenter.z);
            }
            else
            {
                float maxZ = Mathf.Max(pMin.z, pMax.z);
                localRotation = Quaternion.identity;
                return new Vector3(pCenter.x, pCenter.y, maxZ + 0.02f);
            }
        }

        return muzzleLocalOffset;
    }

    /// <summary>
    /// Thiết lập Muzzle Point và đèn chớp nòng.
    /// Nếu người dùng đã gán Muzzle Point (hoặc prefab đã có sẵn), GIỮ NGUYÊN 100% tọa độ và góc xoay mà người dùng thiết lập.
    /// </summary>
    public void SetupMuzzle(bool forceRecalculate = false)
    {
        // 1. Nếu người dùng đã gán Muzzle Point trong Inspector hoặc Prefab
        if (muzzlePoint != null)
        {
            if (forceRecalculate)
            {
                Quaternion targetRot = Quaternion.identity;
                Vector3 targetPos = CalculateMuzzleLocalPosition(out targetRot);
                muzzlePoint.localPosition = targetPos;
                muzzlePoint.localRotation = targetRot;
            }
            SetupMuzzleLightAndParticles();
            return;
        }

        // 2. Tìm kiếm tự động các Transform con có tên chứa "muzzle" (ví dụ: "Muzzle Point", "MuzzlePoint", "muzzle")
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("muzzle"))
            {
                muzzlePoint = child;
                if (forceRecalculate)
                {
                    Quaternion targetRot = Quaternion.identity;
                    Vector3 targetPos = CalculateMuzzleLocalPosition(out targetRot);
                    muzzlePoint.localPosition = targetPos;
                    muzzlePoint.localRotation = targetRot;
                }
                SetupMuzzleLightAndParticles();
                return;
            }
        }

        // 3. Chỉ khi hoàn toàn không có Muzzle Point nào, mới tự động tạo mới
        Quaternion defaultRot = Quaternion.identity;
        Vector3 defaultPos = CalculateMuzzleLocalPosition(out defaultRot);

        GameObject mp = new GameObject("Muzzle Point");
        mp.transform.SetParent(transform, false);
        mp.transform.localPosition = defaultPos;
        mp.transform.localRotation = defaultRot;
        muzzlePoint = mp.transform;

        SetupMuzzleLightAndParticles();
    }

    [ContextMenu("Tự động tính lại vị trí Muzzle (Ghi đè)")]
    public void ContextMenuRecalculateMuzzle()
    {
        SetupMuzzle(forceRecalculate: true);
    }

    private void SetupMuzzleLightAndParticles()
    {
        if (muzzlePoint == null) return;

        if (muzzleLight == null)
        {
            Transform existingLight = muzzlePoint.Find("MuzzleFlashLight");
            if (existingLight != null)
            {
                muzzleLight = existingLight.GetComponent<Light>();
            }
            else
            {
                GameObject lightGo = new GameObject("MuzzleFlashLight");
                lightGo.transform.SetParent(muzzlePoint, false);
                lightGo.transform.localPosition = Vector3.zero;
                lightGo.transform.localRotation = Quaternion.identity;
                muzzleLight = lightGo.AddComponent<Light>();
                muzzleLight.type = LightType.Point;
                muzzleLight.color = new Color(1f, 0.65f, 0.2f);
                muzzleLight.range = 6f;
                muzzleLight.intensity = 5f;
                muzzleLight.enabled = false;
            }
        }
        else
        {
            muzzleLight.transform.SetParent(muzzlePoint, false);
            muzzleLight.transform.localPosition = Vector3.zero;
            muzzleLight.transform.localRotation = Quaternion.identity;
        }

        // Xử lý Particle chớp nòng (WarFX hoặc particle khác)
        if (muzzleFlashParticles != null)
        {
            // Nếu là Prefab Asset chưa nằm trong Scene
            if (!muzzleFlashParticles.gameObject.scene.IsValid())
            {
                Transform existingChild = muzzlePoint.Find(muzzleFlashParticles.gameObject.name);
                if (existingChild != null)
                {
                    muzzleFlashParticles = existingChild.GetComponent<ParticleSystem>();
                }
                else
                {
                    ParticleSystem instance = Instantiate(muzzleFlashParticles, muzzlePoint);
                    instance.name = muzzleFlashParticles.gameObject.name;
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    muzzleFlashParticles = instance;
                }
            }
            else
            {
                muzzleFlashParticles.transform.SetParent(muzzlePoint, false);
                muzzleFlashParticles.transform.localPosition = Vector3.zero;
                muzzleFlashParticles.transform.localRotation = Quaternion.identity;
            }
        }
        else if (muzzlePoint != null)
        {
            muzzleFlashParticles = muzzlePoint.GetComponentInChildren<ParticleSystem>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 mPos = (muzzlePoint != null) ? muzzlePoint.position : transform.TransformPoint(muzzleLocalOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mPos, 0.015f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(mPos, (muzzlePoint != null ? muzzlePoint.forward : transform.forward) * 0.2f);
    }

    private void Update()
    {
        // Khong xu ly neu day la model sung trang tri trong Map/Tu ma khong nam tren Player
        if (Instance != null && Instance != this && GetComponentInParent<Camera>() == null && !transform.root.CompareTag("Player"))
        {
            return;
        }

        // 1. Tuyệt đối KHÔNG xử lý bắn hoặc đổi súng khi đang Pause hoặc Time.timeScale = 0
        if (Time.timeScale == 0f) return;
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused) return;
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused) return;

        // 2. Không bắn khi chuột đang click trên UI (Menu, Setting, Button)
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // Kiểm tra phím 1, H, G hoặc lăn chuột giữa để rút/cất súng
        HandleWeaponSwitchInput();

        // Khong lam gi khi da cat sung
        if (_isHolstered) return;

        IsAimingAtNPC = CheckAimingAtNPC();

        // Ho tro ca Old Input Manager va New Input System
        bool firePressed = false;
        if (Input.GetMouseButtonDown(0))
        {
            firePressed = true;
        }
        else
        {
            try
            {
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    firePressed = true;
                }
            }
            catch { }
        }

        if (firePressed && Time.time >= _nextFireTime)
        {
            if (currentAmmo <= 0)
            {
                _nextFireTime = Time.time + 0.25f; // Thời gian delay giữa các lần bóp cò rỗng
                PlayEmptyGunSound();
                return;
            }

            if (requireAimingAtNPC && !IsAimingAtNPC) return;
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Xử lý phím tắt đổi/rút/cất vũ khí (Phím 1, Phím H, Phím G hoặc Lăn chuột giữa)
    /// </summary>
    public void HandleWeaponSwitchInput()
    {
        if ((PauseMenuController.instance != null && PauseMenuController.instance.isPaused) ||
            (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused))
            return;

        // Cho phép cất súng nếu súng đang cầm trên tay (!isHolstered), hoặc rút súng nếu đã sở hữu (HasGun)
        bool canSwitch = HasGun || !_isHolstered;
        if (!canSwitch) return;

        bool togglePressed = false;

        // 1. Phím 1 (Alpha1 hoặc Keypad1)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            togglePressed = true;
        }
        // 2. Phím H (Holster - Rất trực quan để cất súng)
        else if (Input.GetKeyDown(KeyCode.H))
        {
            togglePressed = true;
        }
        // 3. Phím G (Nếu đã sở hữu súng thì nhấn G sẽ cất / rút súng)
        else if (Input.GetKeyDown(KeyCode.G))
        {
            togglePressed = true;
        }
        // 4. Lăn chuột giữa (Mouse ScrollWheel)
        else if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.05f || Input.mouseScrollDelta.y != 0f)
        {
            togglePressed = true;
        }
        // 5. Hỗ trợ New Input System
        else
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame ||
                        kb.hKey.wasPressedThisFrame || kb.gKey.wasPressedThisFrame)
                    {
                        togglePressed = true;
                    }
                }

                Mouse m = Mouse.current;
                if (m != null && Mathf.Abs(m.scroll.ReadValue().y) > 0.1f)
                    togglePressed = true;
            }
            catch { }
        }

        if (togglePressed)
        {
            ToggleGun();
        }
    }

    /// <summary>
    /// Chuyển đổi qua lại giữa Rút súng và Cất súng
    /// </summary>
    public void ToggleGun()
    {
        if (Time.time < _nextToggleTime) return;

        _nextToggleTime = Time.time + 0.3f;

        if (_isHolstered)
        {
            DrawGun();
        }
        else
        {
            HolsterGun();
        }
    }

    private void PlayEmptyGunSound()
    {
        if (emptyGunClip == null)
        {
            emptyGunClip = Resources.Load<AudioClip>("empty_gun");
            if (emptyGunClip == null)
                emptyGunClip = CreateProceduralDryFire();
        }

        if (_audioSource != null && emptyGunClip != null)
        {
            _audioSource.PlayOneShot(emptyGunClip);
        }

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }
        Debug.Log("[PlayerGun] HẾT ĐẠN! Cần chuyển màn mới hoặc nhặt hộp đạn để nạp lại đầy 6 viên đạn.");
    }

    private AudioClip CreateProceduralDryFire()
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * 0.08f);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 80f) * 0.6f;
        }
        AudioClip clip = AudioClip.Create("DryFire", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private bool CheckAimingAtNPC()
    {
        if (_cam == null) return false;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            if (debugAiming)
                Debug.Log($"[PlayerGun] Raycast trung: '{hit.collider.gameObject.name}' | Tag: '{hit.collider.tag}'");

            Transform t = hit.collider.transform;
            while (t != null)
            {
                if (t.CompareTag(npcTag) || t.GetComponent<IDamageable>() != null || t.GetComponent<NPCHealth>() != null) 
                    return true;
                t = t.parent;
            }
        }
        return false;
    }

    private void Shoot()
    {
        if (Time.timeScale == 0f) return;
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused) return;

        // Trừ đạn
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        storedAmmo = currentAmmo;

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }

        // 1. Tieng sung & Animation giật súng
        if (_audioSource != null && gunshotClip != null)
            _audioSource.PlayOneShot(gunshotClip);

        if (useProceduralRecoil)
        {
            if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
            _recoilCoroutine = StartCoroutine(RecoilRoutine());
        }
        else
        {
            if (gunAnimator == null)
            {
                gunAnimator = GetComponent<Animator>();
                if (gunAnimator == null) gunAnimator = GetComponentInChildren<Animator>(true);
            }

            if (gunAnimator != null && !string.IsNullOrEmpty(shootAnimTrigger) && HasParameter(gunAnimator, shootAnimTrigger))
            {
                gunAnimator.ResetTrigger(shootAnimTrigger);
                gunAnimator.SetTrigger(shootAnimTrigger);
            }
        }

        // 2. Muzzle flash
        if (muzzleLight != null)
        {
            if (_muzzleFlashCoroutine != null) StopCoroutine(_muzzleFlashCoroutine);
            _muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
        }
        // Kích hoạt Prefab hiệu ứng lửa nòng súng (nếu người chơi gán Prefab)
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flashInstance = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            ParticleSystem[] pss = flashInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                ps.Play();
            }
            float destroyDelay = 0.5f;
            if (pss.Length > 0 && pss[0].main.duration > 0)
            {
                destroyDelay = Mathf.Max(0.2f, pss[0].main.duration);
            }
            Destroy(flashInstance, destroyDelay);
        }
        else if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.Play();
        }

        // 3. Chop man hinh
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.TriggerFlash(flashIntensity);

        // 4. U tai
        if (TinnitusEffect.Instance != null)
            TinnitusEffect.Instance.TriggerTinnitus(tinnitusIntensity);

        // 5. Raycast gay sat thuong
        if (_cam != null)
        {
            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
            {
                Debug.Log($"[Sung] Ban trung: {hit.collider.name}");
                Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

                IDamageable dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null) dmg.TakeDamage(1);
            }
        }

        // 6. Cat sung sau khi ban
        if (holsterAfterShoot)
            StartCoroutine(HolsterRoutine());
    }

    /// <summary>
    /// Doi holsterDelay giay roi an model sung di.
    /// </summary>
    private IEnumerator HolsterRoutine()
    {
        // Cho hieu ung chay xong
        yield return new WaitForSeconds(holsterDelay);

        // Neu co Animator thi chay animation cat sung truoc
        if (gunAnimator != null && !string.IsNullOrEmpty(holsterAnimTrigger) && HasParameter(gunAnimator, holsterAnimTrigger))
        {
            gunAnimator.SetTrigger(holsterAnimTrigger);
            // Cho animation chay (uoc tinh 0.5 giay)
            yield return new WaitForSeconds(0.5f);
        }

        HolsterGun();
    }

    /// <summary>
    /// Cất súng vào (giấu mô hình và cập nhật HUD).
    /// </summary>
    public void HolsterGun()
    {
        _isHolstered = true;

        // 1. Tắt toàn bộ Renderers
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            r.enabled = false;
        }

        // 2. Tắt toàn bộ GameObject con chứa Mesh súng (Cube.000, Cube.004, Cylinder.006, v.v.)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (muzzlePoint == null || child != muzzlePoint)
            {
                child.gameObject.SetActive(false);
            }
        }

        if (muzzleLight != null) muzzleLight.enabled = false;

        if (gunModelObject != null && gunModelObject != gameObject)
        {
            gunModelObject.SetActive(false);
        }

        if (gunAnimator != null)
        {
            gunAnimator.enabled = false;
        }

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.OnGunHolstered();
        }

        Debug.Log("[PlayerGun] ĐÃ CẤT SÚNG (Holstered)! Nhấn [1], [H] hoặc [G] để rút súng lại.");
    }

    /// <summary>
    /// Rút súng ra cầm trên tay.
    /// </summary>
    public void DrawGun()
    {
        hasGun = true;
        _hasBeenExplicitlyDrawn = true;
        _isHolstered = false;

        if (_cam == null) _cam = GetComponentInParent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Bật GameObject gốc và model súng
        gameObject.SetActive(true);
        if (gunModelObject != null) gunModelObject.SetActive(true);
        if (transform.parent != null) transform.parent.gameObject.SetActive(true);

        // Bật lại toàn bộ GameObject con chứa Mesh súng
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }

        // Đảm bảo tọa độ FPS luôn đúng nếu chưa được đặt
        if (transform.parent != null && transform.localPosition.magnitude < 0.01f)
        {
            transform.localPosition = new Vector3(0.16f, -0.12f, 0.32f);
            transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            transform.localScale = Vector3.one * 0.28f;
        }

        if (transform.parent != null)
        {
            _defaultLocalPos = transform.localPosition;
            _defaultLocalRot = transform.localRotation;
        }

        // Bật tất cả Renderer con của súng
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            r.enabled = true;
            r.gameObject.SetActive(true);
        }

        if (gunAnimator != null)
        {
            gunAnimator.enabled = true;
            if (!string.IsNullOrEmpty(drawAnimTrigger) && HasParameter(gunAnimator, drawAnimTrigger))
                gunAnimator.SetTrigger(drawAnimTrigger);
        }

        DisableGunColliders();

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }

        Debug.Log("[PlayerGun] ĐÃ RÚT SÚNG (Drawn)! Nhấn [1], [H] hoặc [G] để cất súng.");
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
    }

    private IEnumerator RecoilRoutine()
    {
        // Giat súng: Đẩy lùi về sau (theo trục Z của camera hoặc -X của súng tùy setup)
        // và hất nòng lên một chút
        float elapsed = 0f;
        float kickTime = recoilDuration * 0.25f;
        float recoverTime = recoilDuration * 0.75f;

        Vector3 kickPos = _defaultLocalPos - new Vector3(0f, -0.005f, recoilKickBack);
        Quaternion kickRot = _defaultLocalRot * Quaternion.Euler(0f, 0f, recoilKickUp);

        // Giai đoạn 1: Giật nảy tức thì
        while (elapsed < kickTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / kickTime;
            transform.localPosition = Vector3.Lerp(_defaultLocalPos, kickPos, t);
            transform.localRotation = Quaternion.Slerp(_defaultLocalRot, kickRot, t);
            yield return null;
        }

        // Giai đoạn 2: Hồi phục mượt mà về vị trí ban đầu
        elapsed = 0f;
        while (elapsed < recoverTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverTime;
            // Dùng SmoothStep để về vị trí cực kỳ êm ái
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(kickPos, _defaultLocalPos, smoothT);
            transform.localRotation = Quaternion.Slerp(kickRot, _defaultLocalRot, smoothT);
            yield return null;
        }

        transform.localPosition = _defaultLocalPos;
        transform.localRotation = _defaultLocalRot;
        _recoilCoroutine = null;
    }

    private AudioClip CreateProceduralGunshot()
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * 0.4f);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = (Random.Range(-1f, 1f) * 0.45f + Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.55f) * Mathf.Exp(-t * 18f);
        }
        AudioClip clip = AudioClip.Create("ProceduralGunshot", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Nạp thêm đạn khi nhặt được hộp đạn
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
        storedAmmo = currentAmmo;

        if (PlayerHUDManager.instance != null && !_isHolstered && HasGun)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }
        Debug.Log($"[PlayerGun] Đã nạp thêm đạn: {currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// Đặt trực tiếp số đạn hiện tại
    /// </summary>
    public void SetAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(amount, 0, maxAmmo);
        storedAmmo = currentAmmo;

        if (PlayerHUDManager.instance != null && !_isHolstered && HasGun)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }
        Debug.Log($"[PlayerGun] Thiết lập số đạn: {currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// Nạp đầy đạn (6 viên) khi qua màn mới hoặc bắt đầu ngày mới
    /// </summary>
    public void ReplenishAmmo()
    {
        currentAmmo = maxAmmo;
        storedAmmo = maxAmmo;

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.UpdateAmmoUI(currentAmmo, maxAmmo);
        }
        Debug.Log($"[PlayerGun] Đã hồi đầy {maxAmmo} viên đạn cho màn mới!");
    }
}




