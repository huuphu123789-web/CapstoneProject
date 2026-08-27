using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioEvent : MonoBehaviour
{
    public static AudioEvent Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioData[] audioClips;

    public event Action<string> OnAudioPlay;
    public event Action<string> OnAudioStop;
    public event Action<string> OnAudioComplete;

    private Coroutine musicCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
        }
    }
   
    public void PlaySFX(string audioName)
    {
        AudioData audio = FindAudio(audioName);

        if (audio == null)
            return;

        if (audio.clip == null)
            return;

        sfxSource.PlayOneShot(
            audio.clip,
            audio.volume
        );

        OnAudioPlay?.Invoke(audioName);
    }

    public void PlaySFX3D(
        string audioName,
        Vector3 position)
    {
        AudioData audio = FindAudio(audioName);

        if (audio == null)
            return;

        if (audio.clip == null)
            return;

        GameObject audioObject =
            new GameObject("Audio_" + audioName);

        audioObject.transform.position = position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.clip = audio.clip;
        source.volume = audio.volume;
        source.spatialBlend = 1f;
        source.minDistance = audio.minDistance;
        source.maxDistance = audio.maxDistance;
        source.Play();

        Destroy(
            audioObject,
            audio.clip.length + 0.1f
        );

        OnAudioPlay?.Invoke(audioName);
    }


    public void PlayBGM(string audioName)
    {
        AudioData audio = FindAudio(audioName);

        if (audio == null)
            return;

        if (audio.clip == null)
            return;

        bgmSource.clip = audio.clip;
        bgmSource.volume = audio.volume;
        bgmSource.loop = true;

        bgmSource.Play();

        OnAudioPlay?.Invoke(audioName);
    }


    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();

        OnAudioStop?.Invoke("BGM");
    }


    public void PauseBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Pause();
    }


    public void ResumeBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.UnPause();
    }


    public void StopAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
    }


    public void FadeOutBGM(float duration)
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        musicCoroutine =
            StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(
        float duration)
    {
        float startVolume = bgmSource.volume;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            bgmSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    time / duration
                );

            yield return null;
        }

        bgmSource.volume = 0f;

        bgmSource.Stop();
    }


    public void FadeInBGM(
        string audioName,
        float duration)
    {
        AudioData audio = FindAudio(audioName);

        if (audio == null)
            return;

        if (audio.clip == null)
            return;

        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }

        musicCoroutine =
            StartCoroutine(
                FadeInCoroutine(
                    audio,
                    duration
                )
            );
    }

    private IEnumerator FadeInCoroutine(
        AudioData audio,
        float duration)
    {
        bgmSource.clip = audio.clip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;

        bgmSource.Play();

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            bgmSource.volume =
                Mathf.Lerp(
                    0f,
                    audio.volume,
                    time / duration
                );

            yield return null;
        }

        bgmSource.volume = audio.volume;

        OnAudioComplete?.Invoke(
            audio.audioName
        );
    }

    private AudioData FindAudio(
        string audioName)
    {
        foreach (AudioData audio in audioClips)
        {
            if (audio.audioName == audioName)
            {
                return audio;
            }
        }

        Debug.LogWarning(
            "Không tìm thấy Audio: " +
            audioName
        );

        return null;
    }

    public void SetMasterVolume(float volume)
    {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat(
            "MasterVolume",
            Mathf.Log10(
                Mathf.Clamp(volume, 0.0001f, 1f)
            ) * 20f
        );
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat(
            "MusicVolume",
            Mathf.Log10(
                Mathf.Clamp(volume, 0.0001f, 1f)
            ) * 20f
        );
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat(
            "SFXVolume",
            Mathf.Log10(
                Mathf.Clamp(volume, 0.0001f, 1f)
            ) * 20f
        );
    }
}

[Serializable]
public class AudioData
{
    public string audioName;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("3D Audio")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    public float minDistance = 1f;

    public float maxDistance = 30f;
}