using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Gan len prefab to bao trong scene.
/// Nguoi choi nhin vao + bam CHUOT PHAI de mo / dong to bao.
/// Khi dong lan dau: hoan thanh task doc bao.
/// </summary>
public class NewspaperInteractable : MonoBehaviour, IInteractable
{
    // ============================================================
    //  CAU HINH NOI DUNG
    // ============================================================
    [Header("=== NOI DUNG TO BAO ===")]
    public string newspaperTitle   = "NHAT BAO VUNG BIEN GIOI";
    public string issueDate        = "Dem thu 1";

    [TextArea(6, 20)]
    public string newspaperContent =
        "TINH TRANG CANH BAO AN NINH MUC DO CAO\n\n" +
        "Chinh quyen dia phuong xac nhan co nhieu dot dot nhap bat thuong " +
        "tai vung bien gioi phia bac trong nhung ngay vua qua.\n\n" +
        "Cac nhan vien bao canh da duoc lenh tang cuong kiem tra giay to " +
        "va bao cao ngay lap tuc neu phat hien di the la hoac hoat dong kha nghi.\n\n" +
        "--- THONG BAO NOI BO ---\n" +
        "Bot canh so 7 can dac biet chu y. Khong duoc phep de bat cu ai " +
        "vao ma khong co giay to hop le.\n\n" +
        "Chuc anh em ca dem canh giac.";

    [Header("=== FONT (BAT BUOC KHI DUNG RUNTIME) ===")]
    [Tooltip("Keo TMP font asset vao day - vi du LiberationSans SDF trong TextMesh Pro/Resources/Fonts & Materials")]
    public TMP_FontAsset fontAsset;

