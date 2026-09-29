using UnityEngine;

/// <summary>
/// Script điều khiển leo thang Hideout hoàn toàn bằng phím di chuyển WASD.
/// KHÔNG CẦN BẤM PHÍM [E] ĐỂ TƯƠNG TÁC:
/// - Tiến tới thang, giữ W để leo lên.
/// - Giữ S để leo xuống.
/// - Nhấn Space để nhảy buông tay khỏi thang.
/// - Khi lên tới đỉnh sàn sẽ tự động bước ra mặt sàn phòng mượt mà.
/// </summary>
public class HideoutLadderInteractable : MonoBehaviour
{
    public enum ExitDirection
    {
        IntoRoom_NegativeZ, // Bước ra sàn phía trước mặt thang (-Z) - Thang tựa tường
        Forward_PositiveZ,  // Bước ra sàn theo hướng (+Z)
        Right_PositiveX,    // Bước ra sàn sang phải (+X)
        Left_NegativeX      // Bước ra sàn sang trái (-X)
    }

    [Header("=== CÀI ĐẶT TỐC ĐỘ LEO (WASD) ===")]
    [Tooltip("Tốc độ leo thang lên/xuống (m/s)")]
    public float climbSpeed = 2.8f;

    [Tooltip("Tốc độ dịch chuyển nhẹ sang trái/phải trên thang (A/D)")]
    public float strafeSpeed = 1.0f;

    [Header("=== KÍCH THƯỚC VÙNG BÁM THANG (TRIGGER) ===")]
    [Tooltip("Độ dày vùng nhận diện trước mặt thang (mặc định 0.35m để dễ bước vào)")]
    public float triggerDepth = 0.35f;

    [Tooltip("Độ rộng thêm hai bên thang")]
    public float triggerWidthExtra = 0.08f;

    [Tooltip("Khoảng kéo dài trigger xuống dưới để chạm sát mặt sàn đáy hầm (mét)")]
    public float triggerBottomExtension = 0.35f;

    [Header("=== ĐIỂM ĐỈNH THANG & BƯỚC RA SÀN ===")]
    public Transform topExitPoint;
    public Transform bottomPoint;
    public ExitDirection exitDirection = ExitDirection.IntoRoom_NegativeZ;
    public float exitStepDistance = 0.75f;

    [Header("=== ÂM THANH BƯỚC THANG ===")]
    public AudioClip[] ladderStepSounds;
    public float stepSoundInterval = 0.35f;

    // Trạng thái nội bộ
    private float climbCooldown = 0f;
    private float stepSoundTimer = 0f;
    private bool isPlayerInZone = false;
    private PlayerController activePlayer;
    private CharacterController activeCC;
    private BoxCollider mainCollider;
    private BoxCollider triggerCollider;

    void Awake()
    {
        mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            // Đặt collider của thang thành Trigger để người chơi không bị cấn va chạm vật lý khi bước vào thang
            mainCollider.isTrigger = true;
        }

