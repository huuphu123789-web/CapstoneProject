using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý từng đèn chiếu sáng hàng rào (Fence Light / Perimeter Floodlight).
/// Hỗ trợ xoay chỉnh hướng rọi tự do trong Scene, ngắm mục tiêu (Look At Target),
/// bật/tắt, hiệu ứng chớp nháy khi máy phát điện khởi động, và đổi vật liệu phát sáng.
/// </summary>
[ExecuteAlways]
public class FenceLightController : MonoBehaviour
{
    [Header("=== CẤU HÌNH ĐÈN CHIẾU SÁNG ===")]
    [Tooltip("Component Light (Spotlight / Point Light) chiếu sáng")]
    public Light spotLight;

    [Tooltip("Cường độ ánh sáng khi máy phát điện bật")]
    public float targetIntensity = 13f;

    [Tooltip("Khoảng cách chiếu sáng (Range) của đèn")]
    public float targetRange = 16f;

    [Tooltip("Góc mở của chùm tia sáng (Spot Angle)")]
    [Range(10f, 160f)]
    public float spotAngle = 70f;

    [Tooltip("Màu ánh sáng (vàng ấm công nghiệp hoặc trắng sương mù)")]
    public Color lightColor = new Color(1f, 0.92f, 0.75f, 1f);

    [Header("=== CHỈNH HƯỚNG CHIẾU SÁNG (AIMING) ===")]
    [Tooltip("Kéo một Transform (ví dụ: cọc rào, mặt đất, mục tiêu) vào đây nếu muốn đèn tự động chiếu thẳng vào điểm đó")]
    public Transform lookAtTarget;

    [Header("=== VẬT LIỆU BÓNG ĐÈN (TÙY CHỌN) ===")]
    [Tooltip("MeshRenderer của bóng đèn để đổi vật liệu phát sáng Emission (tùy chọn)")]
    public MeshRenderer lampRenderer;

    [Tooltip("Vật liệu khi bật đèn phát sáng")]
    public Material lightOnMaterial;

    [Tooltip("Vật liệu khi tắt đèn")]
    public Material lightOffMaterial;

    [Header("=== HIỆU ỨNG KHỞI ĐỘNG (POWER ON FLICKER) ===")]
    [Tooltip("Số lần chớp nháy khi máy phát điện cấp điện")]
    public int flickerCount = 3;

    [Tooltip("Âm thanh bật đèn / tiếng rè điện")]
    public AudioClip turnOnSound;
    public AudioSource audioSource;

    private bool isPoweredOn = false;

    private void Awake()
    {
        SetupComponents();

        if (Application.isPlaying)
        {
            // Trong game lúc mới vào: Tắt đèn để chờ máy phát điện bật
            SetLightState(false);
        }
        else
        {
            // Trong Editor: Cập nhật thông số để dễ dàng căn chỉnh hướng và độ sáng
            UpdateLightProperties();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            SetLightState(false);
        }
    }

    private void Update()
    {
        // Tự động xoay đèn hướng về mục tiêu nếu có gán Target
        if (lookAtTarget != null)
        {
            Transform lightTransform = (spotLight != null) ? spotLight.transform : transform;
            lightTransform.LookAt(lookAtTarget.position);
        }
    }

    private void OnValidate()
    {
        SetupComponents();
        UpdateLightProperties();
    }

    private void SetupComponents()
    {
        if (spotLight == null)
        {
            // Tìm Light trên chính GameObject này hoặc con của nó
            spotLight = GetComponent<Light>();
            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>();
            }

            // Nếu chưa có bất kỳ Light nào, tạo mới Spotlight gắn liền
            if (spotLight == null)
            {
                spotLight = gameObject.AddComponent<Light>();
                spotLight.type = LightType.Spot;
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Đồng bộ các thông số từ Inspector vào Component Light
    /// </summary>
    public void UpdateLightProperties()
    {
        if (spotLight != null)
        {
            spotLight.range = targetRange;
            spotLight.spotAngle = spotAngle;
            spotLight.color = lightColor;

            if (!Application.isPlaying)
            {
                spotLight.intensity = targetIntensity;
            }
        }
    }

    /// <summary>
    /// Bật nguồn điện chiếu sáng (có hiệu ứng chớp nháy)
    /// </summary>
    public void TurnOn(bool withFlicker = true)
    {
        if (isPoweredOn) return;
        isPoweredOn = true;

        if (withFlicker && gameObject.activeInHierarchy && Application.isPlaying)
        {
            StartCoroutine(PowerOnRoutine());
        }
        else
        {
            SetLightState(true);
        }
    }

    /// <summary>
    /// Tắt nguồn điện
    /// </summary>
    public void TurnOff()
    {
        isPoweredOn = false;
        StopAllCoroutines();
        SetLightState(false);
    }

    private IEnumerator PowerOnRoutine()
    {
        if (turnOnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(turnOnSound);
        }

        // Chớp nháy vài lần như đèn cao áp thật khi máy phát vừa nổ
        for (int i = 0; i < flickerCount; i++)
        {
            SetLightState(true, targetIntensity * Random.Range(0.25f, 0.65f));
            yield return new WaitForSeconds(Random.Range(0.06f, 0.12f));

            SetLightState(false);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
        }

        // Sáng hẳn 100% công suất
        SetLightState(true, targetIntensity);
    }

    public void SetLightState(bool state, float intensity = -1f)
    {
        if (spotLight != null)
        {
            spotLight.enabled = state;
            spotLight.intensity = (intensity >= 0f) ? intensity : targetIntensity;
        }

        if (lampRenderer != null)
        {
            if (state && lightOnMaterial != null)
            {
                lampRenderer.material = lightOnMaterial;
            }
            else if (!state && lightOffMaterial != null)
            {
                lampRenderer.material = lightOffMaterial;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isPoweredOn ? Color.yellow : new Color(1f, 0.9f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        Light l = (spotLight != null) ? spotLight : GetComponentInChildren<Light>();
        if (l != null && l.type == LightType.Spot)
        {
            Vector3 origin = l.transform.position;
            Vector3 forward = l.transform.forward;

            Gizmos.color = isPoweredOn ? Color.yellow : new Color(1f, 0.85f, 0.3f, 0.7f);
            Gizmos.DrawRay(origin, forward * l.range);

            // Vẽ vòng tròn đáy nón ánh sáng để dễ căn chỉnh trong Scene
            float rad = Mathf.Deg2Rad * (l.spotAngle * 0.5f);
            float radius = Mathf.Tan(rad) * l.range;
            Vector3 center = origin + forward * l.range;

            Gizmos.DrawWireSphere(center, radius * 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Light l = (spotLight != null) ? spotLight : GetComponentInChildren<Light>();
        if (l != null && l.type == LightType.Spot)
        {
            Vector3 origin = l.transform.position;
            Vector3 forward = l.transform.forward;
            Vector3 up = l.transform.up;
            Vector3 right = l.transform.right;

            float rad = Mathf.Deg2Rad * (l.spotAngle * 0.5f);
            float radius = Mathf.Tan(rad) * l.range;
            Vector3 center = origin + forward * l.range;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, center + up * radius);
            Gizmos.DrawLine(origin, center - up * radius);
            Gizmos.DrawLine(origin, center + right * radius);
            Gizmos.DrawLine(origin, center - right * radius);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
