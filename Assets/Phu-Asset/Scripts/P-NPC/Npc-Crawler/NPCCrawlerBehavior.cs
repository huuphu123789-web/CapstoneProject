using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Crawler Mutant:
/// 1. Walks normally to the window desk outside the guard booth.
/// 2. Speaks dialogue and converses with the player outside for a few seconds.
/// 3. Suddenly vanishes into thin air from outside the window!
/// 4. Waits patiently inside the booth behind the player until the player ACTUALLY turns around
///    (facing into the room, angle > 100 degrees from window).
/// 5. The instant the player turns around, a horrifying jumpscare model (auto-detected 'jumpscarebehind')
///    launches airborne and flies directly into the camera screen with zero chance to dodge!
/// 6. Plays deafening jumpscare screech, shakes the screen violently, thrashes on camera,
///    and then vanishes completely, advancing the inspection queue.
/// </summary>
public class NPCCrawlerBehavior : MonoBehaviour
{
    [Header("=== MOVEMENT CONFIGURATION ===")]
    public Transform inspectionPoint;
    public float walkSpeed = 2.4f;

    [Header("=== DIALOGUE & VANISH TIMING ===")]
    [UnityEngine.Serialization.FormerlySerializedAs("deskWaitBeforeVanish")]
    [Tooltip("How long Crawler talks outside the window before vanishing (seconds)")]
    public float talkDurationBeforeVanish = 5.0f;
    public float deskWaitBeforeVanish { get => talkDurationBeforeVanish; set => talkDurationBeforeVanish = value; }

    [Header("=== JUMPSCARE MODEL & BEHIND SETTINGS ===")]
    [Tooltip("Drag the jumpscare model (e.g. jumpscarebehind) here. Auto-detects in scene if null.")]
    public GameObject jumpscareModel;

    [Tooltip("Point where the jumpscare model begins its flight. Auto-detects in scene if null.")]
    public Transform customBehindPoint;

    [Tooltip("Fallback distance behind the player inside the booth (meters) if no point is set")]
    public float behindDistance = 1.4f;
    public float preferredBehindDistance { get => behindDistance; set => behindDistance = value; }

    [Header("=== TURN-AROUND DETECTION ===")]
    [Tooltip("Angle from the window desk required to count as turning around (default 100 degrees)")]
    public float turnAroundAngleThreshold = 100f;

    [Tooltip("Max seconds to wait before prompting audio behind player")]
    public float audioReminderInterval = 4.0f;

    [Header("=== AIRBORNE FACE LUNGE SETTINGS ===")]
    [Tooltip("Duration of the flight straight into player camera (seconds)")]
    public float flyDuration = 0.20f;

    [Tooltip("Upward arc height during the leap (meters)")]
    public float leapHeightArc = 0.25f;

    [Tooltip("Distance from camera lens when latched on (meters)")]
    public float faceLatchDistance = 0.25f;

    [Tooltip("Duration thrashing on player screen before vanishing (seconds)")]
    public float faceScareDuration = 0.95f;

    [Header("=== CAMERA SHAKE & IMPACT ===")]
    public float cameraShakeDuration = 0.7f;
    public float cameraShakeMagnitude = 0.55f;

    [Header("=== AUDIO EFFECTS ===")]
    public AudioClip[] footstepSounds;
    public AudioClip vanishSound;
    public AudioClip behindWhisperSound;
    public AudioClip jumpscareRoarSound;
    public float footstepInterval = 0.45f;

    [Header("=== INSPECTION PROGRESSION ===")]
    [Tooltip("Automatically advance inspection queue after Crawler scares and vanishes")]
    public bool advanceInspectionQueue = true;

    [Header("=== STATE ===")]
    public bool isInspecting = false;
    public bool hasVanishedFromWindow = false;
    public bool isWaitingBehindPlayer = false;
    public bool hasAttackedPlayer = false;

    [Header("=== REFERENCES ===")]
    public Animator animator;
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private float footstepTimer;
    private Renderer[] allRenderers;
    private Collider rootCollider;
    private Coroutine ambushCoroutine;

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

        allRenderers = GetComponentsInChildren<Renderer>(true);
        rootCollider = GetComponent<Collider>();
        if (rootCollider == null) rootCollider = GetComponentInChildren<Collider>();

#if UNITY_EDITOR
        if (vanishSound == null)
            vanishSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Squishing_Short_01.wav");

        if (behindWhisperSound == null)
            behindWhisperSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Drn_Chrom_01.wav");

