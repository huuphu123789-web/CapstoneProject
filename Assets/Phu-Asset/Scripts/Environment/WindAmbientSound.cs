using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống âm thanh tiếng gió môi trường (Ambient Wind Sound).
/// Hỗ trợ phát gió nền lặp liên tục, hiệu ứng gió thở (volume breathing) tự nhiên
/// và các cơn gió rít (wind gusts) ngẫu nhiên để tạo bầu không khí ma mị, rùng rợn.
/// </summary>
[DisallowMultipleComponent]
public class WindAmbientSound : MonoBehaviour
{
    public static WindAmbientSound Instance { get; private set; }

    [Header("=== WIND LOOP (TIẾNG GIÓ NỀN) ===")]
    [Tooltip("Clip âm thanh tiếng gió lặp liên tục")]
    public AudioClip windLoopClip;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng cơ bản của gió")]
    public float baseVolume = 0.45f;

    [Tooltip("Tự động phát ngay khi scene khởi chạy")]
    public bool playOnStart = true;

    [Header("=== NATURAL WIND FLUCTUATION (GIÓ ĐUNG ĐƯA TỰ NHIÊN) ===")]
    [Tooltip("Tự động tăng giảm nhẹ âm lượng theo thời gian để gió sống động như thật")]
    public bool enableVolumeBreathing = true;
    public float breathingSpeed = 0.35f;
    [Range(0f, 0.3f)] public float breathingAmount = 0.12f;

    [Header("=== RANDOM WIND GUSTS (CƠN GIÓ RÍT THI THOẢNG) ===")]
    [Tooltip("Bật tiếng gió rít thỉnh thoảng ùa về")]
    public bool enableGusts = true;
    [Tooltip("Danh sách clip các cơn gió rít")]
    public AudioClip[] gustClips;
    [Range(0f, 1f)] public float gustVolume = 0.4f;
    [Tooltip("Khoảng thời gian ngẫu nhiên giữa các cơn gió rít (giây)")]
    public Vector2 gustInterval = new Vector2(10f, 22f);

    [Header("=== AUDIO SOURCES (TỰ ĐỘNG KHỞI TẠO NẾU ĐỂ TRỐNG) ===")]
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource gustSource;

    private Coroutine gustCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // 1. AudioSource cho tiếng gió lặp liên tục (2D Ambient Sound)
        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
        }
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 0f; // 2D: Toàn bộ scene đều nghe rõ
        loopSource.priority = 64;     // Ưu tiên cao
        loopSource.mute = false;

        // 2. AudioSource cho tiếng gió rít từng đợt
        if (gustSource == null)
        {
            gustSource = gameObject.AddComponent<AudioSource>();
        }
        gustSource.loop = false;
        gustSource.playOnAwake = false;
        gustSource.spatialBlend = 0f;
        gustSource.priority = 64;
        gustSource.mute = false;

        // Fallback nạp clip nếu chưa gán trong Inspector
        if (windLoopClip == null)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("Amb_Wind t:AudioClip");
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                windLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            if (windLoopClip == null)
            {
                guids = UnityEditor.AssetDatabase.FindAssets("Wind_Loop t:AudioClip");
                if (guids != null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    windLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
#endif
        }

        if (gustClips == null || gustClips.Length == 0)
        {
#if UNITY_EDITOR
            List<AudioClip> list = new List<AudioClip>();
            for (int i = 1; i <= 4; i++)
            {
                string query = i == 1 ? "Wind t:AudioClip" : $"Wind_{i} t:AudioClip";
                string[] guids = UnityEditor.AssetDatabase.FindAssets(query);
                if (guids != null)
                {
                    foreach (string g in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                        if (path.Contains("Horror Enviroment/Wind"))
                        {
                            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                            if (clip != null && !list.Contains(clip))
                            {
                                list.Add(clip);
                                break;
                            }
                        }
                    }
                }
            }
            if (list.Count > 0)
            {
                gustClips = list.ToArray();
            }
#endif
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayWind();
        }
    }

    private void Update()
    {
        if (loopSource != null && loopSource.isPlaying && enableVolumeBreathing)
        {
            // Hiệu ứng thở (Volume Breathing): âm lượng uốn lượn tự nhiên theo Perlin Noise
            float noise = Mathf.PerlinNoise(Time.time * breathingSpeed, 0f);
            float targetVol = baseVolume + (noise - 0.5f) * 2f * breathingAmount;
            loopSource.volume = Mathf.Clamp01(targetVol);
        }
    }

    public void PlayWind()
    {
        SetupAudioSources();

        if (loopSource != null && windLoopClip != null)
        {
            loopSource.clip = windLoopClip;
            loopSource.volume = baseVolume;
            if (!loopSource.isPlaying)
            {
                loopSource.Play();
            }
        }

        if (enableGusts && gustCoroutine == null && gameObject.activeInHierarchy)
        {
            gustCoroutine = StartCoroutine(GustRoutine());
        }
    }

    public void StopWind()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
        }

        if (gustCoroutine != null)
        {
            StopCoroutine(gustCoroutine);
            gustCoroutine = null;
        }
    }

    public void SetVolume(float volume)
    {
        baseVolume = Mathf.Clamp01(volume);
        if (loopSource != null)
        {
            loopSource.volume = baseVolume;
        }
    }

    private IEnumerator GustRoutine()
    {
        while (enableGusts)
        {
            float waitTime = Random.Range(gustInterval.x, gustInterval.y);
            yield return new WaitForSeconds(waitTime);

            if (gustClips != null && gustClips.Length > 0 && gustSource != null)
            {
                AudioClip randomClip = gustClips[Random.Range(0, gustClips.Length)];
                if (randomClip != null)
                {
                    gustSource.pitch = Random.Range(0.92f, 1.08f);
                    gustSource.PlayOneShot(randomClip, gustVolume);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (gustCoroutine != null)
        {
            StopCoroutine(gustCoroutine);
            gustCoroutine = null;
        }
    }
}