        // Đảm bảo Rigidbody không bị trọng lực kéo rơi thang
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetupTriggerCollider();
    }

    void Update()
    {
        // Đếm ngược thời gian miễn nhiễm sau khi vừa bước lên sàn
        if (climbCooldown > 0f)
        {
            climbCooldown -= Time.deltaTime;
        }

        // Xử lý di chuyển leo thang WASD theo từng frame trong Update để mượt mà nhất
        HandleWASDClimbing();
    }

    private void SetupTriggerCollider()
    {
        if (mainCollider == null) return;

        // Đảm bảo collider chính của thang không cản trở di chuyển
        mainCollider.isTrigger = true;

        // Tìm collider trigger chuyên dụng hoặc tạo mới
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        foreach (var col in colliders)
        {
            if (col.isTrigger && col != mainCollider)
            {
                triggerCollider = col;
                break;
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }

        // Kéo dài trigger xuống dưới đáy (triggerBottomExtension) để chạm hẳn mặt sàn hầm,
        // giúp người chơi chỉ cần đi bộ bình thường tới gần chân thang là chạm ngay trigger!
        triggerCollider.size = new Vector3(
            mainCollider.size.x + triggerWidthExtra,
            mainCollider.size.y + triggerBottomExtension,
            triggerDepth
        );

        // Tâm của Trigger hạ thấp (triggerBottomExtension * 0.5f) và nhô ra phía trước đón người chơi
        Vector3 offsetDir = GetExitDirectionVectorLocal();
        triggerCollider.center = mainCollider.center 
            - new Vector3(0, triggerBottomExtension * 0.5f, 0) 
            + offsetDir * (triggerDepth * 0.45f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (climbCooldown > 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            activePlayer = player;
            activeCC = player.GetComponent<CharacterController>();
            isPlayerInZone = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (activePlayer == null)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                activePlayer = player;
                activeCC = player.GetComponent<CharacterController>();
                isPlayerInZone = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null && player == activePlayer)
        {
            if (activePlayer.isClimbing)
            {
                activePlayer.SetClimbing(false);
            }
            activePlayer = null;
            activeCC = null;
            isPlayerInZone = false;
        }
    }

    private void OnDisable()
    {
        if (activePlayer != null && activePlayer.isClimbing)
        {
            activePlayer.SetClimbing(false);
        }
        activePlayer = null;
        activeCC = null;
        isPlayerInZone = false;
    }

    /// <summary>
    /// Xử lý leo thang mượt mà bằng phím WASD:
    /// W: Leo lên
    /// S: Leo xuống
    /// A/D: Dịch ngang nhẹ
    /// Space: Nhảy buông tay
    /// </summary>
    private void HandleWASDClimbing()
    {
        if (!isPlayerInZone || activePlayer == null || activeCC == null || climbCooldown > 0f)
            return;

        float v = Input.GetAxis("Vertical");   // W (+1) / S (-1)
        float h = Input.GetAxis("Horizontal"); // A (-1) / D (+1)

        // 1. Nhấn Space để nhảy buông tay khỏi thang (chỉ khi đang thực sự bám leo thang)
        if (Input.GetButtonDown("Jump"))
        {
            if (activePlayer.isClimbing)
            {
                activePlayer.SetClimbing(false);
                climbCooldown = 0.6f;

                Vector3 jumpBackDir = -GetExitForwardVector();
                jumpBackDir.y = 0;
                activeCC.Move(jumpBackDir.normalized * 1.3f + Vector3.up * 1.0f);
                return;
            }
        }

        Vector3 topPos = GetTopPosition();
        Vector3 bottomPos = GetBottomPosition();
        Vector3 ladderClimbDir = (topPos - bottomPos).normalized;
        if (ladderClimbDir == Vector3.zero) ladderClimbDir = Vector3.up;

        // 2. Nhấn W: Leo LÊN thang
        if (v > 0.1f)
        {
            activePlayer.SetClimbing(true);

            Vector3 climbMove = ladderClimbDir * (climbSpeed * Time.deltaTime);
            // Cho phép dịch nhẹ sang trái/phải nếu bấm A/D
            if (Mathf.Abs(h) > 0.1f)
            {
                climbMove += transform.right * (h * strafeSpeed * Time.deltaTime);
            }

            activeCC.Move(climbMove);
            PlayStepSoundWithTimer();

            // Kiểm tra nếu đã lên tới đỉnh thang -> Tự động bước dứt khoát ra sàn phòng
            if (activePlayer.transform.position.y >= topPos.y - 0.2f)
            {
                Vector3 stepDir = GetExitForwardVector();
                stepDir.y = 0;
                activeCC.Move(stepDir.normalized * exitStepDistance);

                // Trả lại quyền đi bộ ngay lập tức và bật cooldown để không bị hút lại
                activePlayer.SetClimbing(false);
                climbCooldown = 0.8f;
            }
        }
        // 3. Nhấn S: Leo XUỐNG thang
        else if (v < -0.1f)
        {
            activePlayer.SetClimbing(true);

            Vector3 climbMove = -ladderClimbDir * (climbSpeed * Time.deltaTime);
            if (Mathf.Abs(h) > 0.1f)
            {
                climbMove += transform.right * (h * strafeSpeed * Time.deltaTime);
            }

            activeCC.Move(climbMove);
            PlayStepSoundWithTimer();

            // Nếu chân đã chạm sàn đáy hầm (chỉ dừng leo nếu người chơi ở gần chân thang)
            bool isNearBottom = activePlayer.transform.position.y <= bottomPos.y + 0.5f;
            if (activePlayer.transform.position.y <= bottomPos.y + 0.15f || (isNearBottom && activePlayer.isGrounded))
            {
                activePlayer.SetClimbing(false);
            }
        }
        // 4. Người chơi buông phím (không bấm W/S)
        else
        {
            // Nếu đang đứng trên mặt sàn đáy hầm hoặc đỉnh phòng:
            // Không khóa người chơi, cho phép đi lại tự do!
            bool isNearBottom = activePlayer.transform.position.y <= bottomPos.y + 0.5f;
            bool isNearTop = activePlayer.transform.position.y >= topPos.y - 0.3f;

            if (activePlayer.isGrounded && (isNearBottom || isNearTop))
            {
                activePlayer.SetClimbing(false);
            }
        }
    }

    private void PlayStepSoundWithTimer()
    {
        stepSoundTimer -= Time.deltaTime;
        if (stepSoundTimer <= 0f)
        {
            stepSoundTimer = stepSoundInterval;
            PlayLadderSound();
        }
    }

    private void PlayLadderSound()
    {
        if (ladderStepSounds != null && ladderStepSounds.Length > 0)
        {
            AudioClip clip = ladderStepSounds[Random.Range(0, ladderStepSounds.Length)];
            if (clip != null && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(clip);
            }
        }
    }

    public Vector3 GetTopPosition()
    {
        if (topExitPoint != null) return topExitPoint.position;

        if (mainCollider != null)
        {
            Vector3 localTop = mainCollider.center + new Vector3(0, mainCollider.size.y * 0.5f, 0);
            return transform.TransformPoint(localTop);
        }

        return transform.position + Vector3.up * 2.0f;
    }

    public Vector3 GetTopExitPosition()
    {
        if (topExitPoint != null) return topExitPoint.position;

        Vector3 top = GetTopPosition();
        Vector3 exitDir = GetExitForwardVector();
        exitDir.y = 0;
        return top + exitDir.normalized * exitStepDistance;
    }

    public Vector3 GetBottomPosition()
    {
        if (bottomPoint != null) return bottomPoint.position;

        if (mainCollider != null)
        {
            Vector3 localBottom = mainCollider.center - new Vector3(0, mainCollider.size.y * 0.5f, 0);
            return transform.TransformPoint(localBottom);
        }

        return transform.position;
    }

    public Vector3 GetExitForwardVector()
    {
        switch (exitDirection)
        {
            case ExitDirection.IntoRoom_NegativeZ: return -transform.forward;
            case ExitDirection.Forward_PositiveZ:  return transform.forward;
            case ExitDirection.Right_PositiveX:    return transform.right;
            case ExitDirection.Left_NegativeX:     return -transform.right;
            default: return -transform.forward;
        }
    }

    private Vector3 GetExitDirectionVectorLocal()
    {
        switch (exitDirection)
        {
            case ExitDirection.IntoRoom_NegativeZ: return Vector3.back;
            case ExitDirection.Forward_PositiveZ:  return Vector3.forward;
            case ExitDirection.Right_PositiveX:    return Vector3.right;
            case ExitDirection.Left_NegativeX:     return Vector3.left;
            default: return Vector3.back;
        }
    }

    // Hiển thị trực quan trong Scene View: Điểm chân thang, đỉnh thang và hướng bước ra sàn
    private void OnDrawGizmosSelected()
    {
        Vector3 bottom = GetBottomPosition();
        Vector3 top = GetTopPosition();
        Vector3 exitPos = GetTopExitPosition();

        // 1. Chân thang (Xanh dương)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(bottom, 0.12f);

        // 2. Thân thang (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(bottom, top);

        // 3. Đỉnh thang (Vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(top, 0.12f);

        // 4. Mũi tên và điểm bước ra sàn phòng (Cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(top, exitPos);
        Gizmos.DrawWireSphere(exitPos, 0.18f);

        // 5. Khung vùng Trigger (để thấy độ vừa vặn)
        if (mainCollider != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Vector3 gizmoCenter = mainCollider.center 
                - new Vector3(0, triggerBottomExtension * 0.5f, 0) 
                + GetExitDirectionVectorLocal() * (triggerDepth * 0.45f);
            Vector3 gizmoSize = new Vector3(
                mainCollider.size.x + triggerWidthExtra, 
                mainCollider.size.y + triggerBottomExtension, 
                triggerDepth);
            Gizmos.DrawWireCube(gizmoCenter, gizmoSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
