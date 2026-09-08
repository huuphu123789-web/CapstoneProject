using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCSpinningLegsBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Cấu Hình Chân Xoay Chong Chóng ===")]
    [Tooltip("Kéo xương Đùi/Chân trái (LeftUpLeg / LeftLeg) vào đây")]
    public Transform leftLegBone;
    [Tooltip("Kéo xương Đùi/Chân phải (RightUpLeg / RightLeg) vào đây")]
    public Transform rightLegBone;
    public float spinSpeed = 720f; // Tốc độ xoay (720 độ/giây = 2 vòng/s)

    [Header("=== Âm Thanh & Footstep ===")]
    public AudioClip[] footstepSounds;
    public AudioClip legSpinSound; // Tiếng vồ vập / xoay rắc rắc
    public float footstepInterval = 0.45f;

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Quaternion origLeftLegRot;
    private Quaternion origRightLegRot;
    private float footstepTimer;
    private float spinAngle = 0f;
    private float soundTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (leftLegBone != null) origLeftLegRot = leftLegBone.localRotation;
        if (rightLegBone != null) origRightLegRot = rightLegBone.localRotation;
    }

    void Start()
    {
        if (inspectionPoint == null)
        {
            GameObject zoneObj = GameObject.Find("InspectionZone");
            if (zoneObj != null) inspectionPoint = zoneObj.transform;
        }

        if (inspectionPoint != null) MoveToInspectionPoint();
    }

    public void MoveToInspectionPoint()
    {
        isInspecting = false;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(inspectionPoint.position);
        if (animator != null) animator.SetBool("isWalk", true);

        // Trở lại vị trí chân bình thường khi bước đi
        if (leftLegBone != null) leftLegBone.localRotation = origLeftLegRot;
        if (rightLegBone != null) rightLegBone.localRotation = origRightLegRot;
    }

    public void StartInspection()
    {
        isInspecting = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        if (animator != null) animator.SetBool("isWalk", false);
    }

    void Update()
    {
        if (!isInspecting && agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        if (!isInspecting && agent.enabled && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }

        // Phát âm thanh xoay chân theo chu kỳ
        if (isInspecting && legSpinSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(legSpinSound);
                soundTimer = 0.5f;
            }
        }
    }

    // LẠI UPDATE: XOAY 2 CHÂN NHƯ CHONG CHÓNG KHI ĐẾN VẠCH KIỂM DUYỆT
    void LateUpdate()
    {
        if (isInspecting)
        {
            spinAngle += Time.deltaTime * spinSpeed;

            // Chân trái xoay thuận chiều
            if (leftLegBone != null)
            {
                leftLegBone.localRotation = origLeftLegRot * Quaternion.Euler(spinAngle, 0f, 0f);
            }

            // Chân phải xoay ngược chiều (hoặc cùng chiều) tạo hiệu ứng chong chóng
            if (rightLegBone != null)
            {
                rightLegBone.localRotation = origRightLegRot * Quaternion.Euler(-spinAngle, 0f, 0f);
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        int randomIndex = Random.Range(0, footstepSounds.Length);
        PlaySound(footstepSounds[randomIndex], 0.6f);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}