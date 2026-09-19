using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý tương tác của người chơi qua Raycast từ Camera.
/// Đảm bảo:
/// 1. Tương tác chuẩn xác khi nhìn đúng vào vật thể (Cửa, Máy phát điện, Điểm kiểm tra hàng rào...).
/// 2. Bị chặn tầm nhìn bởi Tường, Cầu thang, Sàn, Vật cản đặc (ngăn tương tác xuyên tường).
/// 3. Không bị lan sang các vật thể con không liên quan (không hiện Open Door khi nhìn vào cầu thang / tường).
/// 4. Bỏ qua các Trigger vô hình không tương tác (vùng sương mù, âm thanh, footstep...).
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("=== CẤU HÌNH TƯƠNG TÁC ===")]
    [Tooltip("Khoảng cách tương tác tối đa (mét)")]
    public float interactDistance = 3.0f;

    [Tooltip("Layer kiểm tra va chạm (Bao gồm Tường, Sàn, Cầu thang, Mặc định, Interactable)")]
    public LayerMask collisionMask = ~0;

    [Header("=== GIAO DIỆN & HIỆU ỨNG ===")]
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private Animator armAnimator;

    private Transform playerRoot;

    void Awake()
    {
        playerRoot = transform.root;
    }

    void Update()
    {
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            return;
        }
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            return;
        }

        PlayerInteraction();
    }

    public void PlayerInteraction()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        // Quét tất cả vật thể và trigger theo tia nhìn thẳng từ Camera
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, collisionMask, QueryTriggerInteraction.Collide);

        if (hits != null && hits.Length > 0)
        {
            // Sắp xếp theo thứ tự khoảng cách gần nhất -> xa nhất
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Bỏ qua collider của chính bản thân người chơi
                if (playerRoot != null && hit.collider.transform.root == playerRoot)
                {
                    continue;
                }

                // Chỉ tìm Interactable trên chính collider đó hoặc cha trực tiếp (1 cấp)
                // (TUYỆT ĐỐI KHÔNG dùng GetComponentInParent xuyên suốt vì sẽ quét nhầm lên tận root Scene/Căn phòng/Cầu thang)
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable == null && hit.collider.transform.parent != null)
                {
                    interactable = hit.collider.transform.parent.GetComponent<Interactable>();
                }

                // 1. Nếu đúng là vật thể tương tác (Cửa, Máy phát điện, Điểm kiểm tra rào...)
                if (interactable != null && !string.IsNullOrEmpty(interactable.promptMessage))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (armAnimator != null)
                        {
                            armAnimator.SetTrigger("Interact");
                        }

                        // Thực hiện tương tác
                        interactable.Interact();

                        // Nếu sau khi tương tác promptMessage bị xóa thì ẩn UI ngay lập tức
                        if (string.IsNullOrEmpty(interactable.promptMessage))
                        {
                            if (hitText != null) hitText.gameObject.SetActive(false);
                            return;
                        }
                    }

                    // Hiện gợi ý tương tác lên màn hình
                    if (hitText != null)
                    {
                        hitText.text = "[E] - " + interactable.promptMessage;
                        hitText.gameObject.SetActive(true);
                    }
                    return;
                }
                else
                {
                    // 2. Nếu chạm phải vật thể KHÔNG CÓ Interactable:
                    // - Nếu đó là Trigger vô hình (vùng âm thanh, sương mù, footstep...) -> Xuyên qua để quét tiếp
                    if (hit.collider.isTrigger)
                    {
                        continue;
                    }

                    // - Nếu đó là VẬT CẢN ĐẶC (Cầu thang, Tường, Sàn, Cột...) -> Chặn đứng tầm nhìn, không cho tương tác xuyên qua!
                    break;
                }
            }
        }

        // Nếu không có vật tương tác trong tầm nhìn -> Ẩn UI gợi ý
        if (hitText != null)
        {
            hitText.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * interactDistance);
    }
}
