using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class NPCInspectionProfile
{
    public string npcName = "Citizen";
    public bool isMutant = false;
    public GameObject npcPrefab;
    public Texture2D cccdTexture;
    public Texture2D surrenderConfirmTexture;
    [TextArea(2, 4)] public string dialogueOnArrive = "Hello officer, here are my papers. Please let me in.";
    [TextArea(2, 4)] public string dialogueOnApproved = "Thank you officer! God bless you.";
    [TextArea(2, 4)] public string dialogueOnDenied = "Please don't leave me out here in the dark...";
}

/// <summary>
/// Quản lý toàn bộ quy trình kiểm tra NPC tại cổng gác:
/// 1. Tự động khởi động khi TaskManager hoàn thành 4 nhiệm vụ ban đầu.
/// 2. Điều phối NPC đi tới vạch InspectionZone trước bốt gác.
/// 3. Xuất trình CCCD và Giấy xác nhận đầu hàng (Surrender Confirm / Giấy thông hành).
/// 4. Xử lý quyết định của người chơi: Cho qua (Approve), Từ chối (Reject), hoặc Bắn súng tiêu diệt (Shoot).
/// 5. Báo cáo hoàn thành cho TaskManager để mở khóa giường ngủ.
/// </summary>
public class NPCInspectionManager : MonoBehaviour
{
    public static NPCInspectionManager instance;

    [Header("=== DANH SÁCH NPC KIỂM TRA (INSPECTION QUEUE) ===")]
    [Tooltip("Danh sách NPC sẽ đến cổng kiểm tra. Bạn có thể tự do thêm 4 mẫu tại đây.")]
    public List<NPCInspectionProfile> npcQueue = new List<NPCInspectionProfile>();

    [Tooltip("Tự động nạp mẫu khi danh sách trống. Tắt (false) để bạn tự thiết lập 4 mẫu trong Inspector.")]
    public bool autoPopulateDefaults = false;

    [Header("=== CÁC ĐIỂM TỌA ĐỘ TRONG SCENE ===")]
    [Tooltip("Điểm đứng chờ kiểm tra trước bốt gác (InspectionZone)")]
    public Transform inspectionPoint;

    [Tooltip("Điểm xuất phát của NPC bên ngoài cổng (đường lớn ngoài rừng)")]
    public Transform spawnPoint;

    [Tooltip("Độ lệch chiều cao Y khi spawn (cộng thêm vào nếu NPC bị chìm chân hoặc lơ lửng)")]
    public float spawnHeightOffset = 0f;

    [Tooltip("Điểm NPC đi vào sau khi được Cho qua (Bên trong khuôn viên căn cứ)")]
    public Transform passDestination;

    [Tooltip("Điểm NPC quay đầu bỏ đi sau khi bị Từ chối (Bên ngoài rừng)")]
    public Transform dismissDestination;

    [Header("=== ÂM THANH & HIỆU ỨNG ===")]
    public AudioClip documentOpenSound;
    public AudioClip gateBuzzerSound;
    public AudioClip alarmBreachSound;
    public AudioClip correctDecisionChime;

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public int currentNPCIndex = -1;
    public bool isInspectionActive = false;
    public bool isCurrentNPCAtDesk = false;

