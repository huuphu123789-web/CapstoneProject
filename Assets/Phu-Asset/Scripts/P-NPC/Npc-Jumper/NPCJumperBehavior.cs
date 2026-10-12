using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Jumper Mutant:
/// Approaches the inspection booth quietly. While being inspected, instead of physically
/// jumping forward, it triggers a horrifying jumpscare model (auto-detected WindowJumpScare)
/// right at the window or player's face, accompanied by violent screen shake and audio impact.
/// </summary>
public class NPCJumperBehavior : MonoBehaviour
{
    [Header("=== MOVEMENT CONFIGURATION ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.4f;

    [Header("=== JUMPSCARE TRIGGER TIMING ===")]
    [Tooltip("Min and Max seconds before triggering the jumpscare after arriving at the booth")]
    public float minTriggerDelay = 2.0f;
    public float maxTriggerDelay = 4.0f;

    [Header("=== JUMPSCARE MODEL SETTINGS ===")]
    [Tooltip("Drag a jumpscare GameObject (e.g. WindowJumpScare) here. Auto-detects in scene if null.")]
    public GameObject jumpscareModel;

    [Tooltip("Drag a jumpscare Prefab here as fallback if no scene object is used.")]
    public GameObject jumpscarePrefab;

    [Tooltip("Target window or spawn point where the jumpscare model will appear. Auto-detects if null.")]
    public Transform jumpscareSpawnPoint;

    [Tooltip("Distance in front of Camera if spawning directly in front of the player")]
    public float cameraForwardOffset = 1.2f;

    [Tooltip("How long the jumpscare model remains visible before disappearing (seconds)")]
    public float jumpscareDuration = 1.8f;

    [Tooltip("Temporarily hide the normal NPC visual renderers while the jumpscare model is active")]
    public bool hideNormalModelDuringScare = true;

    [Header("=== CAMERA SHAKE & IMPACT ===")]
    public float cameraShakeDuration = 0.6f;
    public float cameraShakeMagnitude = 0.5f;

    [Header("=== AUDIO EFFECTS ===")]
    public AudioClip[] footstepSounds;
    public AudioClip nervousBreathingSound;
    public AudioClip jumpscareSound;
    public float footstepInterval = 0.45f;

    [Header("=== STATE ===")]
    public bool isInspecting = false;
    public bool hasTriggeredScare = false;

    [Header("=== REFERENCES ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private float footstepTimer;
    private Coroutine scareCoroutine;
    private Renderer[] normalRenderers;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 35f;
        }

        normalRenderers = GetComponentsInChildren<Renderer>(true);

#if UNITY_EDITOR
        if (jumpscarePrefab == null && jumpscareModel == null)
        {
            jumpscarePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
        }

        if (jumpscareSound == null)
        {
            jumpscareSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Impacts & Hits/JumpScare.wav");
            if (jumpscareSound == null)
            {
                jumpscareSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Blood_Bone_Smash_01.wav");
            }
        }

        if (nervousBreathingSound == null)
        {
            nervousBreathingSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Squishing_Short_01.wav");
        }
#endif
    }

    void Start()
    {
        if (inspectionPoint == null)
        {
            if (NPCInspectionManager.instance != null && NPCInspectionManager.instance.inspectionPoint != null)
            {
                inspectionPoint = NPCInspectionManager.instance.inspectionPoint;
            }
            else
            {
                GameObject zoneObj = FindSceneObject("InspectionZone", "InspectionPoint");
                if (zoneObj != null) inspectionPoint = zoneObj.transform;
            }
        }

        // Auto-detect user created WindowJumpScare (active or inactive)
        if (jumpscareModel == null)
        {
            jumpscareModel = FindSceneObject("windowjumpscare", "WindowJumpScare");
        }

        if (jumpscareSpawnPoint == null)
        {
            if (jumpscareModel != null)
            {
                jumpscareSpawnPoint = jumpscareModel.transform;
            }
            else
            {
                GameObject winObj = FindSceneObject("windowjumpscare", "WindowJumpScare", "BoothWindow", "Glass");
                if (winObj != null) jumpscareSpawnPoint = winObj.transform;
            }
        }

        // Initially deactivate jumpscare model in scene if assigned
        if (jumpscareModel != null && jumpscareModel.scene.IsValid())
        {
            jumpscareModel.SetActive(false);
        }

        if (NPCInspectionManager.instance == null && inspectionPoint != null)
        {
            MoveToInspectionPoint();
        }
    }

    private GameObject FindSceneObject(params string[] possibleNames)
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var targetName in possibleNames)
        {
            string cleanTarget = targetName.Replace(" ", "").Replace("_", "").ToLower();
            foreach (var t in all)
            {
                if (t != null && t.gameObject.scene.IsValid())
                {
                    string cleanObjName = t.name.Replace(" ", "").Replace("_", "").ToLower();
                    if (cleanObjName.Equals(cleanTarget) || cleanObjName.Contains(cleanTarget))
                    {
                        return t.gameObject;
                    }
                }
            }
        }
        return null;
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
        hasTriggeredScare = false;
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
        if (isInspecting) return;
        isInspecting = true;

        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (animator != null) animator.SetBool("isWalk", false);

        if (scareCoroutine != null) StopCoroutine(scareCoroutine);
        scareCoroutine = StartCoroutine(JumpscareModelSequence());
    }

