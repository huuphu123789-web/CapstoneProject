using System.Collections;
using UnityEngine;

public enum GlitchStyle
{
    ContinuousJitter,   // Giật liên tục không ngừng
    RandomSpasm,        // Thỉnh thoảng bẻ ngoắt đầu/giật mình
    ViolentShake        // Rung lắc dữ dội toàn thân
}

/// <summary>
/// Hành vi NPC Giật Lag (Glitch / Spasm Mutant):
/// Kế thừa từ NPCBaseBehavior. Khi đến bốt gác kiểm tra, NPC sẽ có các biểu hiện co giật kì dị.
/// </summary>
public class NPCWeirdBehaviors : NPCBaseBehavior
{
    [Header("=== Trạng Thái Dị Biệt ===")]
    public bool isAnomaly = true;
    public GlitchStyle currentGlitchStyle;

    [Header("=== References Riêng ===")]
    public Transform headBone;

    [Header("=== Âm Thanh Cho Từng Kiểu Giật ===")]
    public AudioClip continuousGlitchSound;
    public AudioClip spasmBoneSound;
    public AudioClip violentShakeSound;
    public float soundInterval = 0.25f; 

    [Header("=== Thông Số Giật ===")]
    [Range(0.01f, 0.15f)] public float positionJitterAmount = 0.04f;
    [Range(5f, 60f)] public float rotationJitterAmount = 30f;
    public float minSpasmInterval = 0.8f;
    public float maxSpasmInterval = 3f;

    private Vector3 originalLocalPos;
    private Quaternion originalHeadRot;
    private float spasmTimer;
    private float soundTimer;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    protected override void Start()
    {
        base.Start();

        originalLocalPos = transform.localPosition;
        if (headBone != null)
        {
            originalHeadRot = headBone.localRotation;
        }

        if (isAnomaly)
        {
            RandomizeGlitchStyle();
        }

        spasmTimer = Random.Range(minSpasmInterval, maxSpasmInterval);
    }

    public void RandomizeGlitchStyle()
    {
        int randomIndex = Random.Range(0, 3);
        currentGlitchStyle = (GlitchStyle)randomIndex;
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();
        originalLocalPos = transform.localPosition;
    }

    public void StopInspection()
    {
        isInspecting = false;
        if (headBone != null) headBone.localRotation = originalHeadRot;
        transform.localPosition = originalLocalPos;
    }

    protected override void Update()
    {
        base.Update();

        // GIẬT GIẶT & ÂM THANH GIẬT
        if (!isAnomaly || !isInspecting) return;

        soundTimer -= Time.deltaTime;

        if (currentGlitchStyle == GlitchStyle.RandomSpasm)
        {
            spasmTimer -= Time.deltaTime;
            if (spasmTimer <= 0)
            {
                StartCoroutine(TriggerSingleSpasm());
                spasmTimer = Random.Range(minSpasmInterval, maxSpasmInterval);
            }
        }
        else
        {
            if (soundTimer <= 0)
            {
                PlayStyleSpecificSound();
                soundTimer = soundInterval + Random.Range(-0.05f, 0.08f);
            }
        }
    }

    void LateUpdate()
    {
        if (!isAnomaly || !isInspecting) return;

        if (currentGlitchStyle == GlitchStyle.ContinuousJitter)
        {
            transform.localPosition = originalLocalPos + Random.insideUnitSphere * positionJitterAmount;

            if (headBone != null)
            {
                headBone.localRotation = originalHeadRot * Quaternion.Euler(
                    Random.Range(-rotationJitterAmount, rotationJitterAmount),
                    Random.Range(-rotationJitterAmount, rotationJitterAmount),
                    Random.Range(-rotationJitterAmount, rotationJitterAmount)
                );
            }
        }
        else if (currentGlitchStyle == GlitchStyle.ViolentShake)
        {
            float shakeX = Mathf.Sin(Time.time * 50f) * positionJitterAmount * 2f;
            float shakeZ = Mathf.Cos(Time.time * 45f) * positionJitterAmount * 2f;
            transform.localPosition = originalLocalPos + new Vector3(shakeX, 0, shakeZ);

            if (headBone != null)
            {
                float headRoll = Mathf.Sin(Time.time * 40f) * rotationJitterAmount * 1.5f;
                headBone.localRotation = originalHeadRot * Quaternion.Euler(0, 0, headRoll);
            }
        }
    }

    private void PlaySoundSmart(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.25f);
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlayStyleSpecificSound()
    {
        AudioClip clipToPlay = null;

        switch (currentGlitchStyle)
        {
            case GlitchStyle.ContinuousJitter:
                clipToPlay = continuousGlitchSound;
                break;
            case GlitchStyle.RandomSpasm:
                clipToPlay = spasmBoneSound;
                break;
            case GlitchStyle.ViolentShake:
                clipToPlay = violentShakeSound;
                break;
        }

        PlaySoundSmart(clipToPlay, 1.0f);
    }

    private IEnumerator TriggerSingleSpasm()
    {
        PlayStyleSpecificSound();

        float duration = Random.Range(0.06f, 0.18f);
        float elapsed = 0f;

        Vector3 randomAngle = new Vector3(
            Random.Range(-rotationJitterAmount * 1.5f, rotationJitterAmount * 1.5f),
            Random.Range(-rotationJitterAmount * 1.5f, rotationJitterAmount * 1.5f),
            Random.Range(-rotationJitterAmount * 1.5f, rotationJitterAmount * 1.5f)
        );

        while (elapsed < duration && isInspecting)
        {
            elapsed += Time.deltaTime;

            if (headBone != null)
            {
                headBone.localRotation = originalHeadRot * Quaternion.Euler(randomAngle);
            }

            transform.localPosition = originalLocalPos + Random.insideUnitSphere * (positionJitterAmount * 1.5f);
            yield return null;
        }

        if (headBone != null) headBone.localRotation = originalHeadRot;
        transform.localPosition = originalLocalPos;
    }
}