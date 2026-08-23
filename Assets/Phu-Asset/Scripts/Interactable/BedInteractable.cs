using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script dùng cho Giường ngủ (Bed).
/// Kế thừa Interactable (Phú-Asset) - Nhấn phím E để nằm lên giường, ngửa mặt nhìn lên trần nhà, màn hình từ từ tối dần và chuyển Scene!
/// </summary>
public class BedInteractable : Interactable
{
    [Header("=== VỊ TRÍ NẰM NGỦ TRÊN GIƯỜNG ===")]
    [Tooltip("Kéo điểm LieDownPoint trên giường vào đây")]
    public Transform lieDownPoint;

    [Header("=== GÓC NHÌN LÊN TRẦN NHÀ ===")]
    [Tooltip("Góc ngẩng đầu nhìn lên trần nhà (-60 đến -80 độ là nhìn lên trần)")]
    public float lookUpAngle = -60f;

    [Header("=== THỜI GIAN HIỆU ỨNG (CINEMATIC TIMING) ===")]
    [Tooltip("Thời gian Player di chuyển và ngả người nằm xuống giường (giây)")]
    public float lieDownDuration = 2.0f;

    [Tooltip("Thời gian nằm ngắm trần nhà TRƯỚC KHI màn hình bắt đầu tối (giây)")]
    public float waitBeforeFade = 2.0f;

    [Tooltip("Thời gian màn hình từ từ tối đen dần (Fade Duration)")]
    public float fadeDuration = 2.5f;

    [Tooltip("Thời gian chờ trong bóng tối trước khi nạp Scene mới")]
    public float waitBeforeLoad = 1.5f;

    [Header("=== CHUYỂN LEVEL ===")]
    [Tooltip("Tên Scene tiếp theo cần tải (VD: Night-2, Level2...)")]
    public string nextSceneName = "Night-2";

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh khi lên giường ngủ (tiếng chăn gối / thở dài...)")]
    public AudioClip sleepSound;
    public AudioSource localAudioSource;

    private bool isSleeping = false;
    private float currentFadeAlpha = 0f; // Alpha màn hình đen (0 = trong suốt, 1 = đen kịt)
    private Camera mainCam;
    private Transform playerTransform;