    private IEnumerator JumpscareModelSequence()
    {
        float delay = Random.Range(minTriggerDelay, maxTriggerDelay);

        // Play subtle nervous anticipation audio
        PlaySound(nervousBreathingSound, 0.7f);

        // Subtle anxious jittering before jumpscare
        float elapsed = 0f;
        Vector3 initialPos = transform.position;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            transform.position = initialPos + Random.insideUnitSphere * 0.012f;
            yield return null;
        }
        transform.position = initialPos;

        // --- TRIGGER JUMPSCARE MODEL ---
        hasTriggeredScare = true;

        // Hide normal NPC model if requested
        if (hideNormalModelDuringScare)
        {
            SetRenderersVisibility(false);
        }

        GameObject activeScareInstance = null;

        // 1. If user provided a scene object (e.g. WindowJumpScare), activate it
        if (jumpscareModel != null && jumpscareModel.scene.IsValid())
        {
            jumpscareModel.SetActive(true);
            activeScareInstance = jumpscareModel;
        }
        // 2. Otherwise instantiate prefab (assigned by user or auto-fallback)
        else
        {
            GameObject prefabToSpawn = (jumpscareModel != null && !jumpscareModel.scene.IsValid()) 
                ? jumpscareModel 
                : jumpscarePrefab;

            if (prefabToSpawn != null)
            {
                Camera mainCam = Camera.main;
                Vector3 targetPos;
                Quaternion targetRot;

                if (jumpscareSpawnPoint != null)
                {
                    targetPos = jumpscareSpawnPoint.position;
                    Vector3 lookDir = (mainCam != null) ? (mainCam.transform.position - targetPos) : jumpscareSpawnPoint.forward;
                    lookDir.y = 0f;
                    targetRot = (lookDir.sqrMagnitude > 0.01f) ? Quaternion.LookRotation(lookDir) : jumpscareSpawnPoint.rotation;
                }
                else if (mainCam != null)
                {
                    targetPos = mainCam.transform.position + mainCam.transform.forward * cameraForwardOffset;
                    targetRot = Quaternion.LookRotation(-mainCam.transform.forward);
                }
                else
                {
                    targetPos = transform.position + transform.forward * 2.0f;
                    targetRot = Quaternion.LookRotation(-transform.forward);
                }

                activeScareInstance = Instantiate(prefabToSpawn, targetPos, targetRot);
                activeScareInstance.SetActive(true);
            }
        }

        // Play loud jumpscare sound
        PlaySound(jumpscareSound, 1.0f);

        // Trigger heavy camera shake (with Cinemachine 3.x integration)
        TriggerCameraShake();

        // Keep jumpscare model on screen for duration
        yield return new WaitForSeconds(jumpscareDuration);

        // Deactivate or destroy jumpscare model
        if (activeScareInstance != null)
        {
            if (activeScareInstance == jumpscareModel && jumpscareModel.scene.IsValid())
            {
                activeScareInstance.SetActive(false);
            }
            else
            {
                Destroy(activeScareInstance);
            }
        }

        // Restore normal NPC renderers
        if (hideNormalModelDuringScare)
        {
            SetRenderersVisibility(true);
        }
    }

    private void SetRenderersVisibility(bool visible)
    {
        if (normalRenderers == null || normalRenderers.Length == 0)
        {
            normalRenderers = GetComponentsInChildren<Renderer>(true);
        }

        foreach (var r in normalRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    private void TriggerCameraShake()
    {
        if (CameraShake.instance != null)
        {
            CameraShake.instance.Shake(cameraShakeDuration, cameraShakeMagnitude);
            return;
        }

        CameraShake shaker = FindFirstObjectByType<CameraShake>();
        if (shaker == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) shaker = player.AddComponent<CameraShake>();
            else if (Camera.main != null) shaker = Camera.main.gameObject.AddComponent<CameraShake>();
        }

        if (shaker != null)
        {
            shaker.Shake(cameraShakeDuration, cameraShakeMagnitude);
        }
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

    void OnDisable()
    {
        if (scareCoroutine != null)
        {
            StopCoroutine(scareCoroutine);
            scareCoroutine = null;
        }
        SetRenderersVisibility(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (jumpscareSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(jumpscareSpawnPoint.position, 0.4f);
            Gizmos.DrawLine(transform.position, jumpscareSpawnPoint.position);
        }
    }
}
