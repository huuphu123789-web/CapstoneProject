using UnityEngine;
using UnityEngine.AI;

public class NPCHeadSpinBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Cấu Hình Xoay Đầu ===")]
    public Transform headBone;        // Kéo xương Head vào đây
    public float spinSpeed = 360f;     // Tốc độ xoay (360 độ/giây)

    [Header("=== Âm Thanh & Footstep ===")]
    public AudioClip[] footstepSounds;
    public AudioClip headSpinSound;    // Tiếng rắc rắc xoay đầu
    public float footstepInterval = 0.45f;
    private float footstepTimer;
    

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Quaternion originalHeadRot;
    private float currentSpinAngle = 0f;
    private float soundTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
    }

    void Start()
    {
        if (headBone != null) originalHeadRot = headBone.localRotation;
        if (inspectionPoint == null)
        {
            GameObject zoneObj = GameObject.Find("InspectionZone");
            if (zoneObj != null) inspectionPoint = zoneObj.transform;
        }

        if (NPCInspectionManager.instance == null && inspectionPoint != null)
        {
            MoveToInspectionPoint();
        }
    }

    private void EnsureAgentOnNavMesh()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    public void MoveToInspectionPoint()
    {
        isInspecting = false;
        EnsureAgentOnNavMesh();
        if (agent != null && agent.isOnNavMesh && inspectionPoint != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            Vector3 target = inspectionPoint.position;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 20f, NavMesh.AllAreas)) target = hit.position;
            agent.SetDestination(target);
        }
        if (animator != null) animator.SetBool("isWalk", true);
    }

    public void StartInspection()
    {
        isInspecting = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (animator != null) animator.SetBool("isWalk", false);
    }

    void Update()
    {
        if (!isInspecting && agent != null && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        if (!isInspecting && agent != null && agent.isOnNavMesh && agent.enabled && !agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance > 0.1f && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }

        // KHI ĐANG KIỂM DUYỆT -> PHÁT TIẾNG XOAY ĐẦU
        if (isInspecting && headSpinSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(headSpinSound);
                soundTimer = 0.6f;
            }
        }
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT -> XOAY ĐẦU 360 ĐỘ
        if (isInspecting && headBone != null)
        {
            currentSpinAngle += Time.deltaTime * spinSpeed;
            headBone.localRotation = originalHeadRot * Quaternion.Euler(0f, currentSpinAngle, 0f);
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