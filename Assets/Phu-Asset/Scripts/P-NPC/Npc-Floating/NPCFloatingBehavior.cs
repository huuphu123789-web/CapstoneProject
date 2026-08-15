using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCFloatingBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Cấu Hình Lơ Lửng ===")]
    public float floatHeight = 0.6f;  // Chiều cao bay lên
    public float floatSpeed = 2.5f;   // Tốc độ nhấp nhô lơ lửng

    [Header("=== Âm Thanh & Footstep ===")]
    public AudioClip[] footstepSounds;
    public AudioClip floatHumSound;   // Tiếng vi vu/u u lơ lửng
    public float footstepInterval = 0.45f;
    private float footstepTimer;

    
    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Vector3 originalLocalPos;
    private float soundTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        originalLocalPos = transform.localPosition;
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
    }

    public void StartInspection()
    {
        isInspecting = true;
        agent.isStopped = true;
        if (animator != null) animator.SetBool("isWalk", false);
        originalLocalPos = transform.localPosition;
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

        // KHI ĐANG KIỂM DUYỆT -> PHÁT TIẾNG LƠ LỬNG
        if (isInspecting && floatHumSound != null)
        {
            soundTimer -= Time.deltaTime;
            if (soundTimer <= 0)
            {
                PlaySound(floatHumSound);
                soundTimer = 1.5f;
            }
        }
    }

    void LateUpdate()
    {
        // KHI ĐẮNG KIỂM DUYỆT -> BAY LƠ LỬNG LÊN TRỜI
        if (isInspecting)
        {
            float newY = Mathf.Sin(Time.time * floatSpeed) * 0.25f + floatHeight;
            transform.localPosition = originalLocalPos + new Vector3(0f, newY, 0f);
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