    void Awake()
    {
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Start()
    {
        promptMessage = "Go to sleep";

        // Tự động tìm hoặc tạo lieDownPoint nếu chưa gán
        if (lieDownPoint == null)
        {
            Transform foundPoint = transform.Find("LieDownPoint");
            if (foundPoint != null)
            {
                lieDownPoint = foundPoint;
            }
            else
            {
                GameObject autoPoint = new GameObject("LieDownPoint");
                autoPoint.transform.SetParent(transform);
                // Vị trí nằm trên đệm giường tầng dưới
                autoPoint.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                autoPoint.transform.localRotation = Quaternion.identity;
                lieDownPoint = autoPoint.transform;
            }
        }
    }

    public override void Interact()
    {
        if (isSleeping) return;

        isSleeping = true;
        promptMessage = ""; // Ẩn gợi ý tương tác

        StartCoroutine(SleepAndTransitionRoutine());
    }

    private IEnumerator SleepAndTransitionRoutine()
    {
        Debug.Log("[Bed] Bắt đầu quá trình nằm ngủ...");

        mainCam = Camera.main;

        // 1. Tìm Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        if (player != null)
        {
            playerTransform = player.transform;
        }

        // 2. Tắt di chuyển chuột & bàn phím
        DisablePlayerInput();

        // 3. Phát âm thanh chăn gối
        PlaySleepSound();

        // 4. Ẩn HUD gameplay
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.ShowHUD(false);
        }

        // Vô hiệu hóa CharacterController để Player có thể dịch chuyển mượt mà
        CharacterController charController = (player != null) ? player.GetComponent<CharacterController>() : null;
        if (charController != null) charController.enabled = false;

        // 5. Tính toán vị trí Player và Camera
        Vector3 startPlayerPos = (playerTransform != null) ? playerTransform.position : transform.position;
        Quaternion startPlayerRot = (playerTransform != null) ? playerTransform.rotation : transform.rotation;

        Vector3 targetPlayerPos = lieDownPoint.position;
        Quaternion targetPlayerRot = lieDownPoint.rotation;

        // Vị trí Camera trên gối
        Vector3 startCamPos = (mainCam != null) ? mainCam.transform.position : startPlayerPos;
        Quaternion startCamRot = (mainCam != null) ? mainCam.transform.rotation : startPlayerRot;

        Vector3 targetCamPos = lieDownPoint.position + (Vector3.up * 0.2f);
        Quaternion targetCamRot = Quaternion.Euler(lookUpAngle, targetPlayerRot.eulerAngles.y, 0f);

        // 6. Di chuyển Player & Camera mượt mà về phía giường và ngửa lên trần nhà
        float elapsed = 0f;
        while (elapsed < lieDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lieDownDuration);

            // Di chuyển Player
            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, t);
                playerTransform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, t);
            }

            // Di chuyển & xoay Camera ngửa mặt lên trần
            if (mainCam != null)
            {
                mainCam.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
                mainCam.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
            }

            yield return null;
        }

        // Đảm bảo ở đúng vị trí cuối cùng
        if (playerTransform != null)
        {
            playerTransform.position = targetPlayerPos;
            playerTransform.rotation = targetPlayerRot;
        }
        if (mainCam != null)
        {
            mainCam.transform.position = targetCamPos;
            mainCam.transform.rotation = targetCamRot;
        }

        // 7. Nằm ngắm trần nhà trong khoảng thời gian đã cài đặt
        Debug.Log("[Bed] Đang nằm ngắm trần nhà...");
        float waitTimer = 0f;
        while (waitTimer < waitBeforeFade)
        {
            waitTimer += Time.deltaTime;
            if (mainCam != null)
            {
                mainCam.transform.position = targetCamPos;
                mainCam.transform.rotation = targetCamRot;
            }
            yield return null;
        }

        // 8. Bắt đầu mờ đen dần từ từ (Fade to Black: 0% -> 100%)
        Debug.Log("[Bed] Màn hình bắt đầu tối dần...");
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            currentFadeAlpha = Mathf.Clamp01(fadeElapsed / fadeDuration);

            if (mainCam != null)
            {
                mainCam.transform.position = targetCamPos;
                mainCam.transform.rotation = targetCamRot;
            }
            yield return null;
        }

        currentFadeAlpha = 1f;

        // 9. Tắt đèn pin sau khi màn hình đã đen hoàn toàn
        TurnOffFlashlight();

        // 10. Chờ trong bóng tối
        yield return new WaitForSeconds(waitBeforeLoad);

        // 11. Chuyển sang Scene tiếp theo
        Debug.Log($"[Bed] Tải Scene tiếp theo: {nextSceneName}");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[Bed] Chưa điền nextSceneName trong Inspector!");
        }
    }

    private void DisablePlayerInput()
    {
        // Tắt Player Movement
        PlayerController pController = FindObjectOfType<PlayerController>();
        if (pController != null) pController.enabled = false;

        // Tắt PlayerBodyRotator
        PlayerBodyRotator bRotator = FindObjectOfType<PlayerBodyRotator>();
        if (bRotator != null) bRotator.enabled = false;

        // Tắt Player Interact
        PlayerInteract pInteract = FindObjectOfType<PlayerInteract>();
        if (pInteract != null) pInteract.enabled = false;

        // Tắt CinemachineBrain trên Camera để giải phóng Camera tự do di chuyển & xoay
        if (mainCam != null)
        {
            MonoBehaviour brain = mainCam.GetComponent("CinemachineBrain") as MonoBehaviour;
            if (brain != null) brain.enabled = false;
        }
    }

    private void TurnOffFlashlight()
    {
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController != null)
        {
            Light flLight = fController.GetComponentInChildren<Light>();
            if (flLight != null) flLight.enabled = false;
            fController.enabled = false;
        }
    }

    private void PlaySleepSound()
    {
        AudioClip clip = (sleepSound != null) ? sleepSound : interactSound;
        if (clip == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.PlayOneShot(clip);
        }
    }

    // Vẽ màn hình mờ đen bằng OnGUI - 100% không bao giờ bị lỗi Canvas, luôn mượt và chuẩn xác
    void OnGUI()
    {
        if (currentFadeAlpha > 0f)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, currentFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (lieDownPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lieDownPoint.position, 0.25f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lieDownPoint.position + (Vector3.up * 0.2f), 0.15f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(lieDownPoint.position, lieDownPoint.forward * 0.8f);
        }
    }
}
