using UnityEngine;

/// <summary>
/// Head Bobbing effect for First-Person Camera:
/// Simulates natural head movement while walking or running.
/// Can be toggled On/Off in Gameplay settings (PlayerPrefs: "HeadBobbing").
/// </summary>
public class HeadBobbing : MonoBehaviour
{
    [Header("=== WALKING BOB ===")]
    [Tooltip("Frequency of head bobbing while walking")]
    public float walkBobFrequency = 9f;
    [Tooltip("Amount/distance of head bobbing while walking")]
    public float walkBobAmount = 0.04f;

    [Header("=== SPRINTING BOB ===")]
    [Tooltip("Frequency of head bobbing while sprinting")]
    public float sprintBobFrequency = 13f;
    [Tooltip("Amount/distance of head bobbing while sprinting")]
    public float sprintBobAmount = 0.07f;

    [Header("=== SMOOTHING ===")]
    [Tooltip("Smoothing speed when bobbing")]
    public float bobSmoothing = 12f;
    [Tooltip("Return speed back to neutral position when standing still")]
    public float returnSpeed = 8f;

    [Header("=== PLAYER REFERENCE ===")]
    [SerializeField] private PlayerController playerController;

    private Vector3 defaultLocalPos;
    private float timer = 0f;
    private bool isHeadBobEnabled = true;

    void Start()
    {
        defaultLocalPos = transform.localPosition;

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }
        }

        RefreshSetting();
    }

    void OnEnable()
    {
        RefreshSetting();
    }

    public void RefreshSetting()
    {
        isHeadBobEnabled = PlayerPrefs.GetInt("HeadBobbing", 1) == 1;
    }

    void Update()
    {
        // Don't bob if game is paused
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
            return;
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
            return;

        // Check setting
        isHeadBobEnabled = PlayerPrefs.GetInt("HeadBobbing", 1) == 1;
        if (!isHeadBobEnabled)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos, Time.deltaTime * returnSpeed);
            timer = 0f;
            return;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null) return;
        }

        bool isMoving = playerController.IsMoving;
        bool isGrounded = playerController.IsGrounded;

        if (isMoving && isGrounded && !playerController.isClimbing)
        {
            bool isSprinting = playerController.IsSprinting;
            float frequency = isSprinting ? sprintBobFrequency : walkBobFrequency;
            float amount = isSprinting ? sprintBobAmount : walkBobAmount;

            timer += Time.deltaTime * frequency;

            Vector3 targetPos = defaultLocalPos;
            targetPos.y += Mathf.Sin(timer) * amount;
            targetPos.x += Mathf.Cos(timer * 0.5f) * (amount * 0.6f);

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos, Time.deltaTime * returnSpeed);
        }
    }
}