    private GameObject currentNPCObject;
    private NavMeshAgent currentAgent;
    private NPCInspectionProfile currentProfile;
    private bool hasDecisionBeenMade = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        EnsureSceneReferences();
        EnsureDefaultProfiles();
        EnsureDeskAndButtons();
    }

    /// <summary>
    /// Tự động tìm kiếm các điểm tọa độ nếu chưa kéo vào Inspector
    /// </summary>
    public void EnsureSceneReferences()
    {
        // 1. Inspection Point
        if (inspectionPoint == null)
        {
            GameObject iz = GameObject.Find("InspectionZone");
            if (iz != null)
            {
                inspectionPoint = iz.transform;
            }
            else
            {
                GameObject newIZ = new GameObject("InspectionZone");
                newIZ.transform.position = new Vector3(-50.84f, -3.21f, -43.69f);
                inspectionPoint = newIZ.transform;
            }
        }

        // 2. Spawn Point (ngoài đường rừng xa về phía +Z)
        if (spawnPoint == null)
        {
            GameObject sp = GameObject.Find("NPC_SpawnPoint");
            if (sp != null)
            {
                spawnPoint = sp.transform;
            }
            else
            {
                sp = new GameObject("NPC_SpawnPoint");
                sp.transform.position = new Vector3(-50.84f, 0.0f, -20.0f);
                spawnPoint = sp.transform;
            }
        }

        // 3. Pass Destination (trong khuôn viên căn cứ về phía -Z)
        if (passDestination == null)
        {
            GameObject pd = GameObject.Find("NPC_PassDestination");
            if (pd != null)
            {
                passDestination = pd.transform;
            }
            else
            {
                pd = new GameObject("NPC_PassDestination");
                pd.transform.position = new Vector3(-50.84f, -3.21f, -65.0f);
                passDestination = pd.transform;
            }
        }

        // 4. Dismiss Destination (quay trở lại ngoài rừng)
        if (dismissDestination == null)
        {
            GameObject dd = GameObject.Find("NPC_DismissDestination");
            if (dd != null)
            {
                dismissDestination = dd.transform;
            }
            else
            {
                dismissDestination = spawnPoint;
            }
        }
    }

    public static Vector3 GetSampledNavMeshPosition(Vector3 sourcePos, float maxDistance = 3.0f)
    {
        if (NavMesh.SamplePosition(sourcePos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return sourcePos;
    }

    /// <summary>
    /// Tự động nạp mẫu 2 NPC (1 Người thường và 1 Đột biến) với đầy đủ ảnh CCCD và Surrender Confirm
    /// </summary>
    public void EnsureDefaultProfiles()
    {
        if (!autoPopulateDefaults) return;
        if (npcQueue != null && npcQueue.Count > 0) return;

        npcQueue = new List<NPCInspectionProfile>();

        // --- NPC 1: Người Thường (Human Survivor) ---
        NPCInspectionProfile p1 = new NPCInspectionProfile();
        p1.npcName = "Nguyen Van An (ID: 079201004521)";
        p1.isMutant = false;
        p1.dialogueOnArrive = "Hello officer, I've walked miles through the forest. Here are my ID and surrender confirm.";
        p1.dialogueOnApproved = "Thank you so much officer! Stay safe!";
        p1.dialogueOnDenied = "Please reconsider... there are monsters out there!";

        // Load hình ảnh
        p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Victor Jean Mercier.png");
        if (p1.cccdTexture == null) p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-3.png");
        if (p1.cccdTexture == null) p1.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-1.png");

        p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/Victor Jean Mercier.png");
        if (p1.surrenderConfirmTexture == null) p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/pass key/npc-1.png");
        if (p1.surrenderConfirmTexture == null) p1.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-3.png");

        // Tìm prefab
        p1.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-Normal.prefab");
        if (p1.npcPrefab == null) p1.npcPrefab = LoadPrefabFromPath("Assets/Loc asset/Prefabs/NPC/npc_1 Variant.prefab");

        npcQueue.Add(p1);

        // --- NPC 2: Sinh Vật Đột Biến (Mutant Anomaly) ---
        NPCInspectionProfile p2 = new NPCInspectionProfile();
        p2.npcName = "Trinh Hoang Long (ID: 031198007214)";
        p2.isMutant = true;
        p2.dialogueOnArrive = "O...ff...i...cer... l...et... m...e... in...";
        p2.dialogueOnApproved = "Ssssskrrrreeeech!";
        p2.dialogueOnDenied = "Grrrraaaagh!";

        p2.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/Trinh Hoang Long.png");
        if (p2.cccdTexture == null) p2.cccdTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/CCQN/npc-6.png");

        p2.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/Surrender Confirm/npc-6.png");
        if (p2.surrenderConfirmTexture == null) p2.surrenderConfirmTexture = LoadTextureFromPath("Assets/Loc asset/sprites/CCCD/pass key/giay_thong_hanh_trần_quốc_hùng.png");

        p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Trinh Hoang Long-SpinningLegs.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/NPC-Glitch.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-HeadSpin.prefab");
        if (p2.npcPrefab == null) p2.npcPrefab = LoadPrefabFromPath("Assets/Phu-Asset/Prefabs/NPC/Npc-Boomer.prefab");

        npcQueue.Add(p2);

        Debug.Log($"[NPCInspectionManager] Đã cấu hình mặc định {npcQueue.Count} lượt kiểm tra NPC cho Night 1!");
    }

    private Texture2D LoadTextureFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return null;
#endif
    }

    private GameObject LoadPrefabFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
        return null;
#endif
    }

    /// <summary>
    /// Đảm bảo trên bàn bốt gác có điểm tương tác Tập Giấy Tờ và 2 Nút Bấm Cho qua / Từ chối
    /// </summary>
    public void EnsureDeskAndButtons()
    {
        // 1. Đảm bảo có DocumentInspectionUI
        if (DocumentInspectionUI.instance == null)
        {
            GameObject uiGO = new GameObject("DocumentInspectionUI_Manager");
            uiGO.AddComponent<DocumentInspectionUI>();
        }

        // 2. Tìm hoặc tạo điểm tương tác Tập Giấy Tờ trên bàn bốt gác
        InspectionDeskInteractable desk = FindObjectOfType<InspectionDeskInteractable>();
        if (desk == null)
        {
            GameObject deskGO = new GameObject("InspectionDocumentsDesk");
            // Tọa độ quầy cửa sổ trước mặt bốt gác
            deskGO.transform.position = new Vector3(-51.05f, 0.45f, -48.65f);

            BoxCollider col = deskGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.8f, 0.4f, 0.8f);

            desk = deskGO.AddComponent<InspectionDeskInteractable>();
        }

        // 3. Tìm hoặc tạo 2 nút bấm vật lý trên bàn
        GateButtonInteractable[] buttons = FindObjectsOfType<GateButtonInteractable>();
        if (buttons == null || buttons.Length == 0)
        {
            // Nút Xanh - CHO QUA
            GameObject btnPass = GameObject.Find("Button_ApprovePass");
            if (btnPass == null)
            {
                btnPass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                btnPass.name = "Button_ApprovePass";
                btnPass.transform.position = new Vector3(-51.4f, 0.48f, -48.8f);
                btnPass.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);

                Renderer r = btnPass.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.15f, 0.8f, 0.25f);

                GateButtonInteractable gb = btnPass.AddComponent<GateButtonInteractable>();
                gb.buttonType = GateButtonInteractable.ButtonType.ApprovePass;
            }

            // Nút Đỏ - TỪ CHỐI
            GameObject btnDeny = GameObject.Find("Button_RejectDeny");
            if (btnDeny == null)
            {
                btnDeny = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                btnDeny.name = "Button_RejectDeny";
                btnDeny.transform.position = new Vector3(-50.8f, 0.48f, -48.8f);
                btnDeny.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);

                Renderer r = btnDeny.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.85f, 0.2f, 0.2f);

                GateButtonInteractable gb = btnDeny.AddComponent<GateButtonInteractable>();
                gb.buttonType = GateButtonInteractable.ButtonType.RejectDeny;
            }
        }
    }

    /// <summary>
    /// Kích hoạt bắt đầu nhiệm vụ kiểm tra cổng gác (được TaskManager gọi khi xong 4 việc dọn dẹp)
    /// </summary>
    public void StartGateInspection()
    {
        if (isInspectionActive) return;
        isInspectionActive = true;
        currentNPCIndex = -1;

        Debug.Log("[NPCInspectionManager] BẮT ĐẦU NHIỆM VỤ KIỂM TRA CỔNG GÁC!");

        // Thông báo bằng âm thanh chuông hoặc điện thoại nếu có
        if (correctDecisionChime != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(correctDecisionChime);
        }

        SpawnNextNPC();
    }

    /// <summary>
    /// Triệu hồi NPC tiếp theo trong hàng chờ tiến tới bốt gác
    /// </summary>
    public void SpawnNextNPC()
    {
        currentNPCIndex++;

        if (currentNPCIndex >= npcQueue.Count)
        {
            // Đã kiểm tra xong toàn bộ NPC trong đêm!
            CompleteInspectionTask();
            return;
        }

        currentProfile = npcQueue[currentNPCIndex];
        hasDecisionBeenMade = false;
        isCurrentNPCAtDesk = false;

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        // Hủy NPC cũ nếu còn
        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : new Vector3(-50.84f, 0.0f, -20.0f);
        spawnPos.y += spawnHeightOffset;
        Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.Euler(0f, 180f, 0f);

        if (currentProfile.npcPrefab != null)
        {
            currentNPCObject = Instantiate(currentProfile.npcPrefab, spawnPos, spawnRot);
        }
        else
        {
            // Tạo NPC tạm thời nếu thiếu Prefab
            currentNPCObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            currentNPCObject.name = currentProfile.npcName;
            currentNPCObject.transform.position = spawnPos;
        }

        // BẢO VỆ CẤU TRÚC PREFAB:
        // Nếu Prefab có 1 GameObject rỗng bọc ngoài Model duy nhất (như Trinh Hoang Long-SpinningLegs):
        // Ta "mở bao bì", lấy Model thực sự làm root để không bị tách rời (split) giữa root và thân NPC!
        if (currentNPCObject.transform.childCount == 1 &&
            currentNPCObject.GetComponent<Animator>() == null &&
            currentNPCObject.GetComponent<LODGroup>() == null &&
            currentNPCObject.GetComponent<SkinnedMeshRenderer>() == null &&
            currentNPCObject.GetComponent<MeshRenderer>() == null)
        {
            Transform actualModel = currentNPCObject.transform.GetChild(0);
            actualModel.SetParent(null, true);
            Destroy(currentNPCObject);
            currentNPCObject = actualModel.gameObject;
        }

        currentNPCObject.name = currentProfile.npcName;
        currentNPCObject.tag = "NPC";

        // Nếu có container "Model" con bên trong, đảm bảo localPosition chuẩn (0,0,0)
        Transform modelChild = currentNPCObject.transform.Find("Model");
        if (modelChild != null)
        {
            modelChild.localPosition = Vector3.zero;
        }

        // Vô hiệu hóa và dọn dẹp các NavMeshAgent ở object con (nếu có) để tránh xung đột
        NavMeshAgent[] allAgents = currentNPCObject.GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var a in allAgents)
        {
            if (a.gameObject != currentNPCObject)
            {
                a.enabled = false;
                Destroy(a);
            }
        }

        // Vô hiệu hóa trọng lực physics để không bị kéo lọt xuống sàn
        Rigidbody[] rbs = currentNPCObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
        }

        // Đảm bảo có CapsuleCollider để đạn của súng có thể bắn trúng
        CapsuleCollider col = currentNPCObject.GetComponent<CapsuleCollider>();
        if (col == null) col = currentNPCObject.GetComponentInChildren<CapsuleCollider>();
        if (col == null)
        {
            col = currentNPCObject.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
        }

        // Thiết lập NavMeshAgent duy nhất trên ROOT NPC
        currentAgent = currentNPCObject.GetComponent<NavMeshAgent>();
        if (currentAgent == null) currentAgent = currentNPCObject.AddComponent<NavMeshAgent>();
        currentAgent.enabled = true;
        currentAgent.speed = 2.4f;
        currentAgent.stoppingDistance = 0.5f;
        currentAgent.baseOffset = 0f;

        // Reset vị trí Agent chuẩn xác trên NavMesh bằng Warp
        currentNPCObject.transform.position = spawnPos;
        currentNPCObject.transform.rotation = spawnRot;

        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            currentAgent.Warp(hit.position);
        }
        else if (!currentAgent.isOnNavMesh)
        {
            currentAgent.Warp(spawnPos);
        }

        // Đảm bảo có NPCHealth để có thể bắn bằng súng
        NPCHealth health = currentNPCObject.GetComponent<NPCHealth>();
        if (health == null) health = currentNPCObject.AddComponent<NPCHealth>();
        if (health.onDisappear == null) health.onDisappear = new UnityEngine.Events.UnityEvent();
        health.onDisappear.RemoveListener(OnNPCShot);
        health.onDisappear.AddListener(OnNPCShot);

        Vector3 targetPos = (inspectionPoint != null) ? inspectionPoint.position : new Vector3(-50.84f, -3.21f, -43.69f);
        targetPos = GetSampledNavMeshPosition(targetPos);

        if (currentAgent.isOnNavMesh)
        {
            currentAgent.isStopped = false;
            currentAgent.SetDestination(targetPos);
        }

        Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalk", true);

        StartCoroutine(WaitForNPCArrivalRoutine());

        Debug.Log($"[NPCInspectionManager] NPC [{currentProfile.npcName}] đang tiến về bốt gác...");
    }

    private IEnumerator WaitForNPCArrivalRoutine()
    {
        // Chờ 0.3s để NavMeshAgent bắt đầu tính toán lộ trình
        yield return new WaitForSeconds(0.3f);

        while (currentAgent != null && currentNPCObject != null)
        {
            if (currentAgent.enabled && currentAgent.isOnNavMesh && !currentAgent.pathPending)
            {
                if (currentAgent.hasPath && currentAgent.remainingDistance <= currentAgent.stoppingDistance + 0.35f)
                {
                    OnNPCArrivedAtDesk();
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// Khi NPC đã tới trước cửa sổ bốt gác
    /// </summary>
    private void OnNPCArrivedAtDesk()
    {
        isCurrentNPCAtDesk = true;

        if (currentAgent != null && currentAgent.isOnNavMesh)
        {
            currentAgent.isStopped = true;
        }

        Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalk", false);

        // Quay mặt về phía bốt gác
        if (currentNPCObject != null)
        {
            Vector3 lookDir = (new Vector3(-51.17f, currentNPCObject.transform.position.y, -50.18f) - currentNPCObject.transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                currentNPCObject.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Kích hoạt tập giấy tờ trên bàn để người chơi bấm E soi
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(true);
        }

        // Kích hoạt các hành vi kì dị tương ứng của Đột biến khi đến bốt gác
        NPCWeirdBehaviors weird = currentNPCObject.GetComponentInChildren<NPCWeirdBehaviors>();
        if (weird != null) weird.StartInspection();

        NPCSpinningLegsBehavior spin = currentNPCObject.GetComponentInChildren<NPCSpinningLegsBehavior>();
        if (spin != null) spin.StartInspection();

        NPCHeadSpinBehavior headSpin = currentNPCObject.GetComponentInChildren<NPCHeadSpinBehavior>();
        if (headSpin != null) headSpin.StartInspection();

        NPCExploderBehavior exploder = currentNPCObject.GetComponentInChildren<NPCExploderBehavior>();
        if (exploder != null) exploder.StartInspection();

        NPCFloatingBehavior floating = currentNPCObject.GetComponentInChildren<NPCFloatingBehavior>();
        if (floating != null) floating.StartInspection();

        NPCDetachedLimbsBehavior detached = currentNPCObject.GetComponentInChildren<NPCDetachedLimbsBehavior>();
        if (detached != null) detached.StartInspection();

        Debug.Log($"[NPCInspectionManager] NPC [{currentProfile.npcName}] ĐÃ ĐẾN VẠCH KIỂM TRA. Giấy tờ đã sẵn sàng!");
    }

    /// <summary>
    /// Mở giao diện xem giấy tờ của NPC hiện tại
    /// </summary>
    public void OpenCurrentNPCDocuments()
    {
        if (!isCurrentNPCAtDesk || currentProfile == null) return;

        if (documentOpenSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(documentOpenSound);
        }

        if (DocumentInspectionUI.instance != null)
        {
            DocumentInspectionUI.instance.OpenInspection(
                currentProfile.cccdTexture,
                currentProfile.surrenderConfirmTexture,
                currentProfile.npcName
            );
        }
    }

    /// <summary>
    /// Người chơi bấm CHO QUA (APPROVE PASS)
    /// </summary>
    public void ApproveCurrentNPC()
    {
        if (hasDecisionBeenMade || !isCurrentNPCAtDesk) return;
        hasDecisionBeenMade = true;

        if (gateBuzzerSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(gateBuzzerSound);
        }

        if (currentProfile.isMutant)
        {
            // NGUY HIỂM: Cho nhầm đột biến vào căn cứ!
            Debug.LogWarning("[NPCInspectionManager] CẢNH BÁO: Người chơi đã cho ĐỘT BIẾN qua cổng!");
            TriggerBreachAlarm();
            StartCoroutine(MutantAttackRoutine());
        }
        else
        {
            // ĐÚNG ĐẮN: Người thường được vào an toàn
            Debug.Log("[NPCInspectionManager] CHUẨN XÁC: Cho công dân bình thường qua cổng an toàn.");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }

            StartCoroutine(NPCWalkThroughGateRoutine());
        }
    }

    /// <summary>
    /// Người chơi bấm TỪ CHỐI (REJECT DENY)
    /// </summary>
    public void RejectCurrentNPC()
    {
        if (hasDecisionBeenMade || !isCurrentNPCAtDesk) return;
        hasDecisionBeenMade = true;

        if (currentProfile.isMutant)
        {
            // ĐÚNG ĐẮN: Phát hiện và đuổi đột biến
            Debug.Log("[NPCInspectionManager] CHUẨN XÁC: Đã phát hiện và từ chối sinh vật đột biến!");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }

            StartCoroutine(MutantFleesRoutine());
        }
        else
        {
            // Nhầm lẫn: Đuổi người thường
            Debug.LogWarning("[NPCInspectionManager] Bạn đã từ chối một người bình thường.");
            StartCoroutine(NPCDismissedRoutine());
        }
    }

    /// <summary>
    /// Khi NPC bị người chơi bắn hạ bằng súng
    /// </summary>
    public void OnNPCShot()
    {
        if (hasDecisionBeenMade) return;
        hasDecisionBeenMade = true;

        if (currentProfile != null && currentProfile.isMutant)
        {
            Debug.Log("[NPCInspectionManager] TUYỆT VỜI! Bạn đã dùng súng tiêu diệt sinh vật đột biến tại chỗ!");
            if (correctDecisionChime != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(correctDecisionChime);
            }
        }
        else
        {
            Debug.LogWarning("[NPCInspectionManager] Bạn đã bắn nhầm một người dân vô tội!");
        }

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        // Chờ 2.5s rồi gọi NPC tiếp theo
        Invoke(nameof(SpawnNextNPC), 2.5f);
    }

    private IEnumerator NPCWalkThroughGateRoutine()
    {
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentAgent != null && passDestination != null)
        {
            Vector3 target = GetSampledNavMeshPosition(passDestination.position);
            if (!currentAgent.isOnNavMesh && currentNPCObject != null)
            {
                currentAgent.Warp(GetSampledNavMeshPosition(currentNPCObject.transform.position));
            }

            if (currentAgent.isOnNavMesh)
            {
                currentAgent.isStopped = false;
                currentAgent.speed = 2.2f;
                currentAgent.SetDestination(target);
            }

            Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isWalk", true);
        }

        yield return new WaitForSeconds(4.5f);

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        SpawnNextNPC();
    }

    private IEnumerator NPCDismissedRoutine()
    {
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentAgent != null && dismissDestination != null)
        {
            Vector3 target = GetSampledNavMeshPosition(dismissDestination.position);
            if (!currentAgent.isOnNavMesh && currentNPCObject != null)
            {
                currentAgent.Warp(GetSampledNavMeshPosition(currentNPCObject.transform.position));
            }

            if (currentAgent.isOnNavMesh)
            {
                currentAgent.isStopped = false;
                currentAgent.speed = 2.0f;
                currentAgent.SetDestination(target);
            }

            Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isWalk", true);
        }

        yield return new WaitForSeconds(4.5f);

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        SpawnNextNPC();
    }

    private IEnumerator MutantFleesRoutine()
    {
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (currentAgent != null && dismissDestination != null)
        {
            Vector3 target = GetSampledNavMeshPosition(dismissDestination.position);
            if (!currentAgent.isOnNavMesh && currentNPCObject != null)
            {
                currentAgent.Warp(GetSampledNavMeshPosition(currentNPCObject.transform.position));
            }

            if (currentAgent.isOnNavMesh)
            {
                currentAgent.isStopped = false;
                currentAgent.speed = 4.5f; // Chạy nhanh bỏ trốn
                currentAgent.SetDestination(target);
            }

            Animator anim = currentNPCObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isWalk", true);
        }

        yield return new WaitForSeconds(3.5f);

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        SpawnNextNPC();
    }

    private IEnumerator MutantAttackRoutine()
    {
        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        // Đột biến lao qua cổng
        if (currentAgent != null && passDestination != null)
        {
            Vector3 target = GetSampledNavMeshPosition(passDestination.position);
            if (!currentAgent.isOnNavMesh && currentNPCObject != null)
            {
                currentAgent.Warp(GetSampledNavMeshPosition(currentNPCObject.transform.position));
            }

            if (currentAgent.isOnNavMesh)
            {
                currentAgent.isStopped = false;
                currentAgent.speed = 5.0f;
                currentAgent.SetDestination(target);
            }
        }

        // Đèn chớp tắt báo động
        if (TaskManager.instance != null)
        {
            TaskManager.instance.FlickerFlashlight(5, 0.08f, 0.8f);
        }

        yield return new WaitForSeconds(4.0f);

        if (currentNPCObject != null)
        {
            Destroy(currentNPCObject);
        }

        SpawnNextNPC();
    }

    private void TriggerBreachAlarm()
    {
        if (alarmBreachSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(alarmBreachSound);
        }
        else if (AudioManager.instance != null && gateBuzzerSound != null)
        {
            AudioManager.instance.PlaySFX(gateBuzzerSound);
        }
    }

    /// <summary>
    /// Khi đã hoàn thành kiểm tra toàn bộ danh sách NPC
    /// </summary>
    public void CompleteInspectionTask()
    {
        isInspectionActive = false;
        Debug.Log("[NPCInspectionManager] ĐÃ HOÀN THÀNH TOÀN BỘ KIỂM TRA CỔNG GÁC!");

        if (InspectionDeskInteractable.instance != null)
        {
            InspectionDeskInteractable.instance.SetDocumentsAvailable(false);
        }

        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteGateInspection();
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Spawn Point (Màu Xanh Biển)
        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.6f);
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 2.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 1.2f, "🔵 [NPC Spawn Point]");
#endif
        }

        // 2. Inspection Point (Màu Vàng)
        if (inspectionPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(inspectionPoint.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(inspectionPoint.position + Vector3.up * 1.2f, "🟡 [Inspection Zone]");
#endif
            // Đường đi từ Spawn tới Inspection Point
            if (spawnPoint != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, inspectionPoint.position);
            }
        }

        // 3. Pass Destination (Màu Xanh Lá)
        if (passDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(passDestination.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(passDestination.position + Vector3.up * 1.2f, "🟢 [Pass Destination]");
#endif
            // Tuyến đường cho qua từ Inspection tới Pass
            if (inspectionPoint != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(inspectionPoint.position, passDestination.position);
            }
        }

        // 4. Dismiss Destination (Màu Đỏ)
        if (dismissDestination != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(dismissDestination.position, 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(dismissDestination.position + Vector3.up * 1.2f, "🔴 [Dismiss Destination]");
#endif
            // Tuyến đường đuổi đi từ Inspection tới Dismiss
            if (inspectionPoint != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(inspectionPoint.position, dismissDestination.position);
            }
        }
    }
}
