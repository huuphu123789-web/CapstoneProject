using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý HUD gameplay: Thanh thể lực (Stamina Bar) + Icon đèn pin.
/// Tự động tìm hoặc tạo thanh Thể lực đẹp mắt nếu chưa có trên Canvas.
/// Phím ESC và Pause Menu do PauseMenuController xử lý.
/// </summary>
public class PlayerHUDManager : MonoBehaviour
{
    public static PlayerHUDManager instance;

    [Header("=== THỂ LỰC (STAMINA) ===")]
    [Tooltip("Kéo Slider thể lực vào đây (nếu để trống script tự động tạo góc dưới trái)")]
    public Slider staminaSlider;
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 18f;
    public float currentStamina;

    [Tooltip("Ngưỡng thể lực tối thiểu (%) cần hồi phục sau khi kiệt sức mới được chạy lại (tránh bị giật animation)")]
    public float minStaminaToSprint = 15f;

    [HideInInspector] public bool isExhausted = false; // Đang kiệt sức

    [Tooltip("Luôn hiện thanh thể lực trên màn hình (Bật = luôn hiện, Tắt = chỉ hiện khi chạy/hồi)")]
    public bool alwaysShowStamina = true;

    [Header("=== ĐÈN PIN (FLASHLIGHT UI) ===")]
    public Image flashlightIcon;
    public Light flashlightLight;
    private Color iconOn  = Color.yellow;
    private Color iconOff = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    // isPaused được PauseMenuController set để PlayerHUDManager biết ẩn HUD
    [HideInInspector] public bool isPaused = false;

    /// <summary>
    /// Kiểm tra người chơi có thể chạy nhanh hay không (chống giật loop animation khi hết thể lực)
    /// </summary>
    public bool CanSprint()
    {
        if (isExhausted) return false;
        return currentStamina > 0f;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;

        // Tự động tìm hoặc tạo UI thanh thể lực nếu chưa có
        EnsureStaminaUI();

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value    = currentStamina;
            staminaSlider.gameObject.SetActive(alwaysShowStamina);
        }

        if (flashlightLight == null)
        {
            FlashlightController fController = FindObjectOfType<FlashlightController>();
            if (fController != null)
                flashlightLight = fController.GetComponentInChildren<Light>();
            if (flashlightLight == null)
                flashlightLight = GetComponentInChildren<Light>();
        }

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
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift);

        // Kiểm tra và giải phóng trạng thái kiệt sức khi thể lực hồi phục đủ ngưỡng
        if (isExhausted)
        {
            if (currentStamina >= minStaminaToSprint)
            {
                isExhausted = false;
            }
        }

        // Đang thực sự chạy: di chuyển + giữ Shift + không kiệt sức + thể lực > 0
        bool isSprinting = isMoving && isHoldingShift && !isExhausted && (currentStamina > 0f);

        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true; // Chạm 0 -> Vào trạng thái kiệt sức, chuyển ngay về đi bộ
            }
        }
        else
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;

            if (alwaysShowStamina)
            {
                if (!staminaSlider.gameObject.activeSelf)
                    staminaSlider.gameObject.SetActive(true);
            }
            else
            {
                // Hiện khi chưa đầy hoặc đang chạy
                bool shouldShow = (currentStamina < maxStamina) || isSprinting;
                if (staminaSlider.gameObject.activeSelf != shouldShow)
                    staminaSlider.gameObject.SetActive(shouldShow);
            }
        }
    }

    // ===== TỰ ĐỘNG TÌM HOẶC TẠO GIAO DIỆN THANH THỂ LỰC =====
    private void EnsureStaminaUI()
    {
        // 1. Nếu đã có staminaSlider thì xong
        if (staminaSlider != null) return;

        // 2. Tìm trong các con của Canvas hoặc Scene có Slider tên "stamina"
        Slider[] allSliders = FindObjectsOfType<Slider>(true);
        foreach (var s in allSliders)
        {
            if (s.gameObject.name.ToLower().Contains("stamina"))
            {
                staminaSlider = s;
                staminaSlider.gameObject.SetActive(true);
                return;
            }
        }

        // 3. Tìm Canvas hiển thị HUD
        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            // Thử tìm Canvas trên Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                targetCanvas = player.GetComponentInChildren<Canvas>(true);
            }
        }
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        // 4. Nếu không có Canvas nào, tạo mới Canvas
        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("PlayerHUD_Canvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 90;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            targetCanvas.gameObject.SetActive(true);
        }

        // 5. Tự động tạo Thanh Thể Lực (Slider) góc dưới bên trái
        GameObject sliderGO = new GameObject("StaminaSlider");
        sliderGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 0f);
        sliderRT.anchorMax = new Vector2(0f, 0f);
        sliderRT.pivot = new Vector2(0f, 0f);
        sliderRT.anchoredPosition = new Vector2(35f, 35f);
        sliderRT.sizeDelta = new Vector2(220f, 18f);

        // Nền thanh thể lực (Background)
        Image bgImage = sliderGO.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);

        // Vùng chứa ruột thanh (Fill Area)
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = new Vector2(-4f, -4f); // Padding 2px
        fillAreaRT.anchoredPosition = Vector2.zero;

        // Ruột thanh thể lực (Fill)
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.82f, 0.45f, 0.95f); // Xanh lá thể lực nổi bật

        // Component Slider
        Slider newSlider = sliderGO.AddComponent<Slider>();
        newSlider.fillRect = fillRT;
        newSlider.targetGraphic = fillImage;
        newSlider.direction = Slider.Direction.LeftToRight;
        newSlider.minValue = 0f;
        newSlider.maxValue = maxStamina;
        newSlider.value = currentStamina;
        newSlider.interactable = false;

        // Chữ nhãn "STAMINA"
        GameObject labelGO = new GameObject("StaminaLabel");
        labelGO.transform.SetParent(sliderGO.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 1f);
        labelRT.anchorMax = new Vector2(0f, 1f);
        labelRT.pivot = new Vector2(0f, 0f);
        labelRT.anchoredPosition = new Vector2(0f, 4f);
        labelRT.sizeDelta = new Vector2(120f, 18f);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = "STAMINA";
        labelText.fontSize = 13f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
        labelText.alignment = TextAlignmentOptions.BottomLeft;

        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont == null) robotoFont = TMP_Settings.defaultFontAsset;
        if (robotoFont != null) labelText.font = robotoFont;

        staminaSlider = newSlider;
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
        if (staminaSlider != null)
        {
            if (visible)
            {
                staminaSlider.gameObject.SetActive(alwaysShowStamina || currentStamina < maxStamina);
            }
            else
            {
                staminaSlider.gameObject.SetActive(false);
            }
        }

        if (flashlightIcon != null)
            flashlightIcon.gameObject.SetActive(visible);

        if (visible) UpdateFlashlightUI();
    }
}