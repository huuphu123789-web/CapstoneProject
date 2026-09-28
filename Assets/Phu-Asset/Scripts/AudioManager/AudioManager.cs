using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager_AutoCreated");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("AudioSource")] 
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;  

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonHoverSFX;
    public AudioClip buttonclickSFX;

    void Awake()
    {
        //*Kiem tra SingleTon
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); //*Giu Audio ton tai khi chuyen scene

        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.mute = false;
        musicSource.mute = false;

        // Nếu có mainMixer nhưng AudioSource chưa gán OutputAudioMixerGroup, tự động gán
        if (mainMixer != null)
        {
            if (sfxSource.outputAudioMixerGroup == null)
            {
                var sfxGroups = mainMixer.FindMatchingGroups("SFX");
                if (sfxGroups != null && sfxGroups.Length > 0)
                    sfxSource.outputAudioMixerGroup = sfxGroups[0];
            }
            if (musicSource.outputAudioMixerGroup == null)
            {
                var musicGroups = mainMixer.FindMatchingGroups("Music");
                if (musicGroups != null && musicGroups.Length > 0)
                    musicSource.outputAudioMixerGroup = musicGroups[0];
            }
        }
    }

    void Start()
    {
        EnsureAudioSources();
        EnsureAudioListener();

        // 1. Đảm bảo AudioListener toàn cầu luôn hoạt động và không bị pause
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        // 2. Tải các giá trị âm lượng đã lưu từ PlayerPrefs
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        bool isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        bool isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        // Tự động khôi phục nếu âm lượng bị 0 hoặc Mute nhầm
        if (savedMaster <= 0.02f || isMasterMuted)
        {
            Debug.LogWarning("[AudioManager] Phát hiện MasterVolume bị 0 hoặc Muted! Tự động khôi phục về 100% (1.0).");
            savedMaster = 1f;
            isMasterMuted = false;
            PlayerPrefs.SetFloat("MasterVolume", 1f);
            PlayerPrefs.SetInt("MasterMuted", 0);
            PlayerPrefs.Save();
        }

        if (savedSFX <= 0.02f || isSFXMuted)
        {
            savedSFX = 1f;
            isSFXMuted = false;
            PlayerPrefs.SetFloat("SFXVolume", 1f);
            PlayerPrefs.SetInt("SFXMuted", 0);
            PlayerPrefs.Save();
        }

        if (savedMusic <= 0.02f)
        {
            savedMusic = 1f;
            PlayerPrefs.SetFloat("MusicVolume", 1f);
            PlayerPrefs.Save();
        }

        // 3. Áp dụng âm lượng ngay lập tức
        SetMasterVolume(savedMaster);
        SetSFXVolume(savedSFX);
        SetMusicVolume(savedMusic);

        // 4. Phát nhạc nền nếu có
        if (backgroundMusic != null) PlayMusic(backgroundMusic);
    }

    void Update()
    {
        // Luôn đảm bảo AudioListener không bị pause ngoài ý muốn
        if (AudioListener.pause)
        {
            AudioListener.pause = false;
        }

        // Phím tắt khẩn cấp: Nhấn F8 để khôi phục 100% toàn bộ âm thanh trong game
        if (Input.GetKeyDown(KeyCode.F8))
        {
            RestoreAllAudio();
        }
    }

    private void EnsureAudioListener()
    {
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[AudioManager] Đã tự động gắn AudioListener vào Main Camera!");
            }
        }
        else
        {
            listener.enabled = true;
        }
    }

    /// <summary>
    /// Khôi phục toàn bộ âm thanh và âm lượng về 100% (dùng khi bị mất tiếng)
    /// </summary>
    public void RestoreAllAudio()
    {
        EnsureAudioSources();
        EnsureAudioListener();

        AudioListener.pause = false;
        AudioListener.volume = 1f;

        PlayerPrefs.SetFloat("MasterVolume", 1f);
        PlayerPrefs.SetFloat("SFXVolume", 1f);
        PlayerPrefs.SetFloat("MusicVolume", 1f);
        PlayerPrefs.SetInt("MasterMuted", 0);
        PlayerPrefs.SetInt("SFXMuted", 0);
        PlayerPrefs.Save();

        SetMasterVolume(1f);
        SetSFXVolume(1f);
        SetMusicVolume(1f);

        if (backgroundMusic != null && (musicSource == null || !musicSource.isPlaying))
        {
            PlayMusic(backgroundMusic);
        }

        Debug.Log("[AudioManager] Đã khôi phục toàn bộ âm thanh về 100%!");
    }

    public void PlayMusic(AudioClip clip)
    {
        if(clip == null) return;
        EnsureAudioSources();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if(clip == null) return;
        EnsureAudioSources();
        sfxSource.PlayOneShot(clip);
    }

    public void SetMasterVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MasterVol", dbValue);
            mainMixer.SetFloat("MasterVolume", dbValue);
        }
        AudioListener.volume = Mathf.Clamp01(sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MusicVol", dbValue);
            mainMixer.SetFloat("MusicVolume", dbValue);
        }
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("SFXVol", dbValue);
            mainMixer.SetFloat("SFXVolume", dbValue);
        }
    }
}
