using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điều khiển chú chó German Shepherd tuần tra quanh căn cứ và chạy tới sủa khi NPC DogBark đến cổng:
/// - Bình thường: đi/chạy tuần tra qua các điểm Patrol Points quanh căn cứ (hoặc tự đi quanh điểm xuất phát).
/// - Khi NPC DogBark tới vạch kiểm tra: chạy nhanh tới trước mặt NPC, quay mặt vào NPC và sủa cảnh báo.
/// - Sau khi kiểm tra xong: tự quay lại tuần tra quanh căn cứ.
/// - Tự tương thích cả AC_Dogs_Type_01.controller có sẵn lẫn Animator tự tạo (isWalk / isRun / isBark).
/// </summary>
public class DogCompanionController : MonoBehaviour
{
    public static DogCompanionController instance;

    [Header("=== TUẦN TRA QUANH CĂN CỨ (PATROL) ===")]
    [Tooltip("Danh sách các điểm Empty quanh căn cứ để chó chạy qua. Nếu để trống chó sẽ tự chạy loanh quanh trong bán kính Wander Radius")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Tooltip("Bán kính tự do chạy quanh vị trí ban đầu nếu chưa gán Patrol Points")]
    public float wanderRadius = 12f;

    [Tooltip("Tốc độ đi bộ tuần tra (khớp nhịp bước A_Walk để không trượt chân)")]
    public float walkSpeed = 1.6f;

    [Tooltip("Tốc độ chạy nhanh (khớp nhịp chạy A_Run_Loop để không trượt chân)")]
    public float runSpeed = 4.2f;

    [Tooltip("Cho chó thỉnh thoảng chạy nhanh khi tuần tra cho sinh động")]
    public bool runWhilePatrolling = false;

    [Tooltip("Thời gian dừng thở/ngửi tại mỗi điểm tuần tra (giây)")]
    public float minIdleTime = 1.5f;
    public float maxIdleTime = 3.5f;

    [Header("=== VỊ TRÍ ĐỨNG SỦA KHI NPC TỚI ===")]
    [Tooltip("Điểm chó chạy tới đứng sủa trước bốt gác (tùy chọn). Nếu để trống sẽ tự đứng cách NPC ~2.2m")]
    public Transform barkStandPoint;

    [Tooltip("Khoảng cách dừng lại trước mặt NPC để sủa (khi không gán Bark Stand Point)")]
    public float barkStopDistance = 2.2f;

    [Header("=== TÊN STATE ANIMATION (MẶC ĐỊNH THEO AC_Dogs_Type_01) ===")]
    public string idleStateName = "1 type_Idle Breathing";
    public string walkStateName = "1 type_Walk_Loop";
    public string runStateName = "1 type_Run Loop";
    public string barkStateName = "1 type_Idle_Playing";

    [Header("=== ÂM THANH CHÚ CHÓ (KÉO AUDIO CLIP VÀO ĐÂY) ===")]
    [Tooltip("Tiếng bước chân chó khi đi bộ / chạy (có thể kéo nhiều clip để phát ngẫu nhiên)")]
    public AudioClip[] footstepClips;
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.24f;
    [Range(0f, 1f)] public float footstepVolume = 0.45f;

    [Tooltip("Tiếng chó thở hổn hển / khịt mũi khi đứng nghỉ (Idle)")]
    public AudioClip idlePantClip;
    [Range(0f, 1f)] public float idleVolume = 0.5f;

    [Tooltip("Tiếng chó sủa thỉnh thoảng khi đi tuần tra quanh căn cứ")]
    public AudioClip[] patrolBarkClips;
    public bool enableRandomPatrolBark = true;
    public float minPatrolBarkDelay = 8f;
    public float maxPatrolBarkDelay = 18f;

    [Tooltip("Tiếng sủa khi phát hiện NPC thường (nếu để trống sẽ dùng clip mặc định của NPCDogBarkBehavior)")]
    public AudioClip alertBarkClip;

    [Tooltip("Tiếng sủa gắt khi phát hiện Quái Đột Biến (nếu để trống sẽ dùng clip mặc định của NPCDogBarkBehavior)")]
    public AudioClip aggressiveBarkClip;

