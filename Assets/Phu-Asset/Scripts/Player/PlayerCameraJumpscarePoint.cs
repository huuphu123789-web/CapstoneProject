using UnityEngine;

/// <summary>
/// Điểm xuất hiện Jumpscare trên Camera của Player (Player Camera Jumpscare Point).
/// - Được gắn làm con (child) của Camera chính (Main Camera hoặc Cinemachine First Person Camera).
/// - Bất kỳ quái vật hoặc hiệu ứng jumpscare nào (như NPC Crawler, NPC Jumper,...) 
///   khi bay tới người chơi sẽ dùng điểm này làm tọa độ đích để vồ thẳng vào mắt người chơi!
/// - Có Gizmo trực quan trong Scene view để người phát triển dễ dàng kéo thả, căn chỉnh khoảng cách và góc nhìn.
/// </summary>
public class PlayerCameraJumpscarePoint : MonoBehaviour
{
    private static PlayerCameraJumpscarePoint instance;

    [Header("=== CẤU HÌNH VỊ TRÍ TRÊN CAMERA ===")]
    [Tooltip("Khoảng cách và vị trí lệch tương đối so với Camera (Local Offset). Mặc định là (0, 0, 0.4) thẳng ngay trước ống kính")]
    public Vector3 localOffset = new Vector3(0f, 0f, 0.4f);

    [Tooltip("Khoảng cách bám mặt tối ưu từ mắt người chơi đến mặt quái vật khi jumpscare (mét)")]
    public float faceLatchDistance = 0.35f;

    [Tooltip("Góc xoay bù trừ nếu model jumpscare bị ngược hoặc lệch góc")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("=== HIỂN THỊ GIZMOS TRONG SCENE ===")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 0.1f, 0.1f, 0.85f);
    public float gizmoSphereRadius = 0.15f;

    public static PlayerCameraJumpscarePoint Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerCameraJumpscarePoint>();
                if (instance == null)
                {
                    instance = CreateDefaultCameraPoint();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureAttachedToCamera();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Khi nạp Scene mới, tự động tìm Camera mới và gắn vào ngay lập tức
        EnsureAttachedToCamera();
    }

    private void Start()
    {
        EnsureAttachedToCamera();
    }

    /// <summary>
    /// Đảm bảo điểm này luôn là con của Camera chính và nằm đúng vị trí offset
    /// </summary>
    public void EnsureAttachedToCamera()
    {
        Camera targetCam = GetComponentInParent<Camera>();
        if (targetCam == null)
        {
            targetCam = Camera.main;
            if (targetCam == null) targetCam = FindFirstObjectByType<Camera>();
        }

        if (targetCam != null && transform.parent != targetCam.transform)
        {
            transform.SetParent(targetCam.transform, false);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.Euler(rotationOffset);
        }
    }

    /// <summary>
    /// Tự động tạo điểm Jumpscare trên Camera chính nếu Scene chưa có
    /// </summary>
    public static PlayerCameraJumpscarePoint CreateDefaultCameraPoint()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        Transform parentTransform = null;
        if (cam != null)
        {
            parentTransform = cam.transform;
        }
        else
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Transform head = player.transform.Find("HeadTarget");
                if (head == null) head = player.transform.Find("CM_FirstPreson");
                parentTransform = (head != null) ? head : player.transform;
            }
        }

        GameObject pointObj = new GameObject("PlayerCameraJumpscarePoint");
        if (parentTransform != null)
        {
            pointObj.transform.SetParent(parentTransform, false);
            pointObj.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            pointObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            pointObj.transform.position = new Vector3(-48.63f, 1.6f, -50.45f) + Vector3.forward * 0.4f;
        }

        PlayerCameraJumpscarePoint point = pointObj.AddComponent<PlayerCameraJumpscarePoint>();
        instance = point;
        Debug.Log("[PlayerCameraJumpscarePoint] Đã tự động tạo điểm Jumpscare trên Camera của Player!");
        return point;
    }

    /// <summary>
    /// Lấy Transform của điểm Jumpscare trên Camera
    /// </summary>
    public static Transform GetPoint()
    {
        return Instance != null ? Instance.transform : null;
    }

    /// <summary>
    /// Lấy vị trí thế giới (World Position) đích mà quái vật jumpscare sẽ bay tới
    /// </summary>
    public static Vector3 GetJumpscarePosition(float customDistance = -1f)
    {
        PlayerCameraJumpscarePoint pt = Instance;
        if (pt != null)
        {
            if (customDistance > 0f && pt.transform.parent != null)
            {
                return pt.transform.parent.position + pt.transform.parent.forward * customDistance;
            }
            return pt.transform.position;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            float dist = (customDistance > 0f) ? customDistance : 0.4f;
            return cam.transform.position + cam.transform.forward * dist;
        }

        return new Vector3(-48.63f, 1.6f, -50.45f);
    }

    /// <summary>
    /// Lấy hướng xoay đối diện thẳng vào ống kính Camera của Player
    /// </summary>
    public static Quaternion GetJumpscareRotation()
    {
        PlayerCameraJumpscarePoint pt = Instance;
        if (pt != null && pt.transform.parent != null)
        {
            // Xoay mặt quái vật hướng thẳng vào ống kính (ngược chiều camera forward)
            return Quaternion.LookRotation(-pt.transform.parent.forward, pt.transform.parent.up) * Quaternion.Euler(pt.rotationOffset);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            return Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);
        }

        return Quaternion.identity;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSphereRadius);

        // Vẽ mũi tên hướng nhìn
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
        Gizmos.DrawRay(transform.position, -transform.forward * 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, gizmoSphereRadius * 0.5f);
        Gizmos.DrawWireSphere(transform.position, gizmoSphereRadius);
    }
}
