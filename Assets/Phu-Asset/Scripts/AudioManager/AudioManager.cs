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
                _instance = FindObjectOfType<AudioManager>();
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
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureAudioSources();

        // 1. Tải các giá trị âm lượng đã lưu từ PlayerPrefs
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        // Tải trạng thái Mute (tắt tiếng) nếu có
        bool isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        bool isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        // 2. Áp dụng âm lượng ngay lập tức vào Mixer lúc khởi động game
        SetMasterVolume(isMasterMuted ? 0f : savedMaster);
        SetSFXVolume(isSFXMuted ? 0f : savedSFX);
        SetMusicVolume(savedMusic);
        // 3. Phát nhạc nền
        if (backgroundMusic != null) PlayMusic(backgroundMusic);
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
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MasterVol", dbValue);
            mainMixer.SetFloat("MasterVolume", dbValue); // Fallback nếu expose tên MasterVolume
        }
    }

    public void SetMusicVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MusicVol", dbValue);
            mainMixer.SetFloat("MusicVolume", dbValue); // Fallback nếu expose tên MusicVolume
        }
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue) * 20 : -80f;
        if (mainMixer != null)
        {
            mainMixer.SetFloat("SFXVol", dbValue);
            mainMixer.SetFloat("SFXVolume", dbValue); // Fallback nếu expose tên SFXVolume
        }
    }

    
}
