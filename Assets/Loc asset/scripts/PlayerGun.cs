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
    [Tooltip("Particle tia lua dau nong neu co (tuy chon)")]
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

    [Tooltip("(Tuy chon) Animator cua sung neu co animation cất súng. De trong neu khong dung.")]
    [SerializeField] private Animator gunAnimator;

    [Tooltip("Ten Trigger trong Animator de chay animation cat sung")]
    [SerializeField] private string holsterAnimTrigger = "Holster";

    [Tooltip("(Tuy chon) GameObject rieng lam model sung. De trong = tu dung chinh gameObject nay.")]
    [SerializeField] private GameObject gunModelObject;

    [Header("=== Cau Hinh NPC ===")]
    [SerializeField] private string npcTag = "NPC";
    [SerializeField] private bool debugAiming = false;

    [Tooltip("Tick = sung dang cat trong tu luc bat dau game. Khong tick = cam sung ngay tu dau.")]
    [SerializeField] private bool startHolstered = true;

    // ── Noi bo ──
    private AudioSource _audioSource;
    private float _nextFireTime = 0f;
    private Coroutine _muzzleFlashCoroutine;
    private Camera _cam;
    private bool _isHolstered = false;
    private bool _hasBeenExplicitlyDrawn = false;

    public bool IsAimingAtNPC { get; private set; }
    public bool IsHolstered => _isHolstered;

    private void Awake()
    {
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

        // Đảm bảo collider luôn bị tắt
        DisableGunColliders();

        // Thiết lập vị trí đầu nòng súng chính xác
        SetupMuzzle();

        // Chỉ ẩn súng lúc bắt đầu nếu người chơi chưa nhặt hoặc chưa gọi DrawGun()
        if (startHolstered && !_hasBeenExplicitlyDrawn)
        {
            _isHolstered = true;
            GameObject t = gunModelObject != null ? gunModelObject : gameObject;
            t.SetActive(false);
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
    /// Tự động thiết lập Muzzle Point và đèn chớp nòng tại đúng miệng nòng súng
    /// </summary>
    [ContextMenu("Căn lại Muzzle vào đầu nòng súng")]
    public void SetupMuzzle()
    {
        Quaternion targetRot = Quaternion.identity;
        Vector3 targetPos = CalculateMuzzleLocalPosition(out targetRot);

        if (muzzlePoint == null)
        {
            Transform found = transform.Find("MuzzlePoint");
            if (found != null)
            {
                muzzlePoint = found;
            }
            else
            {
                GameObject mp = new GameObject("MuzzlePoint");
                mp.transform.SetParent(transform, false);
                muzzlePoint = mp.transform;
            }
        }

        muzzlePoint.localPosition = targetPos;
        muzzlePoint.localRotation = targetRot;

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
            if (requireAimingAtNPC && !IsAimingAtNPC) return;
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
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
        // 1. Tieng sung
        if (_audioSource != null && gunshotClip != null)
            _audioSource.PlayOneShot(gunshotClip);

        // 2. Muzzle flash
        if (muzzleLight != null)
        {
            if (_muzzleFlashCoroutine != null) StopCoroutine(_muzzleFlashCoroutine);
            _muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
        }
        if (muzzleFlashParticles != null)
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
        _isHolstered = true;

        // Cho hieu ung chay xong
        yield return new WaitForSeconds(holsterDelay);

        // Neu co Animator thi chay animation cat sung truoc
        if (gunAnimator != null && !string.IsNullOrEmpty(holsterAnimTrigger))
        {
            gunAnimator.SetTrigger(holsterAnimTrigger);
            // Cho animation chay (uoc tinh 0.5 giay)
            yield return new WaitForSeconds(0.5f);
        }

        // An model sung di
        GameObject target = gunModelObject != null ? gunModelObject : gameObject;
        target.SetActive(false);

        Debug.Log("[PlayerGun] Da cat sung.");
    }

    /// <summary>
    /// Lay sung ra lai tu code hoac UnityEvent.
    /// Vi du: GameManager goi DrawGun() khi vao tinh huong moi.
    /// </summary>
    /// <summary>
    /// Cat sung vao (goi tu GunCabinet hoac GameManager).
    /// </summary>
    public void HolsterGun()
    {
        _isHolstered = true;
        _hasBeenExplicitlyDrawn = false;
        GameObject target = gunModelObject != null ? gunModelObject : gameObject;
        target.SetActive(false);
        Debug.Log("[PlayerGun] Da cat sung.");
    }

    public void DrawGun()
    {
        _hasBeenExplicitlyDrawn = true;
        _isHolstered = false;

        if (_cam == null) _cam = GetComponentInParent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Bật GameObject gốc và model súng
        gameObject.SetActive(true);
        if (gunModelObject != null) gunModelObject.SetActive(true);
        if (transform.parent != null) transform.parent.gameObject.SetActive(true);

        // Đảm bảo tọa độ FPS luôn đúng nếu chưa được đặt
        if (transform.parent != null && transform.localPosition.magnitude < 0.01f)
        {
            transform.localPosition = new Vector3(0.16f, -0.12f, 0.32f);
            transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            transform.localScale = Vector3.one * 0.28f;
        }

        // Bật tất cả Renderer con của súng
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            r.enabled = true;
            r.gameObject.SetActive(true);
        }

        DisableGunColliders();

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Draw");

        Debug.Log("[PlayerGun] Da rut sung va hien thi model thanh cong!");
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleLight.enabled = false;
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
}