    [Range(0f, 1f)] public float barkVolume = 1.0f;

    private enum DogState { PatrolMove, PatrolIdle, RushToNPC, Barking }
    private DogState currentState = DogState.PatrolIdle;

    private Animator animator;
    private NavMeshAgent agent;
    private AudioSource audioSource;

    private Vector3 homePosition;
    private Vector3 currentMoveTarget;
    private int patrolIndex = 0;
    private float idleTimer = 0f;
    private float footstepTimer = 0f;
    private float idleSoundTimer = 3f;
    private float patrolBarkTimer = 10f;
    private string activeAnimState = "";
    private string desiredAnimState = "";
    private bool wantWalk = false;
    private bool wantRun = false;
    private bool wantBark = false;
    private float animTransitionWaitTimer = 0f;

    private bool hasWalkParam = false;
    private bool hasRunParam = false;
    private bool hasBarkParam = false;

    private Transform barkTargetNPC;
    private Coroutine barkAlertRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;

        homePosition = transform.position;

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            CacheAnimatorParameters();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1.8f;
        audioSource.maxDistance = 26f;
        audioSource.dopplerLevel = 0f;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 0.9f;
            agent.stoppingDistance = 0.4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 10f;
        }

        // Kiểm tra xem Scene đã Bake NavMesh chưa
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            agent.enabled = false;
        }
    }

    void Start()
    {
        patrolBarkTimer = Random.Range(minPatrolBarkDelay, maxPatrolBarkDelay);
        PickNextPatrolPoint();
    }

    void Update()
    {
        UpdateAnimationMachine();
        UpdateDogSounds();

        switch (currentState)
        {
            case DogState.PatrolMove:
                UpdatePatrolMove();
                break;

            case DogState.PatrolIdle:
                UpdatePatrolIdle();
                break;

            case DogState.RushToNPC:
                UpdateRushToNPC();
                break;

            case DogState.Barking:
                UpdateBarkingLookAt();
                break;
        }
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Bool) continue;
            if (p.name == "isWalk") hasWalkParam = true;
            else if (p.name == "isRun") hasRunParam = true;
            else if (p.name == "isBark") hasBarkParam = true;
        }
    }

    private void PlayDogAnim(bool walking, bool running, bool barking)
    {
        wantWalk = walking;
        wantRun = running;
        wantBark = barking;

        if (animator == null) return;

        string targetState = idleStateName;
        if (barking) targetState = barkStateName;
        else if (running) targetState = runStateName;
        else if (walking) targetState = walkStateName;

        if (desiredAnimState != targetState)
        {
            desiredAnimState = targetState;
            animTransitionWaitTimer = 0f;
        }

        // Lần đầu tiên khi Start chưa có activeAnimState -> phát ngay
        if (string.IsNullOrEmpty(activeAnimState))
        {
            ApplyAnimImmediately(targetState);
        }
    }

    private bool IsMovementAnimPlaying()
    {
        if (animator == null) return true;
        if (hasWalkParam || hasRunParam || hasBarkParam)
        {
            return wantWalk || wantRun;
        }
        return activeAnimState == walkStateName || activeAnimState == runStateName;
    }

    private void ApplyAnimImmediately(string targetState)
    {
        activeAnimState = targetState;

        if (hasWalkParam || hasRunParam || hasBarkParam)
        {
            if (hasWalkParam) animator.SetBool("isWalk", wantWalk);
            if (hasRunParam) animator.SetBool("isRun", wantRun);
            if (hasBarkParam) animator.SetBool("isBark", wantBark);
            return;
        }

        if (animator.HasState(0, Animator.StringToHash(targetState)))
        {
            animator.CrossFadeInFixedTime(targetState, 0.15f, 0, 0f);
        }
    }

    /// <summary>
    /// Chờ chạy xong vòng animation hiện tại (>= 88%) rồi mới chuyển mượt sang animation tiếp theo,
    /// đồng thời giữ vòng lặp ổn định để AC_Dogs_Type_01 không tự nhảy sang clip demo gây trượt chân.
    /// </summary>
    private void UpdateAnimationMachine()
    {
        if (animator == null || string.IsNullOrEmpty(desiredAnimState)) return;
        if (animator.IsInTransition(0)) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        float cycleProgress = info.normalizedTime - Mathf.Floor(info.normalizedTime);

        // Nếu dùng Animator tự tạo có tham số Bool (isWalk/isRun/isBark)
        if (hasWalkParam || hasRunParam || hasBarkParam)
        {
            if (desiredAnimState != activeAnimState)
            {
                animTransitionWaitTimer += Time.deltaTime;
                if (cycleProgress >= 0.85f || info.normalizedTime >= 0.85f || animTransitionWaitTimer >= 1.2f)
                {
                    ApplyAnimImmediately(desiredAnimState);
                    animTransitionWaitTimer = 0f;
                }
            }
            return;
        }

        // Nếu dùng AC_Dogs_Type_01.controller có sẵn:
        int activeHash = Animator.StringToHash(activeAnimState);
        int desiredHash = Animator.StringToHash(desiredAnimState);

        // 1. Đang chờ chuyển sang animation mới -> đợi chạy xong vòng hiện tại rồi mới chuyển
        if (desiredAnimState != activeAnimState)
        {
            animTransitionWaitTimer += Time.deltaTime;
            bool finishedCycle = (cycleProgress >= 0.88f) || (info.normalizedTime >= 0.92f) || (animTransitionWaitTimer >= 1.25f);
            if (finishedCycle)
            {
                ApplyAnimImmediately(desiredAnimState);
                animTransitionWaitTimer = 0f;
            }
            return;
        }

        // 2. Đang giữ cùng 1 animation: ngăn AC_Dogs_Type_01 tự nhảy sang các state demo khác gây trượt chân
        if (info.shortNameHash != desiredHash)
        {
            if (animator.HasState(0, desiredHash))
            {
                animator.CrossFadeInFixedTime(desiredAnimState, 0.12f, 0, 0f);
            }
        }
        else if (info.normalizedTime >= 0.90f)
        {
            // Tự lặp mượt lại chính animation đang chạy trước khi Exit Time của AC_Dogs_Type_01 nhảy sang clip khác
            animator.CrossFadeInFixedTime(desiredAnimState, 0.10f, 0, 0f);
        }
    }

    private void PickNextPatrolPoint()
    {
        Vector3 nextPos = homePosition;

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            // Lọc các điểm hợp lệ
            int attempts = 0;
            while (attempts < patrolPoints.Count)
            {
                Transform pt = patrolPoints[patrolIndex % patrolPoints.Count];
                patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
                if (pt != null)
                {
                    nextPos = pt.position;
                    break;
                }
                attempts++;
            }
        }
        else
        {
            // Tự chọn điểm ngẫu nhiên quanh căn cứ
            Vector2 rnd = Random.insideUnitCircle * wanderRadius;
            nextPos = homePosition + new Vector3(rnd.x, 0f, rnd.y);
            if (agent != null && agent.enabled && NavMesh.SamplePosition(nextPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                nextPos = hit.position;
            }
        }

        currentMoveTarget = nextPos;
        currentState = DogState.PatrolMove;

        float speed = runWhilePatrolling ? runSpeed * 0.75f : walkSpeed;
        SetDestinationOrDirect(currentMoveTarget, speed);
        PlayDogAnim(!runWhilePatrolling, runWhilePatrolling, false);
    }

    private void UpdatePatrolMove()
    {
        float speed = runWhilePatrolling ? runSpeed * 0.75f : walkSpeed;
        if (MoveTowardsTarget(currentMoveTarget, speed, 0.65f))
        {
            currentState = DogState.PatrolIdle;
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
            StopMovement();
            PlayDogAnim(false, false, false);
        }
    }

    private void UpdatePatrolIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            PickNextPatrolPoint();
        }
    }

    /// <summary>
    /// Được NPCDogBarkBehavior gọi khi NPC DogBark tới trước bốt gác:
    /// Chó sẽ lập tức chạy nhanh tới trước mặt NPC và sủa đủ số tiếng cảnh báo.
    /// </summary>
    public void CallToBarkAt(Transform npcTransform, int barkCount, bool isMutant, AudioClip normalClip, AudioClip aggressiveClip, float barkInterval, System.Action onBarkFinished = null)
    {
        if (barkAlertRoutine != null) StopCoroutine(barkAlertRoutine);
        barkTargetNPC = npcTransform;
        barkAlertRoutine = StartCoroutine(RushAndBarkRoutine(npcTransform, barkCount, isMutant, normalClip, aggressiveClip, barkInterval, onBarkFinished));
    }

    public void ResumePatrol()
    {
        if (barkAlertRoutine != null)
        {
            StopCoroutine(barkAlertRoutine);
            barkAlertRoutine = null;
        }
        barkTargetNPC = null;
        PickNextPatrolPoint();
    }

    private Vector3 GetBarkDestination(Transform npcTransform)
    {
        if (barkStandPoint != null) return barkStandPoint.position;
        if (npcTransform == null) return homePosition;

        Vector3 dirFromNPC = (transform.position - npcTransform.position);
        dirFromNPC.y = 0f;
        if (dirFromNPC.sqrMagnitude < 0.01f) dirFromNPC = npcTransform.forward;
        Vector3 desired = npcTransform.position + dirFromNPC.normalized * barkStopDistance;

        if (agent != null && agent.enabled && NavMesh.SamplePosition(desired, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return desired;
    }

    private IEnumerator RushAndBarkRoutine(Transform npcTransform, int barkCount, bool isMutant, AudioClip normalClip, AudioClip aggressiveClip, float barkInterval, System.Action onBarkFinished)
    {
        // 1. Chạy nhanh tới chỗ NPC
        currentState = DogState.RushToNPC;
        currentMoveTarget = GetBarkDestination(npcTransform);
        SetDestinationOrDirect(currentMoveTarget, runSpeed);
        PlayDogAnim(false, true, false);

        float timeout = 12f;
        while (timeout > 0f && npcTransform != null)
        {
            currentMoveTarget = GetBarkDestination(npcTransform);
            if (MoveTowardsTarget(currentMoveTarget, runSpeed, 0.75f))
            {
                break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 2. Dừng lại, quay mặt về phía NPC và sủa
        currentState = DogState.Barking;
        StopMovement();
        PlayDogAnim(false, false, true);

        for (int i = 0; i < barkCount; i++)
        {
            if (npcTransform == null) break;

            AudioClip effectiveNormal = alertBarkClip != null ? alertBarkClip : normalClip;
            AudioClip effectiveAggressive = aggressiveBarkClip != null ? aggressiveBarkClip : aggressiveClip;

            AudioClip clip = isMutant
                ? (effectiveAggressive != null ? effectiveAggressive : effectiveNormal)
                : (effectiveNormal != null ? effectiveNormal : effectiveAggressive);

            if (clip != null && audioSource != null)
            {
                audioSource.pitch = isMutant ? Random.Range(1.05f, 1.25f) : Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip, barkVolume);
            }

            yield return new WaitForSeconds(barkInterval);
        }

        onBarkFinished?.Invoke();

        // Đứng gầm gừ/canh chừng NPC thêm cho đến khi NPC rời đi hoặc bị hủy
        float guardWait = 0f;
        while (npcTransform != null && guardWait < 12f)
        {
            PlayDogAnim(false, false, false);
            guardWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // Quay lại tuần tra quanh căn cứ
        barkTargetNPC = null;
        barkAlertRoutine = null;
        PickNextPatrolPoint();
    }

    private void UpdateDogSounds()
    {
        if (audioSource == null) return;

        // 1. Tiếng bước chân khi chó đang thực sự di chuyển (chỉ nghe khi ở gần Player <= 11m)
        bool isMovingState = (currentState == DogState.PatrolMove || currentState == DogState.RushToNPC);
        if (isMovingState && IsMovementAnimPlaying() && footstepClips != null && footstepClips.Length > 0)
        {
            bool isRunning = (currentState == DogState.RushToNPC) || (currentState == DogState.PatrolMove && runWhilePatrolling);
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                float distToPlayer = GetDistanceToPlayer();
                if (distToPlayer < 11f)
                {
                    AudioClip stepClip = footstepClips[Random.Range(0, footstepClips.Length)];
                    if (stepClip != null)
                    {
                        float t = 1f - Mathf.InverseLerp(1.5f, 11f, distToPlayer);
                        audioSource.pitch = Random.Range(0.92f, 1.08f);
                        audioSource.PlayOneShot(stepClip, footstepVolume * t * t);
                    }
                }
                footstepTimer = isRunning ? runStepInterval : walkStepInterval;
            }
        }
        else
        {
            footstepTimer = 0.08f;
        }

        // 2. Tiếng thở / khịt mũi lúc đứng nghỉ Idle (chỉ nghe khi ở gần <= 9m)
        if (currentState == DogState.PatrolIdle && idlePantClip != null)
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0f)
            {
                float distToPlayer = GetDistanceToPlayer();
                if (distToPlayer < 9f)
                {
                    float t = 1f - Mathf.InverseLerp(1.5f, 9f, distToPlayer);
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(idlePantClip, idleVolume * t);
                }
                idleSoundTimer = Random.Range(3.5f, 6.5f);
            }
        }

        // 3. Tiếng sủa thỉnh thoảng khi tuần tra quanh căn cứ
        if (enableRandomPatrolBark &&
            (currentState == DogState.PatrolMove || currentState == DogState.PatrolIdle) &&
            patrolBarkClips != null && patrolBarkClips.Length > 0)
        {
            patrolBarkTimer -= Time.deltaTime;
            if (patrolBarkTimer <= 0f)
            {
                AudioClip bark = patrolBarkClips[Random.Range(0, patrolBarkClips.Length)];
                if (bark != null)
                {
                    audioSource.pitch = Random.Range(0.92f, 1.08f);
                    audioSource.PlayOneShot(bark, barkVolume * 0.75f);
                }
                patrolBarkTimer = Random.Range(minPatrolBarkDelay, maxPatrolBarkDelay);
            }
        }
    }

    private float GetDistanceToPlayer()
    {
        Transform listener = Camera.main != null ? Camera.main.transform : null;
        if (listener == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) listener = p.transform;
        }
        return listener != null ? Vector3.Distance(transform.position, listener.position) : 0f;
    }

    private void UpdateRushToNPC()
    {
        // Di chuyển được điều phối trong RushAndBarkRoutine
    }

    private void UpdateBarkingLookAt()
    {
        if (barkTargetNPC == null) return;
        Vector3 dir = barkTargetNPC.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
        }
    }

    private void SetDestinationOrDirect(Vector3 target, float speed)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(target);
        }
    }

    private void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private bool MoveTowardsTarget(Vector3 target, float speed, float arriveDist)
    {
        // Đang chờ chạy xong animation Idle trước khi bước đi -> giữ đứng yên để không bị trượt chân
        if (!IsMovementAnimPlaying())
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            return false;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            if (!agent.pathPending)
            {
                float flatDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.x, target.z));
                if (flatDist <= arriveDist || (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.25f))
                {
                    return true;
                }
            }
            return false;
        }

        // Chế độ đi bộ bám mặt đất khi chưa Bake NavMesh
        Vector3 curr = transform.position;
        Vector3 flatCurr = new Vector3(curr.x, 0f, curr.z);
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        if (Vector3.Distance(flatCurr, flatTarget) <= arriveDist)
        {
            return true;
        }

        Vector3 dir = (flatTarget - flatCurr).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
        }

        Vector3 nextXZ = Vector3.MoveTowards(flatCurr, flatTarget, speed * Time.deltaTime);
        Vector3 snapped = SnapToGround(new Vector3(nextXZ.x, curr.y, nextXZ.z));
        transform.position = snapped;
        return false;
    }

    private Vector3 SnapToGround(Vector3 pos)
    {
        Ray ray = new Ray(pos + Vector3.up * 2.5f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        float bestY = pos.y;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (c.CompareTag("Player") || c.CompareTag("NPC")) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                bestY = hits[i].point.y;
            }
        }
        return new Vector3(pos.x, bestY, pos.z);
    }
}
