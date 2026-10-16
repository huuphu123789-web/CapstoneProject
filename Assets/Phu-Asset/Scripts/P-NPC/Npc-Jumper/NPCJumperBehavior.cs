using System.Collections;
using UnityEngine;

/// <summary>
/// Hành vi NPC Jumper (The Jumper Mutant):
/// Kế thừa từ NPCBaseBehavior.
/// - Khi NPC Jumper đến cửa sổ bốt gác để kiểm tra:
/// - Sau 1 khoảng thời gian ngắn (1.0 - 1.8s) hoặc khi người chơi bấm nút duyệt/từ chối:
///   Quái vật sẽ biến hình và PHÓNG THẲNG TỪ CỬA SỔ với tốc độ cực cao phi thẳng vào mặt/camera của người chơi!
/// - Đích đến là điểm 'PlayerCameraJumpscarePoint' gắn trên Camera của Player.
/// - Phát âm thanh jumpscare dữ dội, rung giật màn hình điên cuồng, cào cấu ngay trước mắt rồi biến mất!
/// </summary>
public class NPCJumperBehavior : NPCBaseBehavior
{
    [Header("=== JUMPSCARE TRIGGER TIMING ===")]
    [Tooltip("Khoảng thời gian chờ ngắn trước khi quái vật nhảy bổ vào camera (giây)")]
    public float minTriggerDelay = 1.0f;
    public float maxTriggerDelay = 1.8f;

    [Header("=== JUMPSCARE MODEL SETTINGS ===")]
    [Tooltip("Kéo đối tượng GameObject có Mesh/Renderer vào đây (hoặc để trống để tự dùng JumpScare.prefab)")]
    public GameObject jumpscareModel;

    [Tooltip("Prefab mô hình quái vật jumpscare (mặc định: JumpScare.prefab)")]
    public GameObject jumpscarePrefab;

    [Tooltip("Điểm xuất phát cú nhảy. Nếu để trống, sẽ nhảy trực tiếp từ vị trí đứng trước cửa sổ của NPC.")]
    public Transform jumpscareSpawnPoint;

    [Tooltip("Tạm thời ẩn mô hình NPC bình thường trong khi mô hình jumpscare đang nhảy vồ")]
    public bool hideNormalModelDuringScare = true;

    [Header("=== THÔNG SỐ BAY & VỒ MẶT (AIRBORNE LUNGE) ===")]
    [Tooltip("Thời gian bay thẳng từ cửa sổ vào ống kính camera của người chơi (giây)")]
    public float flyDuration = 0.22f;

    [Tooltip("Độ cong vòng cung nhào lộn qua bệ cửa sổ (mét)")]
    public float leapHeightArc = 0.35f;

    [Tooltip("Khoảng cách bám mặt sát ống kính camera (mét)")]
    public float faceLatchDistance = 0.35f;

    [Tooltip("Thời gian cào cấu / gầm thét rung giật ngay trước mắt người chơi (giây)")]
    public float faceScareDuration = 1.0f;

    [Header("=== RUNG LẮC CAMERA & VA CHẠM ===")]
    public float cameraShakeDuration = 0.7f;
    public float cameraShakeMagnitude = 0.6f;

    [Header("=== HIỆU ỨNG ÂM THANH ===")]
    public AudioClip nervousBreathingSound;
    public AudioClip jumpscareSound;

    [Header("=== TIẾN TRÌNH KIỂM TRA ===")]
    [Tooltip("Tự động từ chối/bỏ chạy và chuyển lượt kiểm tra tiếp theo sau khi jumpscare")]
    public bool advanceInspectionQueue = true;

    [Header("=== TRẠNG THÁI ===")]
    public bool hasTriggeredScare = false;
    public bool isAttackingPlayer = false;

    private Coroutine scareCoroutine;
    private Renderer[] normalRenderers;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 35f;
        }

        normalRenderers = GetComponentsInChildren<Renderer>(true);

