using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý HUD gameplay: Thanh thể lực (Stamina Bar) + Icon đèn pin + Đạn súng + Hướng dẫn phím (Controls Hint).
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

    [Header("=== ĐẠN SÚNG (AMMO UI) ===")]
    [Tooltip("Text hiển thị số đạn (nếu để trống script tự động tạo ở góc dưới phải)")]
    public TextMeshProUGUI ammoText;

    [Header("=== HƯỚNG DẪN PHÍM (CONTROLS HINT UI) ===")]
    [Tooltip("Text hiển thị phím tắt đèn pin, súng (nếu để trống script tự động tạo góc dưới phải)")]
    public TextMeshProUGUI controlsHintText;
    [Tooltip("Panel nền của bảng hướng dẫn phím")]
    public GameObject controlsHintPanel;
    [Tooltip("Bật/Tắt bảng hướng dẫn phím")]
    public bool showControlsHint = true;

    [Header("=== TÂM NGẮM / CHẤM TƯƠNG TÁC (CROSSHAIR DOT) ===")]
    [Tooltip("Image hiển thị chấm nhỏ giữa màn hình (nếu để trống script tự động tạo)")]
    public Image crosshairDot;
    [Tooltip("Bật/tắt hiển thị chấm ngắm tâm")]
    public bool showCrosshair = true;
    [Tooltip("Kích thước mặc định của chấm (pixels)")]
    public float defaultDotSize = 5f;
    [Tooltip("Kích thước chấm khi nhìn vào vật tương tác / ngắm NPC")]
    public float interactDotSize = 8f;
    [Tooltip("Màu chấm bình thường")]
    public Color defaultDotColor = new Color(1f, 1f, 1f, 0.75f);
    [Tooltip("Màu chấm khi nhìn vào vật có thể tương tác")]
    public Color interactDotColor = new Color(1f, 0.9f, 0.2f, 0.95f);
    [Tooltip("Màu chấm khi cầm súng ngắm trúng NPC")]
    public Color aimNPCDotColor = new Color(1f, 0.25f, 0.25f, 0.95f);

    [Header("=== THAM CHIẾU SÚNG ===")]
    public PlayerGun playerGun;

    private PlayerInteract playerInteract;

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

        if (playerGun == null)
        {
            playerGun = FindObjectOfType<PlayerGun>(true);
        }

        UpdateFlashlightUI();
        EnsureControlsHintUI();
        UpdateControlsHintUI();
        EnsureCrosshairUI();
        if (playerInteract == null) playerInteract = FindObjectOfType<PlayerInteract>();
    }

    void Update()
    {
        UpdateCrosshairUI();

        if (isPaused) return; // Không xử lý HUD khi đang Pause

        HandleStamina();
        HandleWeaponInput();
    }

    // ===== PHÍM TẮT RÚT / CẤT SÚNG (1 HOẶC LĂN CHUỘT) =====
    private float _nextWeaponInputTime = 0f;

    private void HandleWeaponInput()
    {
        if (Time.time < _nextWeaponInputTime) return;

        if (playerGun == null)
        {
            playerGun = FindObjectOfType<PlayerGun>(true);
            if (playerGun == null) return;
        }

        if (!playerGun.HasGun) return;

        bool togglePressed = false;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            togglePressed = true;
        }
        else if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.05f || Input.mouseScrollDelta.y != 0f)
        {
            togglePressed = true;
        }
        else
        {
            try
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame))
                    togglePressed = true;

                Mouse m = Mouse.current;
                if (m != null && Mathf.Abs(m.scroll.ReadValue().y) > 0.1f)
                    togglePressed = true;
            }
            catch { }
        }

        if (togglePressed)
        {
            _nextWeaponInputTime = Time.time + 0.3f;
            playerGun.ToggleGun();
            UpdateControlsHintUI();
        }
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

    // ===== FLASHLIGHT ICON (đổi màu, phản ánh trạng thái sở hữu & bật/tắt) =====
    public void UpdateFlashlightUI()
    {
        if (flashlightIcon == null) return;

        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController != null && !fController.hasFlashlight)
        {
            // Chưa có đèn pin: Làm tối mờ biểu tượng
            flashlightIcon.color = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            return;
        }

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

        if (ammoText != null)
        {
            if (visible && playerGun != null && !playerGun.IsHolstered)
                ammoText.gameObject.SetActive(true);
            else
                ammoText.gameObject.SetActive(false);
        }

        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(visible);
        else if (controlsHintText != null)
            controlsHintText.gameObject.SetActive(visible);

        if (crosshairDot != null)
            crosshairDot.gameObject.SetActive(visible && showCrosshair);

        if (visible)
        {
            UpdateFlashlightUI();
            UpdateControlsHintUI();
        }
    }

    // ===== AMMO UI (Hiển thị số đạn ở góc dưới phải) =====
    public void UpdateAmmoUI(int current, int max)
    {
        EnsureAmmoUI();
        if (ammoText == null) return;

        ammoText.text = $"AMMO: {current} / {max}";
        ammoText.color = (current > 0) ? new Color(1f, 0.85f, 0.3f, 0.95f) : new Color(0.9f, 0.25f, 0.25f, 0.95f);
        ammoText.gameObject.SetActive(true);
        UpdateControlsHintUI();
    }

    public void OnGunHolstered()
    {
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(false);
        }
        UpdateControlsHintUI();
    }

    private void EnsureAmmoUI()
    {
        if (ammoText != null) return;

        // Tìm trong Scene nếu đã có
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in allTexts)
        {
            if (t.gameObject.name.ToLower().Contains("ammo"))
            {
                ammoText = t;
                return;
            }
        }

        // Tự động tạo AmmoText trên Canvas góc dưới bên phải
        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) targetCanvas = player.GetComponentInChildren<Canvas>(true);
        }
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;

        GameObject ammoGO = new GameObject("AmmoText");
        ammoGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = ammoGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 35f);
        rt.sizeDelta = new Vector2(200f, 40f);

        ammoText = ammoGO.AddComponent<TextMeshProUGUI>();
        ammoText.text = "AMMO: 0 / 6";
        ammoText.fontSize = 24f;
        ammoText.fontStyle = FontStyles.Bold;
        ammoText.alignment = TextAlignmentOptions.BottomRight;
        ammoText.color = new Color(0.9f, 0.25f, 0.25f, 0.95f);

        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont == null) robotoFont = TMP_Settings.defaultFontAsset;
        if (robotoFont != null) ammoText.font = robotoFont;
    }

    // ===== HƯỚNG DẪN PHÍM (CONTROLS HINT UI) =====
    public void EnsureControlsHintUI()
    {
        if (controlsHintText != null)
        {
            controlsHintText.fontSize = 17f;
            if (controlsHintPanel != null)
            {
                RectTransform prt = controlsHintPanel.GetComponent<RectTransform>();
                if (prt != null) prt.sizeDelta = new Vector2(300f, 72f);
            }
            return;
        }

        // Tìm trong Scene nếu đã có
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in allTexts)
        {
            if (t.gameObject.name.ToLower().Contains("controlshint") || t.gameObject.name.ToLower().Contains("hinttext"))
            {
                controlsHintText = t;
                controlsHintText.fontSize = 17f;
                if (t.transform.parent != null && t.transform.parent.name.Contains("Panel"))
                {
                    controlsHintPanel = t.transform.parent.gameObject;
                    RectTransform prt = controlsHintPanel.GetComponent<RectTransform>();
                    if (prt != null) prt.sizeDelta = new Vector2(300f, 72f);
                }
                return;
            }
        }

        // Tìm Canvas hiển thị HUD
        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) targetCanvas = player.GetComponentInChildren<Canvas>(true);
        }
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;

        // Tạo Panel nền mờ trang nhã ở góc dưới bên phải (ngay trên AmmoText)
        GameObject panelGO = new GameObject("ControlsHintPanel");
        panelGO.transform.SetParent(targetCanvas.transform, false);
        controlsHintPanel = panelGO;

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot = new Vector2(1f, 0f);
        panelRT.anchoredPosition = new Vector2(-40f, 85f);
        panelRT.sizeDelta = new Vector2(300f, 72f);

        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.05f, 0.55f);

        // Tạo Text bên trong Panel
        GameObject textGO = new GameObject("ControlsHintText");
        textGO.transform.SetParent(panelGO.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = new Vector2(-20f, -10f); // padding viền
        textRT.anchoredPosition = Vector2.zero;

        controlsHintText = textGO.AddComponent<TextMeshProUGUI>();
        controlsHintText.fontSize = 17f;
        controlsHintText.alignment = TextAlignmentOptions.MidlineRight;
        controlsHintText.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
        controlsHintText.lineSpacing = 14f;

        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont == null) robotoFont = TMP_Settings.defaultFontAsset;
        if (robotoFont != null) controlsHintText.font = robotoFont;
    }

    /// <summary>
    /// Cập nhật nội dung hướng dẫn phím dựa trên trang bị người chơi sở hữu (Tiếng Anh)
    /// </summary>
    public void UpdateControlsHintUI()
    {
        if (!showControlsHint)
        {
            if (controlsHintPanel != null) controlsHintPanel.SetActive(false);
            else if (controlsHintText != null) controlsHintText.gameObject.SetActive(false);
            return;
        }

        EnsureControlsHintUI();
        if (controlsHintText == null) return;

        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (playerGun == null) playerGun = FindObjectOfType<PlayerGun>(true);

        bool hasFlashlight = (fController != null && fController.hasFlashlight);
        bool hasGun = (playerGun != null && playerGun.HasGun);

        // Nếu chưa nhặt cả 2 món thì ẩn bảng hướng dẫn để màn hình gọn gàng
        if (!hasFlashlight && !hasGun)
        {
            if (controlsHintPanel != null) controlsHintPanel.SetActive(false);
            else controlsHintText.gameObject.SetActive(false);
            return;
        }

        if (controlsHintPanel != null) controlsHintPanel.SetActive(true);
        controlsHintText.gameObject.SetActive(true);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (hasFlashlight)
        {
            bool isFlashlightOn = (flashlightLight != null && flashlightLight.enabled);
            string flAction = isFlashlightOn ? "Turn Off Flashlight" : "Turn On Flashlight";
            sb.AppendLine($"<color=#FFDD44>[F]</color> {flAction}");
        }

        if (hasGun)
        {
            bool isHolstered = (playerGun != null && playerGun.IsHolstered);
            string gunAction = isHolstered ? "Draw Gun" : "Holster Gun";
            sb.Append($"<color=#FFDD44>[1 / Scroll]</color> {gunAction}");
        }

        controlsHintText.text = sb.ToString().TrimEnd();
    }

    // ===== TÂM NGẮM / CHẤM TƯƠNG TÁC (CROSSHAIR DOT) =====
    public void EnsureCrosshairUI()
    {
        if (crosshairDot != null) return;

        // Tìm xem trên Canvas đã có Crosshair / Reticle chưa
        Image[] allImages = FindObjectsOfType<Image>(true);
        foreach (var img in allImages)
        {
            string n = img.gameObject.name.ToLower();
            if (n.Contains("crosshair") || n.Contains("reticle") || n == "dot")
            {
                crosshairDot = img;
                return;
            }
        }

        // Tự động tạo Crosshair Dot ngay chính giữa Canvas
        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) targetCanvas = player.GetComponentInChildren<Canvas>(true);
        }
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;

        GameObject dotGO = new GameObject("CrosshairDot");
        dotGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = dotGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(defaultDotSize, defaultDotSize);

        crosshairDot = dotGO.AddComponent<Image>();
        crosshairDot.sprite = CreateCircleSprite(32);
        crosshairDot.color = defaultDotColor;
        crosshairDot.raycastTarget = false; // Không cản trở Raycast tương tác chuột
    }

    public void UpdateCrosshairUI()
    {
        if (crosshairDot == null) return;

        bool shouldShow = showCrosshair && !isPaused &&
                          (PauseMenuController.instance == null || !PauseMenuController.instance.isPaused);

        if (crosshairDot.gameObject.activeSelf != shouldShow)
        {
            crosshairDot.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow) return;

        if (playerInteract == null) playerInteract = FindObjectOfType<PlayerInteract>();
        if (playerGun == null) playerGun = FindObjectOfType<PlayerGun>(true);

        // NẾU ĐANG CHĨA / NHÌN VÀO VẬT THỂ TƯƠNG TÁC:
        // Ẩn chấm tâm đi theo yêu cầu, nhường chỗ cho dòng thông báo [E] nằm bên trái
        if (playerInteract != null && playerInteract.isLookingAtInteractable)
        {
            if (crosshairDot.gameObject.activeSelf) crosshairDot.gameObject.SetActive(false);
            return;
        }

        // Đảm bảo hiện lại chấm khi không nhìn vào vật tương tác
        if (!crosshairDot.gameObject.activeSelf)
        {
            crosshairDot.gameObject.SetActive(true);
        }

        // 1. Khi cầm súng ngắm trúng NPC / Quái vật -> Đổi màu đỏ + phóng to nhẹ để ngắm bắn chuẩn
        if (playerGun != null && !playerGun.IsHolstered && playerGun.IsAimingAtNPC)
        {
            crosshairDot.color = aimNPCDotColor;
            crosshairDot.rectTransform.sizeDelta = new Vector2(interactDotSize, interactDotSize);
        }
        // 2. Trạng thái bình thường -> Chấm tròn trắng thanh mảnh tinh tế
        else
        {
            crosshairDot.color = defaultDotColor;
            crosshairDot.rectTransform.sizeDelta = new Vector2(defaultDotSize, defaultDotSize);
        }
    }

    /// <summary>
    /// Tạo Sprite hình tròn mịn chống răng cưa (Anti-Aliased Circle) cho tâm ngắm
    /// </summary>
    private Sprite CreateCircleSprite(int size = 32)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2f;
        float radius = center - 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // Làm mờ viền ngoài tạo cảm giác tròn xoe mịn màng
                    float alpha = Mathf.Clamp01(radius - dist + 0.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}