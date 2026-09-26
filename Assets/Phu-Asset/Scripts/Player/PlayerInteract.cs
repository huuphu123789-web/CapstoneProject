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

    public bool isLookingAtInteractable { get; private set; } = false;

    private Transform playerRoot;

    void Awake()
    {
        playerRoot = transform.root;
    }

    void Start()
    {
        if (hitText == null)
        {
            TextMeshProUGUI[] tmps = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.name.ToLower().Contains("hittext") || t.name.ToLower().Contains("interacttext"))
                {
                    hitText = t;
                    break;
                }
            }
        }

        if (hitText != null)
        {
            RectTransform rt = hitText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f); // Neo cạnh phải chữ tại vị trí lệch trái tâm
            rt.anchoredPosition = new Vector2(-20f, 0f); // Nằm lệch về bên trái tâm màn hình 20 pixel
            rt.sizeDelta = new Vector2(600f, 60f);
            hitText.alignment = TextAlignmentOptions.MidlineRight;
            hitText.enableWordWrapping = false;
        }
    }

    void Update()
    {
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            isLookingAtInteractable = false;
            return;
        }
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            isLookingAtInteractable = false;
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

                // Tìm Interactable: kiểm tra collider hiện tại, cha trực tiếp, hoặc Component con/cha của vật phẩm trang bị
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable == null && hit.collider.transform.parent != null)
                {
                    interactable = hit.collider.transform.parent.GetComponent<Interactable>();
                }
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<EquipmentPickup>();
                }

                // 1. Nếu đúng là vật thể tương tác (Cửa, Máy phát điện, Điểm kiểm tra rào, Đèn pin, Súng, Đạn...)
                if (interactable != null && !string.IsNullOrEmpty(interactable.promptMessage))
                {
                    // Nếu là ngăn kéo (Drawer) và ngăn kéo đang mở, kiểm tra xem phía sau có vật phẩm bên trong (như Đạn, Súng, Chìa khóa) không
                    if (interactable is DrawerInteractable drawer && drawer.IsOpen)
                    {
                        // Quét các hit tiếp theo xem có vật phẩm đặt bên trong ngăn kéo không
                        Interactable innerItem = null;
                        foreach (var nextHit in hits)
                        {
                            if (nextHit.distance <= hit.distance) continue;
                            if (nextHit.collider.transform.IsChildOf(drawer.transform))
                            {
                                var candidate = nextHit.collider.GetComponent<Interactable>() ?? nextHit.collider.GetComponentInParent<EquipmentPickup>();
                                if (candidate != null && candidate != drawer && !string.IsNullOrEmpty(candidate.promptMessage))
                                {
                                    innerItem = candidate;
                                    break;
                                }
                            }
                        }

                        if (innerItem != null)
                        {
                            interactable = innerItem;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (armAnimator != null)
                        {
                            armAnimator.SetTrigger("Interact");
                        }

                        // Thực hiện tương tác
                        interactable.Interact();

                        // Nếu sau khi tương tác promptMessage bị xóa hoặc object bị tắt thì ẩn UI ngay lập tức
                        if (string.IsNullOrEmpty(interactable.promptMessage) || !interactable.gameObject.activeInHierarchy)
                        {
                            isLookingAtInteractable = false;
                            if (hitText != null) hitText.gameObject.SetActive(false);
                            return;
                        }
                    }

                    // Hiện gợi ý tương tác lên màn hình và đổi trạng thái tâm ngắm
                    isLookingAtInteractable = true;
                    if (hitText != null)
                    {
                        hitText.text = $"<color=#FFDD44>[E]</color> {interactable.promptMessage}";
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
        isLookingAtInteractable = false;
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