        if (jumpscareRoarSound == null)
        {
            jumpscareRoarSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Impacts & Hits/JumpScare.wav");
            if (jumpscareRoarSound == null)
            {
                jumpscareRoarSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phu-Asset/Audio/Horror Sound FX/Dark/Breaking_Bones_01.wav");
            }
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

        // Auto-detect user created jumpscarebehind (whether active or inactive)
        if (jumpscareModel == null)
        {
            jumpscareModel = FindSceneObject("jumpscarebehind", "JumpScareBehind", "BehindJumpScare", "JumpscareBehind");
        }

        if (customBehindPoint == null)
        {
            if (jumpscareModel != null)
            {
                customBehindPoint = jumpscareModel.transform;
            }
            else
            {
                GameObject behindObj = FindSceneObject("jumpscarebehind", "JumpScareBehind", "BehindJumpScare", "JumpscareBehind");
                if (behindObj != null) customBehindPoint = behindObj.transform;
            }
        }

        // Initially deactivate jumpscare model in scene if assigned
        if (jumpscareModel != null && jumpscareModel.scene.IsValid() && jumpscareModel != gameObject)
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
        hasVanishedFromWindow = false;
        isWaitingBehindPlayer = false;
        hasAttackedPlayer = false;
        SetModelVisibility(true);

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

        if (ambushCoroutine != null) StopCoroutine(ambushCoroutine);
        ambushCoroutine = StartCoroutine(CrawlerBehindAmbushSequence());
    }


    private void GetPlayerCameraOrHead(out Vector3 eyePos, out Vector3 forwardDir)
    {
        // 1. Locate the active Player GameObject in the scene
        GameObject playerObj = null;
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) playerObj = pc.gameObject;

