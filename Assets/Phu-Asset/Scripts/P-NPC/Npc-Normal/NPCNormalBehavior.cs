using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
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
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
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

        if (animator != null) animator.SetBool("isWalk", true);
    }

    public void StartInspection()
    {
        isInspecting = true;
        agent.isStopped = true;
        if (animator != null) animator.SetBool("isWalk", false);
    }

    void Update()
    {
        // Âm thanh bước chân khi đi bộ
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