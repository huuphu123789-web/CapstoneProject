using UnityEngine;

/// <summary>
/// Tạo hiệu ứng ù tai (Tinnitus) và điếc tạm thời sau một tiếng nổ lớn (súng bắn sát tai).
/// Script này sử dụng AudioLowPassFilter để bóp méo âm thanh môi trường
/// và tự phát ra âm thanh vo ve tần số cao bằng cách tạo sóng sine trực tiếp trong code.
/// Gắn script này vào GameObject có AudioListener (thường là Main Camera).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TinnitusEffect : MonoBehaviour
{
    public static TinnitusEffect Instance { get; private set; }

    [Header("=== Cấu Hình Thính Lực ===")]
    [Tooltip("Thời gian hiệu lực của hiệu ứng ù tai (giây)")]
    [SerializeField] private float effectDuration = 4.0f;
    
    [Tooltip("Tần số âm thanh bị lọc thấp nhất (Hz) khi ù tai cực đại. Âm thanh môi trường sẽ rất đục.")]
    [SerializeField] private float lowPassMinFreq = 400f;
    
    [Tooltip("Tần số âm thanh bình thường khi không ù tai.")]
    [SerializeField] private float lowPassMaxFreq = 22000f;

    [Header("=== Cấu Hình Sóng Âm Ù Tai ===")]
    [Tooltip("Tần số tiếng vo ve ù tai (Hz). Khoảng 3000Hz - 4000Hz là tiếng eeee cao nhức tai.")]
    [SerializeField] private float tinnitusFrequency = 3500f;

    [Tooltip("Âm lượng tối đa của tiếng ù tai. Lưu ý: Tiếng eeee cao tần rất chói nên đặt âm lượng nhỏ (0.1 - 0.15 là vừa đủ).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float maxTinnitusVolume = 0.12f;

    // ── Thành phần điều khiển ──
    private AudioSource _audioSource;
    private AudioLowPassFilter _lowPassFilter;
    
    private float _tinnitusVolume = 0f;
    private float _timer = 0f;
    private double _phase = 0.0;
    private double _sampleRate = 48000.0;
    private bool _isEffectActive = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Lấy hoặc tự thêm AudioSource
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f; // Âm thanh 2D chạy trực tiếp vào tai

        // Lấy hoặc tự thêm AudioLowPassFilter
        _lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (_lowPassFilter == null)
        {
            _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        }
        
        // Mặc định tắt filter đi để không tốn CPU xử lý khi bình thường
        _lowPassFilter.enabled = false;
        _lowPassFilter.cutoffFrequency = lowPassMaxFreq;

        int sr = UnityEngine.AudioSettings.outputSampleRate;
        _sampleRate = (sr > 0) ? sr : 48000.0;
    }

    private void Update()
    {
        if (!_isEffectActive) return;

        _timer += Time.deltaTime;
        float progress = _timer / effectDuration;

        if (progress >= 1f)
        {
            // Kết thúc hiệu ứng
            StopTinnitusEffect();
        }
        else
        {
            // 1. Phục hồi thính lực (Mở rộng dần cutoff frequency từ Min về Max)
            // Dùng Lerp mượt (ví dụ: bình phương tiến trình để phục hồi nhanh hơn ở giai đoạn sau)
            float t = progress * progress; 
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassMinFreq, lowPassMaxFreq, t);

            // 2. Giảm dần âm lượng tiếng ù tai
            _tinnitusVolume = Mathf.Lerp(maxTinnitusVolume, 0f, progress);
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng ù tai với cường độ chỉ định (ví dụ súng to thì ù nhiều).
    /// </summary>
    /// <param name="intensity">Độ mạnh hiệu ứng (0 đến 1)</param>
    public void TriggerTinnitus(float intensity = 1.0f)
    {
        intensity = Mathf.Clamp01(intensity);
        
        _timer = 0f;
        _tinnitusVolume = maxTinnitusVolume * intensity;
        _isEffectActive = true;

        // Kích hoạt filter và hạ thấp tần số nhận biết âm thanh
        if (_lowPassFilter != null)
        {
            _lowPassFilter.enabled = true;
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassMaxFreq, lowPassMinFreq, intensity);
        }

        // Bắt đầu phát âm thanh (OnAudioFilterRead sẽ can thiệp để sinh sóng sine)
        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }

    /// <summary>
    /// Kết thúc hiệu ứng và khôi phục trạng thái âm thanh bình thường.
    /// </summary>
    private void StopTinnitusEffect()
    {
        _isEffectActive = false;
        _tinnitusVolume = 0f;
        _timer = 0f;

        if (_lowPassFilter != null)
        {
            _lowPassFilter.cutoffFrequency = lowPassMaxFreq;
            _lowPassFilter.enabled = false;
        }

        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    /// <summary>
    /// Can thiệp vào luồng âm thanh để sinh sóng sine tần số cao trực tiếp bằng thuật toán.
    /// </summary>
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_isEffectActive || _tinnitusVolume <= 0f) return;

        double increment = tinnitusFrequency * 2.0 * System.Math.PI / _sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            _phase += increment;
            if (_phase > 2.0 * System.Math.PI)
            {
                _phase -= 2.0 * System.Math.PI;
            }

            // Tạo giá trị sóng sine
            float sampleValue = (float)System.Math.Sin(_phase) * _tinnitusVolume;

            for (int c = 0; c < channels; c++)
            {
                // Trộn (Mix) tiếng ù tai vào các âm thanh hiện có trong game
                data[i + c] += sampleValue;
            }
        }
    }
}
