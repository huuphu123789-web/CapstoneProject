using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý việc tự động tạo (spawn) hoặc định vị Player tại điểm xuất phát (mặc định: StartSpawn trong hầm Cellar)
/// khi chuyển cảnh từ MainMenu sang Night-1 hoặc khi bắt đầu màn chơi.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager instance;

    [Header("=== PREFAB PLAYER ===")]
    [Tooltip("Kéo Prefab Player vào đây (hoặc để trống để tự động nạp từ Resources/Player hoặc Prefabs/Player)")]
    public GameObject playerPrefab;

    [Header("=== ĐIỂM XUẤT PHÁT (SPAWN POINT) ===")]
    [Tooltip("Kéo điểm xuất phát vào đây (nếu để trống, script sẽ tự động tìm SpawnPoint / PlayerSpawnPoint / StartSpawn trong Scene)")]
    public Transform defaultSpawnPoint;

    [Tooltip("Tên GameObject điểm xuất phát cần tự động tìm kiếm (Mặc định: player spawn point)")]
    public string targetSpawnPointName = "player spawn point";

    [Tooltip("Độ lệch trục Y khi đặt chân Player để tránh kẹt sàn (mét)")]
    public float yOffset = 0.08f;

    [Header("=== TÙY CHỌN ===")]
    [Tooltip("Tự động dịch chuyển Player đến điểm xuất phát ngay khi scene được nạp")]
    public bool autoPositionOnStart = true;

    [Tooltip("Thời gian chờ (frame) trước khi ổn định vị trí vật lý")]
    public bool stabilizePhysicsNextFrame = true;

    // Tự động lắng nghe sự kiện chuyển Scene ngay cả khi bắt đầu từ MainMenu
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitRuntimeListener()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.ToLower().Contains("menu")) return;

        PlayerSpawnManager mgr = FindAnyObjectByType<PlayerSpawnManager>();
        if (mgr == null)
        {
            GameObject spawnerObj = new GameObject("[Auto] PlayerSpawnManager");
            mgr = spawnerObj.AddComponent<PlayerSpawnManager>();
        }

        mgr.EnsurePlayerSpawnedAndPositioned();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        EnsurePlayerSpawnedAndPositioned();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.ToLower().Contains("menu")) return;
        EnsurePlayerSpawnedAndPositioned();
        StartCoroutine(StabilizePlayerPosition());
    }

    void Start()
    {
        StartCoroutine(StabilizePlayerPosition());
    }

    /// <summary>
    /// Kiểm tra và khởi tạo Player nếu chưa có trong Scene, sau đó dịch chuyển đến Spawn Point
    /// </summary>
    public GameObject EnsurePlayerSpawnedAndPositioned()
    {
        // 1. Tìm điểm xuất phát trước
        Transform spawnPoint = GetSpawnPoint();

        // 2. Tìm Player hiện có trong Scene
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        GameObject playerObj = (pc != null) ? pc.gameObject : GameObject.FindWithTag("Player");

        // Nếu PlayerController nằm trên con (PlayerModel):
        if (playerObj != null && playerObj.transform.parent != null && playerObj.transform.parent.CompareTag("Player"))
        {
            playerObj = playerObj.transform.parent.gameObject;
        }

        // 3. Nếu chưa có Player nào trong Scene -> Tự động Instantiate Player Prefab!
        if (playerObj == null)
        {
            GameObject prefabToSpawn = playerPrefab;

            if (prefabToSpawn == null)
            {
                prefabToSpawn = Resources.Load<GameObject>("Player");
            }

#if UNITY_EDITOR
            if (prefabToSpawn == null)
            {
                prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phu-Asset/Prefabs/Player/Player.prefab");
            }
#endif

            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position + Vector3.up * yOffset : Vector3.zero;
                Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

                playerObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                playerObj.name = "Player";
                pc = playerObj.GetComponentInChildren<PlayerController>();

                Debug.Log($"[PlayerSpawnManager] Đã tự động tạo mới Player từ Prefab tại vị trí: {spawnPos}");
            }
            else
            {
                Debug.LogError("[PlayerSpawnManager] Không tìm thấy Player Prefab để khởi tạo!");
                return null;
            }
        }

        // 4. Chỉ định vị Player tới Spawn Point NẾU scene thực sự có điểm Spawn Point (không dùng tọa độ map cũ)
        if (autoPositionOnStart && playerObj != null && spawnPoint != null)
        {
            PositionPlayerAt(playerObj, pc, spawnPoint.position, spawnPoint.rotation);
        }
        else if (playerObj != null)
        {
            // Giữ nguyên vị trí Player đã đặt sẵn trong map mới, chỉ cập nhật JumpscarePoint & trạng thái Đèn Pin
            EnsurePlayerPostSpawnSetup(playerObj);
        }

        return playerObj;
    }

    /// <summary>
    /// Tìm điểm xuất phát (ưu tiên SpawnPoint / PlayerSpawnPoint / StartSpawn) trong Scene hiện tại.
    /// Tự động bỏ qua NPC_SpawnPoint để không nhận nhầm điểm spawn của NPC.
    /// </summary>
    public Transform GetSpawnPoint()
    {
        if (defaultSpawnPoint != null) return defaultSpawnPoint;

        string targetCompact = string.IsNullOrEmpty(targetSpawnPointName) ? "" : targetSpawnPointName.Trim().ToLower().Replace(" ", "").Replace("_", "");
        Transform bestMatch = null;
        int bestPriority = int.MaxValue;

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform t in allTransforms)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            string lower = t.name.Trim().ToLower();

            // Tuyệt đối bỏ qua các điểm spawn của NPC (ví dụ: NPC_SpawnPoint)
            if (lower.Contains("npc")) continue;

            string compact = lower.Replace(" ", "").Replace("_", "");

            int priority = int.MaxValue;
            if (!string.IsNullOrEmpty(targetCompact) && compact == targetCompact)
                priority = 0;
            else if (compact == "playerspawnpoint" || compact == "spawnpoint" || compact == "playerspawn")
                priority = 1;
            else if (compact == "startspawn")
                priority = 2;
            else if (compact == "cellarspawnpoint" || compact == "cellarspawn")
                priority = 3;
            else if (compact == "bedwakeuppoint")
                priority = 4;

            if (priority < bestPriority)
            {
                bestPriority = priority;
                bestMatch = t;
                if (bestPriority == 0) break;
            }
        }

        if (bestMatch != null)
        {
            defaultSpawnPoint = bestMatch;
            return defaultSpawnPoint;
        }

        return null;
    }

    /// <summary>
    /// Đưa Player đến đúng vị trí và hướng xoay một cách an toàn cho CharacterController và Camera
    /// </summary>
    public void PositionPlayerAt(GameObject playerObj, PlayerController pc, Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 finalPos = targetPos + Vector3.up * yOffset;

        if (pc != null)
        {
            pc.TeleportTo(finalPos, targetRot);
        }
        else
        {
            CharacterController cc = playerObj.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerObj.transform.position = finalPos;
            playerObj.transform.rotation = targetRot;

            if (cc != null) cc.enabled = true;
        }

        // Đảm bảo Camera chính đi theo Player vào hầm Cellar
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = finalPos + Vector3.up * 1.6f;
            cam.transform.rotation = targetRot;
        }

        EnsurePlayerPostSpawnSetup(playerObj);

        Debug.Log($"[PlayerSpawnManager] Đã đặt Player tại SpawnPoint: {finalPos}, góc xoay: {targetRot.eulerAngles.y}°");
    }

    private void EnsurePlayerPostSpawnSetup(GameObject playerObj)
    {
        if (playerObj == null) return;

        // Đảm bảo Camera chính luôn có PlayerInteract để tương tác với cửa, vật phẩm...
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<PlayerInteract>() == null && FindAnyObjectByType<PlayerInteract>() == null)
        {
            cam.gameObject.AddComponent<PlayerInteract>();
            Debug.Log("[PlayerSpawnManager] Đã tự động gắn PlayerInteract lên MainCamera!");
        }

        // Cập nhật PlayerCameraJumpscarePoint nếu có
        PlayerCameraJumpscarePoint jp = FindAnyObjectByType<PlayerCameraJumpscarePoint>();
        if (jp != null)
        {
            jp.EnsureAttachedToCamera();
        }

        // Đảm bảo Đèn Pin luôn tắt khi mới vào game cho đến khi nhặt
        FlashlightController fl = playerObj.GetComponentInChildren<FlashlightController>(true);
        if (fl != null && (TaskManager.instance == null || !TaskManager.instance.hasFlashlight))
        {
            fl.hasFlashlight = false;
            Light flLight = fl.GetFlashlightLight();
            if (flLight != null) flLight.enabled = false;
        }

        // Trong scene Night-1 (new), tắt bóng đổ của các đèn phụ (Point/Spot) để tránh tràn Shadow Atlas và tối ưu FPS
        string activeSceneLower = SceneManager.GetActiveScene().name.Trim().ToLower();
        if (activeSceneLower == "night-1 (new)" || activeSceneLower == "night 1 (new)")
        {
            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light l in allLights)
            {
                if (l == null || l.type == LightType.Directional) continue;
                if (l.transform.IsChildOf(playerObj.transform) || l.GetComponentInParent<FlashlightController>() != null)
                    continue;
                if (l.shadows != LightShadows.None)
                    l.shadows = LightShadows.None;
            }
        }
    }

    private IEnumerator StabilizePlayerPosition()
    {
        if (!stabilizePhysicsNextFrame) yield break;

        yield return null;

        Transform spawnPoint = GetSpawnPoint();
        if (spawnPoint == null) yield break;

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        GameObject playerObj = (pc != null) ? pc.gameObject : GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            PositionPlayerAt(playerObj, pc, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
