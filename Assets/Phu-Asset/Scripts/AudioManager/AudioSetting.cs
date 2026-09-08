using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Mute Master Button")]
    [SerializeField] private Image masterMuteImage;     
    [SerializeField] private Sprite masterSoundOnSprite;      
    [SerializeField] private Sprite masterSoundOffSprite;     

    [Header("Mute SFX Button")]
    [SerializeField] private Image sfxMuteImage;        
    [SerializeField] private Sprite sfxSoundOnSprite;         
    [SerializeField] private Sprite sfxSoundOffSprite;        

    private bool isMasterMuted = false;
    private bool isSFXMuted = false;

    void Start()
    {
        // Chỉ làm nhiệm vụ hiển thị đúng vị trí thanh Slider khi người chơi mở bảng cài đặt
    float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
    float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
    float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
    if (masterSlider != null) masterSlider.value = 1f;
    if (musicSlider != null) musicSlider.value = savedMusic;
    if (sfxSlider != null) sfxSlider.value = savedSFX;
    
    // Tự động cập nhật lại hình ảnh nút Loa Bật/Tắt cho đúng trạng thái đã lưu
    isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
    isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
    
    ApplyMasterMuteState(savedMaster);
    ApplySFXMuteState(savedSFX);

    }

    // ================= XỬ LÝ MUTE MASTER =================
    public void ToggleMuteMaster()
    {
        isMasterMuted = !isMasterMuted;
        PlayerPrefs.SetInt("MasterMuted", isMasterMuted ? 1 : 0);

        float currentVol = masterSlider != null ? masterSlider.value : 1f;
        ApplyMasterMuteState(currentVol);
    }

    private void ApplyMasterMuteState(float volumeValue)
    {
        if (AudioManager.instance != null)
        {
            if (isMasterMuted)
            {
                AudioManager.instance.SetMasterVolume(0f);
            }
            else
            {
                AudioManager.instance.SetMasterVolume(volumeValue);
            }
        }

        if (masterMuteImage != null)
        {
            masterMuteImage.sprite = isMasterMuted ? masterSoundOffSprite : masterSoundOnSprite;
        }
    }

    // ================= XỬ LÝ MUTE SFX =================
    public void ToggleMuteSFX()
    {
        isSFXMuted = !isSFXMuted;
        PlayerPrefs.SetInt("SFXMuted", isSFXMuted ? 1 : 0);

        float currentVol = sfxSlider != null ? sfxSlider.value : 1f;
        ApplySFXMuteState(currentVol);
    }

    private void ApplySFXMuteState(float volumeValue)
    {
        if (AudioManager.instance != null)
        {
            if (isSFXMuted)
            {
                AudioManager.instance.SetSFXVolume(0f);
            }
            else
            {
                AudioManager.instance.SetSFXVolume(volumeValue);
            }
        }

        if (sfxMuteImage != null)
        {
            sfxMuteImage.sprite = isSFXMuted ? sfxSoundOffSprite : sfxSoundOnSprite;
        }
    }

    // ================= XỬ LÝ KHI KÉO SLIDER =================
    public void OnMasterSliderChanged(float value)
    {
        if (!isMasterMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(value);
        }
    }

    public void OnSFXSliderChanged(float value)
    {
        if (!isSFXMuted && AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
    }
}