using UnityEngine;
using UnityEngine.AI;

public class NPCNormalBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Âm Thanh Bước Chân ===")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.45f;
    private float footstepTimer;

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;

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
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
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
        // Âm thanh bước chân khi đi bộ
        if (!isInspecting && agent != null && agent.isOnNavMesh && agent.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        // Kiểm tra tới vạch kiểm duyệt
        if (!isInspecting && agent != null && agent.isOnNavMesh && agent.enabled && !agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance > 0.1f && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        int randomIndex = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[randomIndex];

        if (clip != null)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
            else if (audioSource != null) audioSource.PlayOneShot(clip, 0.6f);
        }
    }
}