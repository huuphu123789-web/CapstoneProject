using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum GlitchStyle
{
    ContinuousJitter,   // Giật liên tục không ngừng
    RandomSpasm,        // Thỉnh thoảng bẻ ngoắt đầu/giật mình
    ViolentShake        // Rung lắc dữ dội toàn thân
}

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWeirdBehaviors : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Âm Thanh Bước Chân (Footsteps) ===")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.45f;
    private float footstepTimer;

    [Header("=== Trạng Thái NPC ===")]
    public bool isAnomaly = true;
    public bool isInspecting = false;
    public GlitchStyle currentGlitchStyle;

    [Header("=== References ===")]
    public Transform headBone;
    public Animator animator;
    public AudioSource audioSource; // AudioSource dự phòng trên NPC

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

    private NavMeshAgent agent;
    private Vector3 originalLocalPos;
    private Quaternion originalHeadRot;
    private float spasmTimer;
    private float soundTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        
        // Nếu quên kéo AudioSource thì tự động tạo/gắn vào NPC
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Start()
    {
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

        if (inspectionPoint == null)
        {
            GameObject zoneObj = GameObject.Find("InspectionZone");
            if (zoneObj != null) inspectionPoint = zoneObj.transform;
        }

        if (inspectionPoint != null)
        {
            MoveToInspectionPoint();
        }
    }

    public void MoveToInspectionPoint()
    {
        isInspecting = false;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(inspectionPoint.position);

        if (animator != null)
        {
            animator.SetBool("isWalk", true);
        }
    }

    public void RandomizeGlitchStyle()
    {
        int randomIndex = Random.Range(0, 3);
        currentGlitchStyle = (GlitchStyle)randomIndex;
    }

    public void StartInspection()
    {
        isInspecting = true;
        agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool("isWalk", false);
        }

        originalLocalPos = transform.localPosition;
    }

    public void StopInspection()
    {
        isInspecting = false;
        if (headBone != null) headBone.localRotation = originalHeadRot;
        transform.localPosition = originalLocalPos;
    }

    void Update()
    {
        // 1. ÂM THANH BƯỚC CHÂN
        if (!isInspecting && agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        // Kiểm tra tới vạch kiểm duyệt
        if (!isInspecting && agent.enabled && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }

        // 2. GIẬT GIẶT & ÂM THANH GIẬT
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

    /// <summary>
    /// Hàm phát Sound thông minh (Ưu tiên AudioManager, nếu không có sẽ tự phát qua AudioSource)
    /// </summary>
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

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        int randomIndex = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[randomIndex];

        PlaySoundSmart(clip, 0.6f);
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