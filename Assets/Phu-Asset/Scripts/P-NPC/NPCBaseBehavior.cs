using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Script cha cơ sở (Base Class) cho tất cả các loại NPC trong game.
/// Quản lý tập trung:
/// - Hệ thống NavMeshAgent tìm đường và di chuyển tới bốt gác (inspectionPoint).
/// - Tốc độ đi bộ (walkSpeed).
/// - Hệ thống âm thanh bước chân (footstepSounds & footstepInterval).
/// - Trạng thái kiểm duyệt (isInspecting).
/// - Tự động tìm kiếm Animator, AudioSource, NavMeshAgent.
/// Các hành vi dị biệt (Floating, HeadSpin, Crawler, Boomer, Glitch...) chỉ cần kế thừa và override các hàm ảo (virtual).
/// </summary>
public abstract class NPCBaseBehavior : MonoBehaviour
{
    [Header("=== Cấu Hình Tự Đi Bộ ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.5f;

    [Header("=== Âm Thanh Bước Chân ===")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.45f;
    protected float footstepTimer;

    [Header("=== Trạng Thái ===")]
    public bool isInspecting = false;

    [Header("=== References Cơ Bản ===")]
    public Animator animator;
    public AudioSource audioSource;
    public NavMeshAgent agent;

    protected virtual void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
    }

    protected virtual void Start()
    {
        if (inspectionPoint == null)
        {
            if (NPCInspectionManager.instance != null && NPCInspectionManager.instance.inspectionPoint != null)
            {
                inspectionPoint = NPCInspectionManager.instance.inspectionPoint;
            }
            else
            {
                GameObject zoneObj = GameObject.Find("InspectionZone");
                if (zoneObj == null) zoneObj = GameObject.Find("InspectionPoint");
                if (zoneObj != null) inspectionPoint = zoneObj.transform;
            }
        }

        // Tự động di chuyển nếu không có Manager điều phối sẵn
        if (NPCInspectionManager.instance == null && inspectionPoint != null)
        {
            MoveToInspectionPoint();
        }
    }

    public virtual void EnsureAgentOnNavMesh()
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

    public virtual void MoveToInspectionPoint()
    {
        isInspecting = false;
        EnsureAgentOnNavMesh();

        if (agent != null && agent.isOnNavMesh && inspectionPoint != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            Vector3 target = inspectionPoint.position;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                target = hit.position;
            }
            agent.SetDestination(target);
        }

        if (animator != null) animator.SetBool("isWalk", true);
    }

    public virtual void StartInspection()
    {
        isInspecting = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetBool("isWalk", false);
        }

        OnInspectionStarted();
    }

    /// <summary>
    /// Hàm ảo được gọi khi NPC đã dừng lại tại vạch kiểm tra.
    /// Các script con (Floating, Glitch, Jumper, Screamer...) ghi đè hàm này để kích hoạt hành vi riêng.
    /// </summary>
    protected virtual void OnInspectionStarted()
    {
    }

    protected virtual void Update()
    {
        HandleFootsteps();
        CheckArrivalAtInspectionPoint();
    }

    protected virtual void HandleFootsteps()
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
    }

    protected virtual void CheckArrivalAtInspectionPoint()
    {
        if (!isInspecting && agent != null && agent.isOnNavMesh && agent.enabled && !agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance > 0.1f && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                StartInspection();
            }
        }
    }

    public virtual void PlayFootstepSound()
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

    public static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    public static void GetPlayerCameraOrHead(out Vector3 eyePos, out Vector3 forwardDir)
    {
        // 1. Kiểm tra điểm Jumpscare trên Camera nếu đã có
        if (PlayerCameraJumpscarePoint.Instance != null && PlayerCameraJumpscarePoint.Instance.transform.parent != null)
        {
            Transform camT = PlayerCameraJumpscarePoint.Instance.transform.parent;
            eyePos = camT.position;
            forwardDir = camT.forward;
            return;
        }

        // 2. Tìm nhân vật Player trong Scene
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in taggedPlayers)
            {
                if (p.scene.IsValid() && p.activeInHierarchy) { playerObj = p; break; }
            }
            if (playerObj == null && taggedPlayers.Length > 0) playerObj = taggedPlayers[0];
        }

        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

        if (playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;
            if (mainCam != null && Vector3.Distance(mainCam.transform.position, playerPos) < 3.5f)
            {
                eyePos = mainCam.transform.position;
                forwardDir = mainCam.transform.forward;
                return;
            }

            Transform head = playerObj.transform.Find("HeadTarget");
            if (head == null) head = playerObj.transform.Find("CM_FirstPreson");
            if (head == null)
            {
                Camera childCam = playerObj.GetComponentInChildren<Camera>();
                if (childCam != null) head = childCam.transform;
            }

            if (head != null)
            {
                eyePos = head.position;
                forwardDir = head.forward;
                return;
            }

            eyePos = playerPos + Vector3.up * 1.6f;
            forwardDir = playerObj.transform.forward;
            return;
        }

        if (mainCam != null)
        {
            eyePos = mainCam.transform.position;
            forwardDir = mainCam.transform.forward;
            return;
        }

        eyePos = new Vector3(-48.63f, 1.6f, -50.45f);
        forwardDir = Vector3.forward;
    }

    protected virtual void TriggerCameraShake(float duration = 0.6f, float magnitude = 0.5f)
    {
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(duration, magnitude);
            return;
        }

        CameraShake shaker = Object.FindFirstObjectByType<CameraShake>();
        if (shaker == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) shaker = player.AddComponent<CameraShake>();
            else if (Camera.main != null) shaker = Camera.main.gameObject.AddComponent<CameraShake>();
        }

        if (shaker != null)
        {
            shaker.Shake(duration, magnitude);
        }
    }
}