#if UNITY_EDITOR
        if (jumpscarePrefab == null)
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

    protected override void Start()
    {
        base.Start();

        // 1. Kiểm tra jumpscareModel: Phải có Renderer/Mesh thực tế, tránh các GameObject rỗng
        if (jumpscareModel != null && !IsValidVisualObject(jumpscareModel))
        {
            Debug.LogWarning($"[NPCJumperBehavior] '{jumpscareModel.name}' không có Renderer/Mesh. Chuyển sang dùng JumpScare.prefab.");
            jumpscareModel = null;
        }

        // 2. Tải JumpScare.prefab nếu chưa có model
        if (jumpscareModel == null && jumpscarePrefab == null)
        {
#if UNITY_EDITOR
            jumpscarePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
#endif
        }

        // 3. Nếu jumpscareSpawnPoint trỏ vào asset prefab không nằm trong Scene, reset về null để dùng vị trí NPC
        if (jumpscareSpawnPoint != null && (!jumpscareSpawnPoint.gameObject.scene.IsValid()))
        {
            jumpscareSpawnPoint = null;
        }

        // 4. Nếu jumpscareModel là Scene object hợp lệ, ẩn tạm ban đầu
        if (jumpscareModel != null && jumpscareModel.scene.IsValid())
        {
            jumpscareModel.SetActive(false);
        }
    }

    private bool IsValidVisualObject(GameObject obj)
    {
        if (obj == null) return false;
        Renderer rend = obj.GetComponentInChildren<Renderer>(true);
        return (rend != null);
    }

    public override void MoveToInspectionPoint()
    {
        base.MoveToInspectionPoint();
        hasTriggeredScare = false;
        isAttackingPlayer = false;
    }

    public override void StartInspection()
    {
        if (isInspecting) return;
        base.StartInspection();

        if (scareCoroutine != null) StopCoroutine(scareCoroutine);
        scareCoroutine = StartCoroutine(JumpscareModelSequence());
    }

    protected override void OnInspectionStarted()
    {
        base.OnInspectionStarted();

        if (scareCoroutine == null)
        {
            scareCoroutine = StartCoroutine(JumpscareModelSequence());
        }
    }

    /// <summary>
    /// Kích hoạt jumpscare ngay lập tức (khi người chơi bấm Duyệt hoặc Từ chối trước khi hết thời gian chờ)
    /// </summary>
    public void TriggerImmediateJumpscare()
    {
        if (hasTriggeredScare) return;
        if (scareCoroutine != null) StopCoroutine(scareCoroutine);
        scareCoroutine = StartCoroutine(ExecuteJumpscareFlight());
    }

    private IEnumerator JumpscareModelSequence()
    {
        float delay = Random.Range(minTriggerDelay, maxTriggerDelay);

        // Phát âm thanh thở dốc hồi hộp
        PlaySound(nervousBreathingSound, 0.7f);

        // Hiệu ứng giật nhẹ người trước khi lao tới
        float elapsed = 0f;
        Vector3 initialPos = transform.position;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            transform.position = initialPos + Random.insideUnitSphere * 0.012f;
            yield return null;
        }
        transform.position = initialPos;

        // Tiến hành bay vồ
        yield return StartCoroutine(ExecuteJumpscareFlight());
    }

    private IEnumerator ExecuteJumpscareFlight()
    {
        if (hasTriggeredScare) yield break;

        hasTriggeredScare = true;
        isAttackingPlayer = true;

        // 1. Xác định vị trí xuất phát cú nhảy: Mặc định là ngay tại vị trí đứng trước cửa sổ của NPC!
        Vector3 startFlyPos;
        if (jumpscareSpawnPoint != null && jumpscareSpawnPoint.gameObject.scene.IsValid())
        {
            startFlyPos = jumpscareSpawnPoint.position;
        }
        else
        {
            startFlyPos = transform.position + Vector3.up * 1.0f;
        }

        // 2. Tìm model hoặc prefab jumpscare trực quan
        GameObject modelToUse = null;
        if (jumpscareModel != null && IsValidVisualObject(jumpscareModel))
        {
            modelToUse = jumpscareModel;
        }
        else if (jumpscarePrefab != null && IsValidVisualObject(jumpscarePrefab))
        {
            modelToUse = jumpscarePrefab;
        }
        else
        {
#if UNITY_EDITOR
            modelToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/JumpScare/JumpScare.prefab");
#endif
        }

        // 3. Chuẩn bị đối tượng jumpscare model
        GameObject activeScareInstance = null;
        if (modelToUse != null)
        {
            if (modelToUse.scene.IsValid())
            {
                activeScareInstance = modelToUse;
                activeScareInstance.transform.position = startFlyPos;
                activeScareInstance.SetActive(true);
            }
            else
            {
                // Khởi tạo prefab quái vật tại đúng vị trí cửa sổ
                activeScareInstance = Instantiate(modelToUse, startFlyPos, transform.rotation);
                activeScareInstance.SetActive(true);
            }

            // Đảm bảo bật tất cả Renderer trên mô hình jumpscare
            Renderer[] allRends = activeScareInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRends)
            {
                r.enabled = true;
            }
        }

        // 4. Nếu có mô hình jumpscare riêng, ẩn mô hình NPC người thường
        if (hideNormalModelDuringScare && activeScareInstance != null && activeScareInstance != gameObject)
        {
            SetRenderersVisibility(false);
        }

        // 5. Nếu không tìm thấy mô hình nào, dùng chính NPC này để vồ tới camera (fallback an toàn tuyệt đối)
        if (activeScareInstance == null)
        {
            activeScareInstance = gameObject;
            startFlyPos = transform.position;
        }

        // 6. BAY TỪ CỬA SỔ VỒ THẲNG VÀO CAMERA CỦA PLAYER
        yield return StartCoroutine(FlyAndLatchOnPlayerFace(activeScareInstance, startFlyPos));

        // 7. KẾT THÚC & BIẾN MẤT
        if (activeScareInstance != null && activeScareInstance != gameObject)
        {
            if (activeScareInstance.scene.IsValid() && activeScareInstance == jumpscareModel)
            {
                activeScareInstance.SetActive(false);
            }
            else
            {
                Destroy(activeScareInstance);
            }
        }

        isAttackingPlayer = false;

        // Chuyển lượt kiểm tra tiếp theo
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

            Destroy(gameObject, 0.2f);
        }
        else if (hideNormalModelDuringScare)
        {
            SetRenderersVisibility(true);
        }
    }

    private IEnumerator FlyAndLatchOnPlayerFace(GameObject scareObject, Vector3 startFlyPos)
    {
        if (scareObject == null) yield break;

        scareObject.SetActive(true);
        Transform scareTransform = scareObject.transform;
        scareTransform.position = startFlyPos;

        // Kích hoạt Animator nếu có
        Animator anim = scareObject.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play(0, 0, 0f);
        }

        // Tạm khóa di chuyển người chơi trong tích tắc để không bị né
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        bool wasPcEnabled = (pc != null && pc.enabled);
        if (pc != null) pc.enabled = false;

        // Tìm đầu quái vật (HeadTarget, frontface, head, Head)
        Transform monsterFace = FindDeepChild(scareTransform, "HeadTarget");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "frontface");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "head");
        if (monsterFace == null) monsterFace = FindDeepChild(scareTransform, "Head");

        // Phát tiếng gầm jumpscare
        PlaySound(jumpscareSound, 1.0f);

        // Tính khoảng cách offset của đầu quái
        Vector3 initialHeadOffset = (monsterFace != null)
            ? (monsterFace.position - scareTransform.position)
            : (scareTransform.up * 1.5f);
        Vector3 startHeadPos = startFlyPos + initialHeadOffset;

        float elapsed = 0f;

        // 1. LAO VỚI TỐC ĐỘ CỰC CAO TỪ CỬA SỔ PHI THẲNG VÀO ỐNG KÍNH CAMERA / JUMPSCARE POINT
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);

            // Tọa độ đích: lấy từ PlayerCameraJumpscarePoint gắn trên camera
            Vector3 targetFacePos = PlayerCameraJumpscarePoint.GetJumpscarePosition(faceLatchDistance);
            GetPlayerCameraOrHead(out Vector3 eyePos, out Vector3 camFwd);

            // Đường cong vòng cung bay qua cửa sổ
            float arc = Mathf.Sin(t * Mathf.PI) * leapHeightArc;
            Vector3 currentFacePos = Vector3.Lerp(startHeadPos, targetFacePos, t) + Vector3.up * arc;

            // Xoay mặt quái vật hướng thẳng vào mắt người chơi
            Vector3 toCamera = (eyePos - currentFacePos);
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                scareTransform.rotation = Quaternion.LookRotation(toCamera.normalized);
            }

            // Đồng bộ vị trí root để mặt quái khớp chính xác tại currentFacePos
            Vector3 headOffset = (monsterFace != null)
                ? (monsterFace.position - scareTransform.position)
                : (scareTransform.up * 1.5f);
            scareTransform.position = currentFacePos - headOffset;

            yield return null;
        }

        // 2. VA CHẠM: Kích hoạt rung lắc màn hình cực mạnh
        TriggerCameraShake(cameraShakeDuration, cameraShakeMagnitude);

        // 3. BÁM MÀN HÌNH: Cào cấu và gào thét ngay sát mặt người chơi
        float scareElapsed = 0f;
        while (scareElapsed < faceScareDuration)
        {
            scareElapsed += Time.deltaTime;

            Vector3 jitter = Random.insideUnitSphere * 0.025f;
            Vector3 targetFacePos = PlayerCameraJumpscarePoint.GetJumpscarePosition(faceLatchDistance) + jitter;

            scareTransform.rotation = PlayerCameraJumpscarePoint.GetJumpscareRotation();

            Vector3 headOffset = (monsterFace != null)
                ? (monsterFace.position - scareTransform.position)
                : (scareTransform.up * 1.5f);
            scareTransform.position = targetFacePos - headOffset;

            yield return null;
        }

        // Khôi phục di chuyển người chơi
        if (pc != null && wasPcEnabled) pc.enabled = true;
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

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(clip);
        else if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }

    private void OnDisable()
    {
        if (scareCoroutine != null)
        {
            StopCoroutine(scareCoroutine);
            scareCoroutine = null;
        }
    }
}
