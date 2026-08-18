using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý HUD gameplay: Thanh thể lực + Icon đèn pin.
/// Phím ESC và Pause Menu do PauseMenuController xử lý.
/// </summary>
public class PlayerHUDManager : MonoBehaviour
{
    public static PlayerHUDManager instance;

    [Header("=== THỂ LỰC (STAMINA) ===")]
    public Slider staminaSlider;
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 18f;
    public float currentStamina;

    [Header("=== ĐÈN PIN (FLASHLIGHT UI) ===")]
    public Image flashlightIcon;
    public Light flashlightLight;
    private Color iconOn  = Color.yellow;
    private Color iconOff = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    // isPaused được PauseMenuController set để PlayerHUDManager biết ẩn HUD
    [HideInInspector] public bool isPaused = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value    = currentStamina;
            staminaSlider.gameObject.SetActive(false); // Ẩn khi đầy
        }

        if (flashlightLight == null)
            flashlightLight = FindObjectOfType<Light>();

        UpdateFlashlightUI();
    }

    void Update()
    {
        if (isPaused) return; // Không xử lý HUD khi đang Pause

        HandleStamina();
    }

    // ===== STAMINA =====
    private void HandleStamina()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving    = (h != 0 || v != 0);
        bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0) currentStamina = 0;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
            // Hiện khi chưa đầy, ẩn khi đầy 100%
            staminaSlider.gameObject.SetActive(currentStamina < maxStamina);
        }
    }

    // ===== FLASHLIGHT ICON (đổi màu, luôn hiện) =====
    public void UpdateFlashlightUI()
    {
        if (flashlightIcon == null) return;
        bool isOn = (flashlightLight != null && flashlightLight.enabled);
        flashlightIcon.color = isOn ? iconOn : iconOff;
    }

    // ===== ẨN / HIỆN HUD KHI PAUSE =====
    public void ShowHUD(bool visible)
    {
        // Stamina: chỉ hiện khi visible VÀ chưa đầy
        if (staminaSlider != null)
            staminaSlider.gameObject.SetActive(visible && currentStamina < maxStamina);

        if (flashlightIcon != null)
            flashlightIcon.gameObject.SetActive(visible);

        if (visible) UpdateFlashlightUI();
    }
}