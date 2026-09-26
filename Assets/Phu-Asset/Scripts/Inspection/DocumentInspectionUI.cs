using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện soi chiếu giấy tờ: CCCD (Căn cước công dân / quân nhân) 
/// và Giấy xác nhận đầu hàng (Surrender Confirm / Giấy thông hành).
/// Tự động sinh UI nếu chưa có sẵn trên Canvas.
/// </summary>
public class DocumentInspectionUI : MonoBehaviour
{
    public static DocumentInspectionUI instance;

    [Header("=== GIAO DIỆN CHÍNH ===")]
    public GameObject inspectionPanel;
    public RawImage cccdImage;
    public RawImage surrenderImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI hintText;

    [Header("=== NÚT QUYẾT ĐỊNH TRÊN UI (TÙY CHỌN) ===")]
    public Button approveButton;
    public Button rejectButton;
    public Button closeButton;

    [HideInInspector] public bool isOpen = false;

    private CursorLockMode previousLockMode = CursorLockMode.Locked;
    private bool previousCursorVisible = false;
    private int openFrame = -1;

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
        EnsureUI();
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isOpen) return;
        if (Time.frameCount == openFrame) return; // Bỏ qua frame vừa mở để tránh trùng phím E

        // Bấm E hoặc ESC để đóng giấy tờ
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            CloseInspection();
        }
    }

    /// <summary>
    /// Mở bảng kiểm tra giấy tờ với 2 Texture CCCD và Giấy xác nhận đầu hàng
    /// </summary>
    public void OpenInspection(Texture cccdTex, Texture surrenderTex, string npcName = "")
    {
        EnsureUI();

        isOpen = true;
        openFrame = Time.frameCount;

        if (cccdImage != null)
        {
            cccdImage.texture = cccdTex;
            cccdImage.gameObject.SetActive(cccdTex != null);
        }

        if (surrenderImage != null)
        {
            surrenderImage.texture = surrenderTex;
            surrenderImage.gameObject.SetActive(surrenderTex != null);
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(npcName)
                ? "ENTRY DOCUMENTS INSPECTION"
                : $"ENTRY DOCUMENTS - {npcName.ToUpper()}";
        }

        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(true);
        }

        // Mở khóa chuột để người chơi có thể tương tác hoặc đọc giấy tờ
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Thông báo cho HUD tạm dừng nhận lệnh di chuột
        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.isPaused = true;
        }
    }

    /// <summary>
    /// Đóng bảng giấy tờ để quan sát NPC bên ngoài cửa sổ bốt gác
    /// </summary>
    public void CloseInspection()
    {
        if (!isOpen) return;
        isOpen = false;

        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }

        // Khôi phục con trỏ chuột cho FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerHUDManager.instance != null)
        {
            PlayerHUDManager.instance.isPaused = false;
        }
    }

    private void OnApproveClicked()
    {
        CloseInspection();
        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.ApproveCurrentNPC();
        }
    }

    private void OnRejectClicked()
    {
        CloseInspection();
        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.RejectCurrentNPC();
        }
    }

    /// <summary>
    /// Tự động khởi tạo cấu trúc UI chuyên nghiệp nếu trong Scene chưa có
    /// </summary>
    public void EnsureUI()
    {
        if (inspectionPanel != null) return;

        // 1. Tìm Canvas
        Canvas targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("InspectionCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 95;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont == null) robotoFont = TMP_Settings.defaultFontAsset;

        // 2. Tạo Panel bao bọc toàn màn hình với nền tối mờ
        inspectionPanel = new GameObject("DocumentInspectionPanel");
        inspectionPanel.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRT = inspectionPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = inspectionPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.85f);

        // 3. Header Tiêu đề
        GameObject titleGO = new GameObject("InspectionTitle");
        titleGO.transform.SetParent(inspectionPanel.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -40f);
        titleRT.sizeDelta = new Vector2(800f, 50f);

        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "ENTRY DOCUMENTS INSPECTION";
        titleText.fontSize = 28f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.88f, 0.35f, 1f);
        if (robotoFont != null) titleText.font = robotoFont;

        // 4. Khung hiển thị 2 tài liệu (CCCD bên trái, Surrender bên phải)
        GameObject docsContainer = new GameObject("DocumentsContainer");
        docsContainer.transform.SetParent(inspectionPanel.transform, false);
        RectTransform docsRT = docsContainer.AddComponent<RectTransform>();
        docsRT.anchorMin = new Vector2(0.5f, 0.5f);
        docsRT.anchorMax = new Vector2(0.5f, 0.5f);
        docsRT.pivot = new Vector2(0.5f, 0.5f);
        docsRT.anchoredPosition = new Vector2(0f, 20f);
        docsRT.sizeDelta = new Vector2(1200f, 620f);

        // --- Khung CCCD (Trái) ---
        GameObject cccdFrame = new GameObject("CCCD_Frame");
        cccdFrame.transform.SetParent(docsContainer.transform, false);
        RectTransform cccdFrameRT = cccdFrame.AddComponent<RectTransform>();
        cccdFrameRT.anchorMin = new Vector2(0.26f, 0.5f);
        cccdFrameRT.anchorMax = new Vector2(0.26f, 0.5f);
        cccdFrameRT.pivot = new Vector2(0.5f, 0.5f);
        cccdFrameRT.anchoredPosition = Vector2.zero;
        cccdFrameRT.sizeDelta = new Vector2(500f, 550f);

        Image cccdBorder = cccdFrame.AddComponent<Image>();
        cccdBorder.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GameObject cccdLabelGO = new GameObject("CCCD_Label");
        cccdLabelGO.transform.SetParent(cccdFrame.transform, false);
        RectTransform cccdLabelRT = cccdLabelGO.AddComponent<RectTransform>();
        cccdLabelRT.anchorMin = new Vector2(0.5f, 1f);
        cccdLabelRT.anchorMax = new Vector2(0.5f, 1f);
        cccdLabelRT.pivot = new Vector2(0.5f, 0f);
        cccdLabelRT.anchoredPosition = new Vector2(0f, 8f);
        cccdLabelRT.sizeDelta = new Vector2(400f, 30f);
        TextMeshProUGUI cccdLabel = cccdLabelGO.AddComponent<TextMeshProUGUI>();
        cccdLabel.text = "NATIONAL ID CARD";
        cccdLabel.fontSize = 18f;
        cccdLabel.fontStyle = FontStyles.Bold;
        cccdLabel.alignment = TextAlignmentOptions.Center;
        cccdLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        if (robotoFont != null) cccdLabel.font = robotoFont;

        GameObject cccdImgGO = new GameObject("CCCD_RawImage");
        cccdImgGO.transform.SetParent(cccdFrame.transform, false);
        RectTransform cccdImgRT = cccdImgGO.AddComponent<RectTransform>();
        cccdImgRT.anchorMin = Vector2.zero;
        cccdImgRT.anchorMax = Vector2.one;
        cccdImgRT.sizeDelta = new Vector2(-16f, -16f);
        cccdImgRT.anchoredPosition = Vector2.zero;
        cccdImage = cccdImgGO.AddComponent<RawImage>();

        // --- Khung Surrender Confirm (Phải) ---
        GameObject surrFrame = new GameObject("Surrender_Frame");
        surrFrame.transform.SetParent(docsContainer.transform, false);
        RectTransform surrFrameRT = surrFrame.AddComponent<RectTransform>();
        surrFrameRT.anchorMin = new Vector2(0.74f, 0.5f);
        surrFrameRT.anchorMax = new Vector2(0.74f, 0.5f);
        surrFrameRT.pivot = new Vector2(0.5f, 0.5f);
        surrFrameRT.anchoredPosition = Vector2.zero;
        surrFrameRT.sizeDelta = new Vector2(500f, 550f);

        Image surrBorder = surrFrame.AddComponent<Image>();
        surrBorder.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GameObject surrLabelGO = new GameObject("Surrender_Label");
        surrLabelGO.transform.SetParent(surrFrame.transform, false);
        RectTransform surrLabelRT = surrLabelGO.AddComponent<RectTransform>();
        surrLabelRT.anchorMin = new Vector2(0.5f, 1f);
        surrLabelRT.anchorMax = new Vector2(0.5f, 1f);
        surrLabelRT.pivot = new Vector2(0.5f, 0f);
        surrLabelRT.anchoredPosition = new Vector2(0f, 8f);
        surrLabelRT.sizeDelta = new Vector2(400f, 30f);
        TextMeshProUGUI surrLabel = surrLabelGO.AddComponent<TextMeshProUGUI>();
        surrLabel.text = "SURRENDER CONFIRMATION";
        surrLabel.fontSize = 18f;
        surrLabel.fontStyle = FontStyles.Bold;
        surrLabel.alignment = TextAlignmentOptions.Center;
        surrLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        if (robotoFont != null) surrLabel.font = robotoFont;

        GameObject surrImgGO = new GameObject("Surrender_RawImage");
        surrImgGO.transform.SetParent(surrFrame.transform, false);
        RectTransform surrImgRT = surrImgGO.AddComponent<RectTransform>();
        surrImgRT.anchorMin = Vector2.zero;
        surrImgRT.anchorMax = Vector2.one;
        surrImgRT.sizeDelta = new Vector2(-16f, -16f);
        surrImgRT.anchoredPosition = Vector2.zero;
        surrenderImage = surrImgGO.AddComponent<RawImage>();

        // 5. Thanh điều khiển dưới cùng (Nút Cho qua, Từ chối, Đóng)
        GameObject buttonsBar = new GameObject("InspectionButtonsBar");
        buttonsBar.transform.SetParent(inspectionPanel.transform, false);
        RectTransform barRT = buttonsBar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.5f, 0f);
        barRT.anchorMax = new Vector2(0.5f, 0f);
        barRT.pivot = new Vector2(0.5f, 0f);
        barRT.anchoredPosition = new Vector2(0f, 40f);
        barRT.sizeDelta = new Vector2(900f, 60f);

        // Nút Cho qua (Xanh)
        approveButton = CreateButton(buttonsBar.transform, "ApproveBtn", new Vector2(-280f, 0f), new Vector2(220f, 50f),
            new Color(0.12f, 0.65f, 0.28f, 0.95f), "APPROVE", robotoFont, OnApproveClicked);

        // Nút Đóng giấy tờ (Xám)
        closeButton = CreateButton(buttonsBar.transform, "CloseBtn", new Vector2(0f, 0f), new Vector2(240f, 50f),
            new Color(0.3f, 0.3f, 0.3f, 0.9f), "[ESC / E] CLOSE", robotoFont, CloseInspection);

        // Nút Từ chối (Đỏ)
        rejectButton = CreateButton(buttonsBar.transform, "RejectBtn", new Vector2(280f, 0f), new Vector2(220f, 50f),
            new Color(0.8f, 0.2f, 0.2f, 0.95f), "REJECT", robotoFont, OnRejectClicked);

        // 6. Gợi ý phím tắt
        GameObject hintGO = new GameObject("InspectionHint");
        hintGO.transform.SetParent(inspectionPanel.transform, false);
        RectTransform hintRT = hintGO.AddComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.5f, 0f);
        hintRT.anchorMax = new Vector2(0.5f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 15f);
        hintRT.sizeDelta = new Vector2(800f, 25f);
        hintText = hintGO.AddComponent<TextMeshProUGUI>();
        hintText.text = "Tip: Close documents to observe NPC behavior outside the booth window.";
        hintText.fontSize = 14f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(0.75f, 0.75f, 0.75f, 0.8f);
        if (robotoFont != null) hintText.font = robotoFont;
    }

    private Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, Color color, string label, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnGO.AddComponent<Image>();
        img.color = color;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 16f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        if (font != null) txt.font = font;

        if (action != null)
        {
            btn.onClick.AddListener(action);
        }

        return btn;
    }
}