    [Header("=== GIAO DIEN BAO ===")]
    public Color paperColor   = new Color(0.96f, 0.93f, 0.82f, 1f);
    public Color titleColor   = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color contentColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Header("=== AM THANH ===")]
    public AudioClip rustleSound;

    // ============================================================
    //  IInteractable
    // ============================================================
    [Header("=== TUONG TAC ===")]
    [SerializeField] private string _prompt = "Doc bao";
    public string InteractPrompt => _isOpen ? "" : _prompt;

    public void OnLookAt()   { }
    public void OnLookAway() { }

    public void Interact(GameObject interactor)
    {
        if (_isOpen) CloseNewspaper();
        else         OpenNewspaper();
    }

    // ============================================================
    //  TRANG THAI
    // ============================================================
    private bool       _isOpen   = false;
    private bool       _taskDone = false;
    private GameObject _uiRoot;
    private Coroutine  _closeCo;

    // ============================================================
    //  MO / DONG
    // ============================================================
    private void OpenNewspaper()
    {
        if (_isOpen) return;
        _isOpen = true;
        PlayRustle();
        LockLook(true);

        // Lay font truoc khi build
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fontAsset == null)
                fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
                Debug.LogWarning("[Newspaper] Khong tim thay TMP font! Vao Window > TextMeshPro > Import TMP Essential Resources");
        }

        _uiRoot = BuildUI();
        _closeCo = StartCoroutine(WaitForClose());
    }

    private void CloseNewspaper()
    {
        if (!_isOpen) return;
        _isOpen = false;
        PlayRustle();
        LockLook(false);

        if (_uiRoot != null) { Destroy(_uiRoot); _uiRoot = null; }
        if (_closeCo != null) { StopCoroutine(_closeCo); _closeCo = null; }

        if (!_taskDone)
        {
            _taskDone = true;
            _prompt = "";
            if (LocAsset.TaskManager.instance != null)
                LocAsset.TaskManager.instance.CompleteNewspaper();
        }
    }

    // ============================================================
    //  CHO DONG
    // ============================================================
    private IEnumerator WaitForClose()
    {
        yield return new WaitForSeconds(0.2f);
        while (_isOpen)
        {
            bool close = false;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                close = true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                close = true;

            if (close) CloseNewspaper();
            yield return null;
        }
    }

    // ============================================================
    //  BUILD UI TO BAO
    // ============================================================
    private GameObject BuildUI()
    {
        // Canvas nen
        GameObject canvasGO = new GameObject("NewspaperCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Bo toi
        GameObject dimGO = new GameObject("Dim");
        dimGO.transform.SetParent(canvasGO.transform, false);
        Image dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        SetFull(dimGO);

        // Panel chinh
        GameObject paperGO = new GameObject("Paper");
        paperGO.transform.SetParent(canvasGO.transform, false);
        Image paper = paperGO.AddComponent<Image>();
        paper.color = paperColor;
        RectTransform paperRT = paper.rectTransform;
        paperRT.anchorMin        = new Vector2(0.5f, 0.5f);
        paperRT.anchorMax        = new Vector2(0.5f, 0.5f);
        paperRT.pivot            = new Vector2(0.5f, 0.5f);
        paperRT.sizeDelta        = new Vector2(760f, 620f);
        paperRT.anchoredPosition = Vector2.zero;

        // ----- VERTICAL LAYOUT tren paper truc tiep (khong dung ScrollRect/Mask) -----
        VerticalLayoutGroup vlg = paperGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(32, 32, 24, 50);
        vlg.spacing             = 10f;
        vlg.childControlWidth   = vlg.childControlHeight  = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Tieu de
        AddTMP(paperGO.transform, newspaperTitle, 26f, titleColor, FontStyles.Bold, TextAlignmentOptions.Center);

        // Duong ke dam
        AddLine(paperGO.transform, new Color(0.2f, 0.2f, 0.2f, 1f), 2f);

        // Ngay phat hanh
        if (!string.IsNullOrEmpty(issueDate))
            AddTMP(paperGO.transform, issueDate, 14f, new Color(0.4f, 0.35f, 0.2f, 1f), FontStyles.Italic, TextAlignmentOptions.Center);

        // Duong ke mo
        AddLine(paperGO.transform, new Color(0.2f, 0.2f, 0.2f, 0.35f), 1f);

        // Noi dung chinh
        AddTMP(paperGO.transform, newspaperContent, 18f, contentColor, FontStyles.Normal, TextAlignmentOptions.Left);

        // Hint dong (absolute position o day duoi)
        GameObject hintGO = new GameObject("CloseHint");
        hintGO.transform.SetParent(paperGO.transform, false);
        // Phai disable layout element de dat tuyet doi
        LayoutElement hintLE = hintGO.AddComponent<LayoutElement>();
        hintLE.ignoreLayout = true;
        TextMeshProUGUI hint = hintGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) hint.font = fontAsset;
        hint.text      = "[Chuot phai] / [ESC] - Dong bao";
        hint.fontSize  = 14f;
        hint.color     = new Color(0.35f, 0.28f, 0.15f, 0.9f);
        hint.fontStyle = FontStyles.Italic;
        hint.alignment = TextAlignmentOptions.Center;
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin        = new Vector2(0f, 0f);
        hintRT.anchorMax        = new Vector2(1f, 0f);
        hintRT.pivot            = new Vector2(0.5f, 0f);
        hintRT.sizeDelta        = new Vector2(0f, 36f);
        hintRT.anchoredPosition = new Vector2(0f, 8f);

        return canvasGO;
    }

    // ============================================================
    //  HELPERS
    // ============================================================
    private TextMeshProUGUI AddTMP(Transform parent, string text, float size, Color color,
        FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("T");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) t.font = fontAsset;
        t.text              = text;
        t.fontSize          = size;
        t.color             = color;
        t.fontStyle         = style;
        t.alignment         = align;
        t.enableWordWrapping = true;
        LayoutElement le    = go.AddComponent<LayoutElement>();
        le.flexibleWidth    = 1f;
        return t;
    }

    private void AddLine(Transform parent, Color color, float height)
    {
        GameObject go  = new GameObject("Line");
        go.transform.SetParent(parent, false);
        Image img      = go.AddComponent<Image>();
        img.color      = color;
        LayoutElement le        = go.AddComponent<LayoutElement>();
        le.minHeight            = le.preferredHeight = height;
        le.flexibleWidth        = 1f;
    }

    private void SetFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void LockLook(bool lockIt)
    {
        MouseLook ml = FindFirstObjectByType<MouseLook>();
        if (ml != null)
        {
            if (lockIt) ml.UnlockCursor();
            else        ml.LockCursor();
        }
        PlayerInteraction pi = FindFirstObjectByType<PlayerInteraction>();
        if (pi != null) pi.enabled = !lockIt;
    }

    private void PlayRustle()
    {
        if (rustleSound != null)
            AudioSource.PlayClipAtPoint(rustleSound,
                Camera.main != null ? Camera.main.transform.position : transform.position);
    }

    private void OnDestroy()
    {
        if (_isOpen) CloseNewspaper();
    }
}