        if (playerObj == null)
        {
            GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in taggedPlayers)
            {
                if (p.GetComponent<CharacterController>() != null || p.GetComponent<PlayerController>() != null)
                {
                    playerObj = p;
                    break;
                }
            }
            if (playerObj == null && taggedPlayers.Length > 0) playerObj = taggedPlayers[0];
        }

        // 2. Camera reference in scene
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();

        // 3. If Player is found in booth:
        if (playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;

            // If mainCam is within 3.5 meters of Player, it is correctly following the player!
            if (mainCam != null && Vector3.Distance(mainCam.transform.position, playerPos) < 3.5f)
            {
                eyePos = mainCam.transform.position;
                forwardDir = mainCam.transform.forward;
                return;
            }

            // Otherwise, mainCam was NOT updated by Cinemachine yet (still at old spawn house)!
            // Retrieve actual head/eye position directly from Player's HeadTarget, CM_FirstPreson, or camera
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

        // 4. Fallback to mainCam if no Player object
        if (mainCam != null)
        {
            eyePos = mainCam.transform.position;
            forwardDir = mainCam.transform.forward;
            return;
        }

        // 5. Default booth position fallback
        eyePos = new Vector3(-48.63f, 1.6f, -50.45f);
        forwardDir = Vector3.forward;
    }

    private Vector3 GetBehindJumpScarePosition(out Quaternion rotation)
    {
        // 1. If assigned in inspector and is a valid scene object
        if (customBehindPoint != null && customBehindPoint.gameObject.scene.IsValid())
        {
            rotation = customBehindPoint.rotation;
            return customBehindPoint.position;
        }

        // 2. Search scene for JumpscareBehiand (the point created by user in Map inside booth)
        GameObject sceneBehind = FindSceneObject("JumpscareBehiand", "jumpscarebehind", "BehindJumpScare", "JumpscareBehind");
        if (sceneBehind != null)
        {
            customBehindPoint = sceneBehind.transform;
            rotation = sceneBehind.transform.rotation;
            return sceneBehind.transform.position;
        }

        // 3. Fallback to exact user coordinates of JumpscareBehiand in booth: (-49.32, -1.69, -51.0)
        rotation = Quaternion.Euler(0f, 45f, 0f);
        return new Vector3(-49.32f, -1.69f, -51.0f);
    }

    private IEnumerator CrawlerBehindAmbushSequence()
    {
        Debug.Log($"[NPCCrawlerBehavior] Arrived at window desk. Talking with officer for {talkDurationBeforeVanish}s...");

        // 1. TALK AT WINDOW: Give player time to hear dialogue and inspect papers
        yield return new WaitForSeconds(talkDurationBeforeVanish);

        // 2. DISAPPEAR from outside window
        hasVanishedFromWindow = true;
        PlaySound(vanishSound, 0.9f);
        SetModelVisibility(false);

        if (agent != null) agent.enabled = false;
        if (rootCollider != null) rootCollider.enabled = false;

        yield return new WaitForSeconds(0.2f);

        // 3. PREPARE JUMPSCARE MODEL EXACTLY AT USER'S 'JumpscareBehiand' POINT
        Vector3 deskPos = (inspectionPoint != null) ? inspectionPoint.position : new Vector3(-46.32f, -3.22f, -43.68f);

        // Resolve Jumpscare Model (Support Scene Objects AND Project Prefabs)
        GameObject scarePrefabOrSceneObj = jumpscareModel;
        if (scarePrefabOrSceneObj == null)
        {
            scarePrefabOrSceneObj = FindSceneObject("JumpScare", "jumpscarebehind", "JumpscareBehiand", "BehindJumpScare");
#if UNITY_EDITOR
            if (scarePrefabOrSceneObj == null)
            {
                scarePrefabOrSceneObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
            }
#endif
        }

        // Get exact position of user's behind jumpscare point inside booth
        Vector3 behindPos = GetBehindJumpScarePosition(out Quaternion behindRot);

        GetPlayerCameraOrHead(out Vector3 playerEyePos, out Vector3 playerFwd);

        // Face the player inside the booth
        Vector3 toPlayer = (playerEyePos - behindPos);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            behindRot = Quaternion.LookRotation(toPlayer.normalized);
        }

        // Instantiate or activate Jumpscare Model STRICTLY at behind jumpscare point
        GameObject activeScareObject = null;
        if (scarePrefabOrSceneObj != null)
        {
            if (scarePrefabOrSceneObj.scene.IsValid())
            {
                scarePrefabOrSceneObj.transform.position = behindPos;
                scarePrefabOrSceneObj.transform.rotation = behindRot;
                scarePrefabOrSceneObj.SetActive(true);
                activeScareObject = scarePrefabOrSceneObj;
            }
            else
            {
                activeScareObject = Instantiate(scarePrefabOrSceneObj, behindPos, behindRot);
                activeScareObject.SetActive(true);
            }
        }

        // Keep normal NPC mutant body completely invisible
        SetModelVisibility(false);
        transform.position = behindPos; // Move AudioSource to behind point for whisper sound

        isWaitingBehindPlayer = true;
        PlaySound(behindWhisperSound, 0.85f);
        Debug.Log($"[NPCCrawlerBehavior] Vanished from window! Jumpscare model is waiting at behind point {behindPos}. WAITING FOR TURN-AROUND...");

        // 4. WAIT UNTIL PLAYER ACTUALLY TURNS AROUND TOWARDS THE BEHIND JUMPSCARE POINT
        bool playerTurnedAround = false;
        float reminderTimer = 0f;

        while (!playerTurnedAround)
        {
            GetPlayerCameraOrHead(out Vector3 currentEyePos, out Vector3 currentFwd);

            Vector3 toMonster = (behindPos - currentEyePos);
            toMonster.y = 0f;
            toMonster.Normalize();

            Vector3 currentToDesk = (deskPos - currentEyePos);
            currentToDesk.y = 0f;
            currentToDesk.Normalize();

            Vector3 fwdFlat = currentFwd;
            fwdFlat.y = 0f;
            fwdFlat.Normalize();

            float angleToMonster = Vector3.Angle(fwdFlat, toMonster);
            float angleFromDesk = Vector3.Angle(fwdFlat, currentToDesk);

            // Player turns around when looking towards behind point or away from window
            if (angleToMonster <= 75f || angleFromDesk >= 90f)
            {
                playerTurnedAround = true;
                Debug.Log($"[NPCCrawlerBehavior] Player turned around towards jumpscare! (AngleToMonster: {angleToMonster:F1} deg). LAUNCHING DIRECTLY AT CAMERA!");
                break;
            }

            // Periodic audio whispers behind the player
            reminderTimer += Time.deltaTime;
            if (reminderTimer >= audioReminderInterval)
            {
                reminderTimer = 0f;
                PlaySound(behindWhisperSound, 0.9f);
            }

            yield return null;
        }

        // 5. FLY FROM 'JumpscareBehiand' DIRECTLY AT PLAYER'S EYES / CAMERA
        yield return StartCoroutine(FlyAndLatchOnPlayerFace(activeScareObject, behindPos));

        // 6. VANISH COMPLETELY
        if (activeScareObject != null)
        {
            if (activeScareObject.scene.IsValid() && activeScareObject != scarePrefabOrSceneObj)
            {
                Destroy(activeScareObject);
            }
            else
            {
                activeScareObject.SetActive(false);
            }
        }
        SetModelVisibility(false);
        Debug.Log("[NPCCrawlerBehavior] Jumpscare completed. Vanished completely!");

        if (advanceInspectionQueue && NPCInspectionManager.instance != null)
        {
            if (InspectionDeskInteractable.instance != null)
            {
                InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
            }

            if (!NPCInspectionManager.instance.hasDecisionBeenMade)
            {
                NPCInspectionManager.instance.RejectCurrentNPC();
            }
        }

        Destroy(gameObject, 0.2f);
    }

    private static Transform FindDeepChild(Transform parent, string name)
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

    private IEnumerator FlyAndLatchOnPlayerFace(GameObject scareObject, Vector3 startFlyPos)
    {
        hasAttackedPlayer = true;
        isWaitingBehindPlayer = false;

        if (scareObject == null) yield break;

        scareObject.SetActive(true);
        Transform scareTransform = scareObject.transform;

        // Ensure start position is STRICTLY the behind point
        scareTransform.position = startFlyPos;

        // Trigger jumpscare animation
        Animator anim = scareObject.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play(0, 0, 0f);
        }

        // Disable player movement temporarily during jumpscare so player cannot dodge
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        bool wasPcEnabled = (pc != null && pc.enabled);
        if (pc != null) pc.enabled = false;

        // Find face/head transform on the monster (HeadTarget, frontface, head)
        Transform monsterFace = FindDeepChild(scareTransform, "HeadTarget");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "frontface");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "head");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "Head");

        // Play loud jumpscare screech
        PlaySound(jumpscareRoarSound, 1.0f);

        // Precompute initial head position
        Vector3 initialHeadOffset = (monsterFace != null)
            ? (monsterFace.position - scareTransform.position)
            : (scareTransform.up * 1.5f);
        Vector3 startHeadPos = startFlyPos + initialHeadOffset;

        float elapsed = 0f;

        // HIGH-SPEED AIRBORNE FLIGHT STRAIGHT INTO PLAYER'S CAMERA LENS
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);

            GetPlayerCameraOrHead(out Vector3 eyePos, out Vector3 camFwd);

            // Target position: right in front of camera lens
            Vector3 targetFacePos = eyePos + camFwd * faceLatchDistance;

            // Straight direct flight into the camera
            Vector3 currentFacePos = Vector3.Lerp(startHeadPos, targetFacePos, t);

            // Orient monster facing DIRECTLY into camera lens
            Vector3 toCamera = (eyePos - currentFacePos);
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                scareTransform.rotation = Quaternion.LookRotation(toCamera.normalized);
            }

            // Align root so the FACE hits exactly at currentFacePos
            Vector3 headOffset = (monsterFace != null)
                ? (monsterFace.position - scareTransform.position)
                : (scareTransform.up * 1.5f);
            scareTransform.position = currentFacePos - headOffset;

            yield return null;
        }

        // IMPACT: Trigger violent Cinemachine screen shake
        TriggerCameraShake();

        // LATCH ON SCREEN: Thrashing violently right in front of player's face
        float scareElapsed = 0f;
        while (scareElapsed < faceScareDuration)
        {
            scareElapsed += Time.deltaTime;

            GetPlayerCameraOrHead(out Vector3 eyePos, out Vector3 camFwd);

            Vector3 jitter = Random.insideUnitSphere * 0.02f;
            Vector3 targetFacePos = eyePos + camFwd * faceLatchDistance + jitter;

            scareTransform.rotation = Quaternion.LookRotation(-camFwd);

            Vector3 headOffset = (monsterFace != null)
                ? (monsterFace.position - scareTransform.position)
                : (scareTransform.up * 1.5f);
            scareTransform.position = targetFacePos - headOffset;

            yield return null;
        }

        if (pc != null && wasPcEnabled) pc.enabled = true;
    }


    private void SetModelVisibility(bool visible)
    {
        if (allRenderers == null || allRenderers.Length == 0)
        {
            allRenderers = GetComponentsInChildren<Renderer>(true);
        }

        foreach (var r in allRenderers)
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
        if (ambushCoroutine != null)
        {
            StopCoroutine(ambushCoroutine);
            ambushCoroutine = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        if (customBehindPoint != null)
        {
            Gizmos.DrawWireSphere(customBehindPoint.position, 0.4f);
        }
    }
}
