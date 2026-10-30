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
    [Tooltip("Kéo điểm xuất phát vào đây (nếu để trống, script sẽ tự động tìm StartSpawn trong hầm Cellar)")]
    public Transform defaultSpawnPoint;

    [Tooltip("Tên GameObject điểm xuất phát cần tự động tìm kiếm (Mặc định: StartSpawn)")]
    public string targetSpawnPointName = "StartSpawn";

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

        PlayerSpawnManager mgr = FindFirstObjectByType<PlayerSpawnManager>() ?? FindObjectOfType<PlayerSpawnManager>();
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
        PlayerController pc = FindFirstObjectByType<PlayerController>() ?? FindObjectOfType<PlayerController>();
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
                Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position + Vector3.up * yOffset : new Vector3(-11.596f, -12.450f, -25.516f);
                Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.Euler(0f, 147.234f, 0f);

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

        // 4. Định vị Player tới Spawn Point
        if (autoPositionOnStart && playerObj != null)
        {
            Vector3 targetPos = (spawnPoint != null) ? spawnPoint.position : new Vector3(-11.596f, -12.450f, -25.516f);
            Quaternion targetRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.Euler(0f, 147.234f, 0f);
            PositionPlayerAt(playerObj, pc, targetPos, targetRot);
        }

        return playerObj;
    }

    /// <summary>
    /// Tìm điểm xuất phát StartSpawn trong Scene hoặc Cellar
    /// </summary>
    public Transform GetSpawnPoint()
    {
        if (defaultSpawnPoint != null) return defaultSpawnPoint;

        // 1. Tìm theo tên chỉ định (Mặc định: StartSpawn)
        if (!string.IsNullOrEmpty(targetSpawnPointName))
        {
            GameObject found = GameObject.Find(targetSpawnPointName);
            if (found != null)
            {
                defaultSpawnPoint = found.transform;
                return defaultSpawnPoint;
            }
        }

        // 2. Tìm kiếm danh sách các tên thông dụng: StartSpawn, startspawn, CellarSpawnPoint
        string[] candidates = new string[] { "StartSpawn", "startspawn", "Start_Spawn", "CellarSpawnPoint", "cellarspawnpoint", "CellarSpawn" };
        foreach (string cName in candidates)
        {
            GameObject found = GameObject.Find(cName);
            if (found != null)
            {
                defaultSpawnPoint = found.transform;
                return defaultSpawnPoint;
            }
        }

        // 3. Tìm bên trong Cellar
        GameObject cellar = GameObject.Find("Cellar");
        if (cellar != null)
        {
            foreach (string cName in candidates)
            {
                Transform pt = cellar.transform.Find(cName);
                if (pt != null)
                {
                    defaultSpawnPoint = pt;
                    return defaultSpawnPoint;
                }
            }

            // Tìm đệ quy trong tất cả các con của Cellar
            foreach (Transform child in cellar.GetComponentsInChildren<Transform>(true))
            {
                string lower = child.name.ToLower();
                if (lower == "startspawn" || lower == "start_spawn" || lower == "cellarspawnpoint" || lower.Contains("startspawn") || lower.Contains("cellarspawn"))
                {
                    defaultSpawnPoint = child;
                    return defaultSpawnPoint;
                }
            }
        }

        // 4. Fallback: BedWakeupPoint
        GameObject fallback = GameObject.Find("BedWakeupPoint");
        if (fallback != null)
        {
            defaultSpawnPoint = fallback.transform;
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

        // Cập nhật PlayerCameraJumpscarePoint nếu có
        PlayerCameraJumpscarePoint jp = FindFirstObjectByType<PlayerCameraJumpscarePoint>() ?? FindObjectOfType<PlayerCameraJumpscarePoint>();
        if (jp != null)
        {
            jp.EnsureAttachedToCamera();
        }

        // Đảm bảo Đèn Pin luôn tắt khi mới vào game cho đến khi nhặt từ tủ đồ
        FlashlightController fl = playerObj.GetComponentInChildren<FlashlightController>(true);
        if (fl != null && (TaskManager.instance == null || !TaskManager.instance.hasFlashlight))
        {
            fl.hasFlashlight = false;
            Light flLight = fl.GetFlashlightLight();
            if (flLight != null) flLight.enabled = false;
        }

        Debug.Log($"[PlayerSpawnManager] Đã đặt Player tại StartSpawn: {finalPos}, góc xoay: {targetRot.eulerAngles.y}°");
    }

    private IEnumerator StabilizePlayerPosition()
    {
        if (!stabilizePhysicsNextFrame) yield break;

        yield return null;

        Transform spawnPoint = GetSpawnPoint();
        Vector3 targetPos = (spawnPoint != null) ? spawnPoint.position : new Vector3(-11.596f, -12.450f, -25.516f);
        Quaternion targetRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.Euler(0f, 147.234f, 0f);

        PlayerController pc = FindFirstObjectByType<PlayerController>() ?? FindObjectOfType<PlayerController>();
        GameObject playerObj = (pc != null) ? pc.gameObject : GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            PositionPlayerAt(playerObj, pc, targetPos, targetRot);
        }
    }
}
