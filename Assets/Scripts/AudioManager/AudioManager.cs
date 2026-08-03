using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; 
    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("AudioSource")] 
    [SerializeField]  private AudioSource musicSource;
    [SerializeField] private AudioSource  sfxSource;  

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonHoverSFX;
    public AudioClip buttonclickSFX;

    void Awake()
    {
        //*Kiem tra SingleTon
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); //*Giu Audio ton tai khi chuyen scene

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if(clip ==null)
        {
            return;
        }
        musicSource.clip=clip;
        musicSource.loop=true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if(clip== null)
        {
            return;
        }
        //* PlayoneShot giup phat  cac am thanh ngan de len nhau  ma khong bi ngat quang
        sfxSource.PlayOneShot(clip);
    }

    public void SetMasterVolume(float sliderValue)
    {
        //*Cong thuc doi tu 0->1 sang Decibel: 20 * log10(sliderValue)
        //*Neu sliderValue =0 (toi thieu), ta dat cung la -80dB (im lang hoan toan)
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue) *20 : -80f;
        mainMixer.SetFloat("MusicVol",dbValue); //* "MasterVol" la ten bien da expose o Mixer

    }

    public void SetMusicVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue) *20 : -80f;
        mainMixer.SetFloat("MusicVol",dbValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dbValue = sliderValue > 0 ? Mathf.Log10(sliderValue)* 20 : -80f;
        mainMixer.SetFloat("SFXVol",dbValue);
    }

    
}
