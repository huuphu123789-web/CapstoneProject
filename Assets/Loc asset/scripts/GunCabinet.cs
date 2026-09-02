using UnityEngine;

/// <summary>
/// Gan script nay vao GameObject "Tu sung" (phai co Collider).
/// Nguoi choi nhin vao tu va nhan chuot phai (E) de:
///   - Rut sung ra (neu dang cat)
///   - Cat sung vao (neu dang cam)
/// </summary>
public class GunCabinet : MonoBehaviour, IInteractable
{
    [Header("=== Cau Hinh Tu Sung ===")]
    [Tooltip("Keo GameObject 'gun' trong Hierarchy vao day")]
    [SerializeField] private PlayerGun playerGun;

    [Tooltip("Text hien thi khi nhin vao tu va dang cat sung")]
    [SerializeField] private string promptDraw = "Lay sung ra";

    [Tooltip("Text hien thi khi nhin vao tu va dang cam sung")]
    [SerializeField] private string promptHolster = "Cat sung vao";

    [Header("=== Hieu Ung (Tuy chon) ===")]
    [Tooltip("Am thanh khi mo tu / lay sung ra")]
    [SerializeField] private AudioClip drawSound;

    [Tooltip("Am thanh khi dong tu / cat sung vao")]
    [SerializeField] private AudioClip holsterSound;

    [Tooltip("(Tuy chon) Animator cua tu sung de chay animation mo/dong")]
    [SerializeField] private Animator cabinetAnimator;

    [Tooltip("Ten Trigger mo tu trong Animator")]
    [SerializeField] private string openAnimTrigger = "Open";

    [Tooltip("Ten Trigger dong tu trong Animator")]
    [SerializeField] private string closeAnimTrigger = "Close";

    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null && (drawSound != null || holsterSound != null))
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f; // Am thanh 3D tu tu sung
        }

        // Tu dong tim PlayerGun trong Scene neu chua gán
        if (playerGun == null)
            playerGun = FindObjectOfType<PlayerGun>();
    }

    // Hien thi prompt tuong ung voi trang thai hien tai cua sung
    public string InteractPrompt => (playerGun != null && playerGun.IsHolstered)
        ? promptDraw
        : promptHolster;

    public void Interact(GameObject interactor)
    {
        if (playerGun == null)
        {
            Debug.LogWarning("[GunCabinet] Khong tim thay PlayerGun! Hay keo gun vao Inspector.");
            return;
        }

        if (playerGun.IsHolstered)
        {
            // Rut sung ra
            playerGun.DrawGun();

            if (_audio != null && drawSound != null)
                _audio.PlayOneShot(drawSound);

            if (cabinetAnimator != null)
                cabinetAnimator.SetTrigger(openAnimTrigger);

            Debug.Log("[GunCabinet] Da lay sung ra.");
        }
        else
        {
            // Cat sung vao
            playerGun.HolsterGun();

            if (_audio != null && holsterSound != null)
                _audio.PlayOneShot(holsterSound);

            if (cabinetAnimator != null)
                cabinetAnimator.SetTrigger(closeAnimTrigger);

            Debug.Log("[GunCabinet] Da cat sung vao tu.");
        }
    }

    public void OnLookAt()
    {
        // Co the them hieu ung highlight tu sung o day neu muon
    }

    public void OnLookAway()
    {
        // Co the bo highlight o day
    }
}
