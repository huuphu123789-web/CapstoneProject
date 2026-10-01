using System.Collections;
using UnityEngine;

/// <summary>
/// Script tạo hiệu ứng đèn chớp tắt / chập chờn kinh dị (Horror Light Flicker).
/// - Gắn trực tiếp vào Point Light, Spot Light hoặc bất kỳ nguồn sáng nào.
/// - Nhiều chế độ: Chớp giật điện kinh dị (HorrorBrokenLight), Chập chờn liên tục (ContinuousJitter), hoặc Chớp tắt chu kỳ (PeriodicBlink).
/// - Tự động nhận diện cường độ sáng của đèn (Intensity).
/// - Hỗ trợ âm thanh chập điện tùy chọn (AudioClip).
/// </summary>
public class LightFlicker : MonoBehaviour
{
    public enum FlickerMode
    {
        HorrorBrokenLight, // Kiểu bóng đèn hỏng: sáng bình thường, thỉnh thoảng giật chớp tắt loạn xạ và tắt ngúm chốc lát
        ContinuousJitter,  // Chập chờn rung rinh liên tục không ngừng
        PeriodicBlink      // Chớp tắt theo chu kỳ đều đặn
    }

    [Header("=== CHẾ ĐỘ CHỚP TẮT ===")]
    public FlickerMode mode = FlickerMode.HorrorBrokenLight;

    [Header("=== CƯỜNG ĐỘ SÁNG (INTENSITY) ===")]
    [Tooltip("Cường độ sáng tối thiểu khi đèn tắt / chớp")]
    public float minIntensity = 0f;

    [Tooltip("Cường độ sáng tối đa khi đèn sáng (Nếu để 0 sẽ tự lấy giá trị của Light)")]
    public float maxIntensity = 0f;

    [Header("=== CẤU HÌNH KINH DỊ (HORROR BROKEN LIGHT) ===")]
    [Tooltip("Thời gian chờ tối thiểu giữa các đợt chớp giật (giây)")]
    public float minSteadyTime = 1.0f;

    [Tooltip("Thời gian chờ tối đa giữa các đợt chớp giật (giây)")]
    public float maxSteadyTime = 3.5f;

    [Tooltip("Số lần chớp giật trong một đợt (tối thiểu)")]
    public int minFlickerBurst = 2;

    [Tooltip("Số lần chớp giật trong một đợt (tối đa)")]
    public int maxFlickerBurst = 6;

    [Tooltip("Tốc độ mỗi nhịp chớp giật (giây)")]
    public float burstSpeed = 0.05f;

    [Tooltip("Tỉ lệ xảy ra sự cố tắt hẳn bóng đèn trong giây lát (0 đến 1)")]
    [Range(0f, 1f)]
    public float blackoutChance = 0.4f;

    [Tooltip("Thời gian tắt hẳn bóng đèn (giây)")]
    public float blackoutDuration = 0.35f;

    [Header("=== CẤU HÌNH CHỚP LIÊN TỤC (CONTINUOUS JITTER) ===")]
    public float jitterSpeed = 20f;
    [Range(0f, 1f)]
    public float jitterSmoothing = 0.5f;

    [Header("=== ÂM THANH CHẬP ĐIỆN (TÙY CHỌN) ===")]
    public AudioClip flickerSound;
    public AudioSource audioSource;

    private Light targetLight;
    private float defaultIntensity;
    private Coroutine flickerCoroutine;

    void Awake()
    {
        targetLight = GetComponent<Light>();
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight != null)
        {
            defaultIntensity = (maxIntensity > 0f) ? maxIntensity : targetLight.intensity;
            if (maxIntensity <= 0f) maxIntensity = defaultIntensity;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        if (targetLight == null) return;
        targetLight.enabled = true;
        targetLight.intensity = maxIntensity;

        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);

        if (mode == FlickerMode.HorrorBrokenLight)
        {
            flickerCoroutine = StartCoroutine(HorrorFlickerRoutine());
        }
        else if (mode == FlickerMode.PeriodicBlink)
        {
            flickerCoroutine = StartCoroutine(PeriodicBlinkRoutine());
        }
    }

    void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (targetLight != null)
        {
            targetLight.enabled = true;
            targetLight.intensity = maxIntensity;
        }
    }

    void Update()
    {
        if (mode == FlickerMode.ContinuousJitter && targetLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * jitterSpeed, 0.0f);
            float targetInt = Mathf.Lerp(minIntensity, maxIntensity, noise);
            targetLight.intensity = Mathf.Lerp(targetLight.intensity, targetInt, 1.0f - jitterSmoothing);
            targetLight.enabled = (targetLight.intensity > 0.05f);
        }
    }

    private IEnumerator HorrorFlickerRoutine()
    {
        while (true)
        {
            // 1. Giai đoạn sáng ổn định
            targetLight.enabled = true;
            targetLight.intensity = maxIntensity;
            float steadyTime = Random.Range(minSteadyTime, maxSteadyTime);
            yield return new WaitForSeconds(steadyTime);

            // 2. Giai đoạn chớp giật liên thanh (Burst)
            int burstCount = Random.Range(minFlickerBurst, maxFlickerBurst + 1);
            PlayFlickerSound();

            for (int i = 0; i < burstCount; i++)
            {
                // Hạ độ sáng hoặc tắt
                targetLight.intensity = Random.Range(minIntensity, maxIntensity * 0.25f);
                targetLight.enabled = (targetLight.intensity > 0.05f);
                yield return new WaitForSeconds(Random.Range(burstSpeed * 0.5f, burstSpeed * 1.5f));

                // Bật sáng lại bất ngờ
                targetLight.enabled = true;
                targetLight.intensity = Random.Range(maxIntensity * 0.6f, maxIntensity);
                yield return new WaitForSeconds(Random.Range(burstSpeed * 0.5f, burstSpeed * 1.5f));
            }

            // 3. Cơ hội tắt lịm trong bóng tối (Blackout)
            if (Random.value < blackoutChance)
            {
                targetLight.enabled = false;
                targetLight.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(blackoutDuration * 0.5f, blackoutDuration * 1.5f));

                // Nháy nhẹ một cái trước khi sáng trở lại
                targetLight.enabled = true;
                targetLight.intensity = maxIntensity * 0.4f;
                yield return new WaitForSeconds(0.04f);
                targetLight.enabled = false;
                yield return new WaitForSeconds(0.06f);
            }

            // Hồi phục lại trạng thái sáng hoàn toàn
            targetLight.enabled = true;
            targetLight.intensity = maxIntensity;
        }
    }

    private IEnumerator PeriodicBlinkRoutine()
    {
        while (true)
        {
            targetLight.enabled = true;
            targetLight.intensity = maxIntensity;
            yield return new WaitForSeconds(0.5f);

            targetLight.enabled = false;
            targetLight.intensity = minIntensity;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void PlayFlickerSound()
    {
        if (flickerSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(flickerSound);
        }
    }
}
