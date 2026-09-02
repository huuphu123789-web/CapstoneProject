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

    [Header("=== Hieu Ung Sung ===")]
    [SerializeField] private Light muzzleLight;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    [Header("=== Am Thanh Sung ===")]
    [SerializeField] private AudioClip gunshotClip;

    [Header("=== Cuong Do Hieu Ung ===")]
    [Range(0f, 1f)] [SerializeField] private float flashIntensity = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float tinnitusIntensity = 0.7f;

    [Header("=== Cat Sung Sau Khi Ban ===")]
    [Tooltip("Tu dong cat sung sau khi ban trung NPC")]
    [SerializeField] private bool holsterAfterShoot = true;

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

    public bool IsAimingAtNPC { get; private set; }
    public bool IsHolstered => _isHolstered;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        _cam = GetComponentInParent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Ap dung trang thai cat/rut sung luc bat dau
        if (startHolstered)
        {
            _isHolstered = true;
            GameObject t = gunModelObject != null ? gunModelObject : gameObject;
            t.SetActive(false);
        }

        if (gunshotClip == null)
            gunshotClip = CreateProceduralGunshot();
        _audioSource.clip = gunshotClip;

        if (muzzleLight == null)
        {
            GameObject lightGo = new GameObject("MuzzleFlashLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
            muzzleLight = lightGo.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(1f, 0.6f, 0.2f);
            muzzleLight.range = 7f;
            muzzleLight.intensity = 5f;
            muzzleLight.enabled = false;
        }
    }

    private void Update()
    {
        // Khong lam gi khi da cat sung
        if (_isHolstered) return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            IsAimingAtNPC = false;
            return;
        }

        IsAimingAtNPC = CheckAimingAtNPC();

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && Time.time >= _nextFireTime)
        {
            if (!IsAimingAtNPC) return;
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
                if (t.CompareTag(npcTag)) return true;
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
        GameObject target = gunModelObject != null ? gunModelObject : gameObject;
        target.SetActive(false);
        Debug.Log("[PlayerGun] Da cat sung.");
    }

    public void DrawGun()
    {
        _isHolstered = false;
        GameObject target = gunModelObject != null ? gunModelObject : gameObject;
        target.SetActive(true);

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Draw");

        Debug.Log("[PlayerGun] Da rut sung.");
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




