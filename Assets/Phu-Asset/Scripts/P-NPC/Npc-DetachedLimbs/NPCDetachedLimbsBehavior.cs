using UnityEngine;
using UnityEngine.AI;

public class NPCDetachedLimbsBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Xương Tách Rời (Detached Limbs) ===")]
    [Tooltip("Kéo xương Cánh Tay (LeftArm / RightArm) hoặc Chân vào đây")]
    public Transform leftArmBone;
    public Transform rightArmBone;
    public float detachDistance = 0.4f; // Khoảng cách tách rời khỏi cơ thể
    public float floatSpeed = 4f;

    [Header("=== Âm Thanh & Footstep ===")]
    public AudioClip[] footstepSounds;
    public AudioClip detachSound;        // Tiếng rắc rắc thịt/xương tách ra
    public float footstepInterval = 0.45f;
        private float footstepTimer;

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Vector3 origLeftArmPos;
    private Vector3 origRightArmPos;
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
        if (leftArmBone != null) origLeftArmPos = leftArmBone.localPosition;
        if (rightArmBone != null) origRightArmPos = rightArmBone.localPosition;

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
        PlaySound(detachSound);
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
    }

    void LateUpdate()
    {
        // KHI ĐANG KIỂM DUYỆT -> TÁCH VÀ LƠ LỬNG CÁNH TAY RA NGOÀI
        if (isInspecting)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * 0.15f;

            if (leftArmBone != null)
            {
                leftArmBone.localPosition = origLeftArmPos + new Vector3(-detachDistance, offsetY, 0f);
            }

            if (rightArmBone != null)
            {
                rightArmBone.localPosition = origRightArmPos + new Vector3(detachDistance, -offsetY, 0f);
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