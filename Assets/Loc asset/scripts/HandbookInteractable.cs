using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Gáº¯n lÃªn prefab handbook_3d trong scene.
/// NgÆ°á»i chÆ¡i nhÃ¬n vÃ o + báº¥m CHUá»˜T PHáº¢I Ä‘á»ƒ má»Ÿ / Ä‘Ã³ng sá»• tay.
/// Khi má»Ÿ: hiá»‡n UI 2 trang (trÃ¡i / pháº£i) vá»›i ná»™i dung hÆ°á»›ng dáº«n.
/// Láº­t trang báº±ng A/D hoáº·c mÅ©i tÃªn trÃ¡i/pháº£i.
/// ÄÃ³ng báº±ng ESC hoáº·c chuá»™t pháº£i.
/// </summary>
public class HandbookInteractable : MonoBehaviour, IInteractable
{
    // ============================================================
    //  Dá»® LIá»†U TRANG
    // ============================================================
    [System.Serializable]
    public class HandbookPage
    {
        public string pageTitle = "TiÃªu Ä‘á»";
        [TextArea(4, 16)]
        public string pageContent = "Ná»™i dung trang...";
    }

    [Header("=== Ná»˜I DUNG Sá»” TAY ===")]
    public string handbookTitle = "Sá»” TAY CHIáº¾N SÄ¨";
    public List<HandbookPage> pages = new List<HandbookPage>
    {
        new HandbookPage
        {
            pageTitle   = "NHIá»†M Vá»¤ & QUY Táº®C",
            pageContent =
                "1. LuÃ´n tuÃ¢n thá»§ lá»‡nh chá»‰ huy.\n" +
                "2. KhÃ´ng Ä‘á»ƒ báº¥t ká»³ ai qua tráº¡m\n" +
                "   khi chÆ°a kiá»ƒm tra giáº¥y tá».\n" +
                "3. BÃ¡o cÃ¡o ngay má»i hoáº¡t Ä‘á»™ng\n" +
                "   kháº£ nghi qua bá»™ Ä‘Ã m.\n" +
                "4. KhÃ´ng Ä‘Æ°á»£c rá»i vá»‹ trÃ­ khi\n" +
                "   chÆ°a cÃ³ ngÆ°á»i thay ca.\n" +
                "5. VÅ© khÃ­ luÃ´n trong tÃ¬nh tráº¡ng\n" +
                "   sáºµn sÃ ng chiáº¿n Ä‘áº¥u."
        },
        new HandbookPage
        {
            pageTitle   = "KHU Vá»°C & Vá»Š TRÃ",
            pageContent =
                "â–º Khu A  â€“ Cá»•ng chÃ­nh\n" +
                "â–º Khu B  â€“ Kho vÅ© khÃ­\n" +
                "â–º Khu C  â€“ Tráº¡i giam giá»¯\n" +
                "â–º Khu D  â€“ ÄÃ i quan sÃ¡t\n\n" +
                "âš  Khu vá»±c C vÃ  D\n" +
                "   CHá»ˆ dÃ nh cho cáº¥p trÃªn.\n" +
                "   KhÃ´ng Ä‘Æ°á»£c tiáº¿p cáº­n\n" +
                "   khi chÆ°a cÃ³ lá»‡nh."
        },
        new HandbookPage
        {
            pageTitle   = "TÃN HIá»†U KHáº¨N Cáº¤P",
            pageContent =
                "ðŸ”´ Äá»Ž   â€“ BÃ¡o Ä‘á»™ng táº¥n cÃ´ng\n" +
                "ðŸŸ¡ VÃ€NG â€“ Cáº£nh bÃ¡o xÃ¢m nháº­p\n" +
                "ðŸŸ¢ XANH â€“ Tráº¡ng thÃ¡i bÃ¬nh thÆ°á»ng\n\n" +
                "Khi nghe cÃ²i 3 tiáº¿ng:\n" +
                "â†’ Vá» vá»‹ trÃ­ phÃ²ng thá»§ ngay!\n\n" +
                "Khi nghe cÃ²i liÃªn tá»¥c:\n" +
                "â†’ Di táº£n theo lá»™ trÃ¬nh B."
        },
        new HandbookPage
        {
            pageTitle   = "LIÃŠN Láº C & BÃO CÃO",
            pageContent =
                "Táº§n sá»‘ bá»™ Ä‘Ã m chÃ­nh: 87.5 MHz\n" +
                "Táº§n sá»‘ dá»± phÃ²ng  : 102.3 MHz\n\n" +
                "BÃ¡o cÃ¡o ca: má»—i 2 giá» má»™t láº§n.\n\n" +
                "Máº­t kháº©u Ä‘Ãªm nay:\n" +
                "\"BIÃŠN CÆ¯Æ NG\"\n\n" +
                "Pháº£n há»“i máº­t kháº©u:\n" +
                "\"Tá»” QUá»C\""
        },
    };

    [Header("=== GIAO DIá»†N ===")]
    public Color coverColor   = new Color(0.22f, 0.28f, 0.18f, 1f);
    public Color paperColor   = new Color(0.93f, 0.89f, 0.78f, 1f);
    public Color titleColor   = new Color(0.12f, 0.08f, 0.04f, 1f);
    public Color contentColor = new Color(0.15f, 0.12f, 0.08f, 1f);
    public Color accentColor  = new Color(0.55f, 0.12f, 0.08f, 1f);   // Ä‘á» quÃ¢n sá»±

    [Header("=== FONT (kÃ©o TMP Font vÃ o Ä‘Ã¢y) ===")]
    public TMP_FontAsset fontAsset;

    [Header("=== Ã‚M THANH ===")]
    public AudioClip flipSound;
    public AudioClip openSound;

    // ============================================================
    //  IInteractable
    // ============================================================
    [Header("=== TÆ¯Æ NG TÃC ===")]
    [SerializeField] private string _prompt = "Xem sá»• tay";
    public string InteractPrompt => _isOpen ? "" : _prompt;

        // ============================================================
    //  AUTO-SETUP COLLIDER & HIGHLIGHT
    // ============================================================
    private Renderer[] _renderers;
    private Color[] _origColors;
    private bool _isHighlighted;

    private void Awake()
    {
        // Tu dong dam bao co Collider tren sach hoac tren cac part con
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            // Tinh bounds tu cac Renderer con neu co
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                bc.center = transform.InverseTransformPoint(b.center);
                bc.size = transform.InverseTransformVector(b.size);
            }
            else
            {
                bc.size = new Vector3(0.3f, 0.08f, 0.4f);
            }
        }

        _renderers = GetComponentsInChildren<Renderer>();
        if (_renderers != null && _renderers.Length > 0)
        {
            _origColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_Color"))
                    _origColors[i] = _renderers[i].material.color;
            }
        }
    }

    public void OnLookAt()
    {
        if (_isHighlighted || _isOpen || _renderers == null) return;
        _isHighlighted = true;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material.HasProperty("_Color"))
                _renderers[i].material.color = new Color(1f, 0.9f, 0.5f, 1f);
        }
    }

    public void OnLookAway()
    {
        if (!_isHighlighted || _renderers == null) return;
        _isHighlighted = false;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _origColors != null && i < _origColors.Length && _renderers[i].material.HasProperty("_Color"))
                _renderers[i].material.color = _origColors[i];
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_isOpen) CloseHandbook();
        else         OpenHandbook();
    }

    // ============================================================
    //  TRáº NG THÃI
    // ============================================================
    private bool       _isOpen     = false;
    private int        _spreadIndex = 0;   // má»—i spread = 2 trang liÃªn tiáº¿p
    private int        SpreadCount  => Mathf.CeilToInt(pages.Count / 2f);

    private GameObject _uiRoot;
    private Coroutine  _inputCo;

    // UI refs Ä‘á»ƒ update khi láº­t trang
    private TextMeshProUGUI _leftTitle, _leftContent, _leftPageNum;
    private TextMeshProUGUI _rightTitle, _rightContent, _rightPageNum;
    private TextMeshProUGUI _spreadLabel;
    private Button _btnPrev, _btnNext;

    // ============================================================
    //  Má»ž / ÄÃ“NG
    // ============================================================
    private void OpenHandbook()
    {
        if (_isOpen) return;
        _isOpen = true;
        _spreadIndex = 0;

        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;
        }

        PlaySFX(openSound);
        LockLook(true);
        _uiRoot = BuildUI();
        RefreshPages();
        _inputCo = StartCoroutine(InputLoop());
    }

    private void CloseHandbook()
    {
        if (!_isOpen) return;
        _isOpen = false;
        LockLook(false);
        if (_uiRoot != null) { Destroy(_uiRoot); _uiRoot = null; }
        if (_inputCo != null) { StopCoroutine(_inputCo); _inputCo = null; }
    }

    // ============================================================
    //  Láº¬T TRANG
    // ============================================================
    private void FlipForward()
    {
        if (_spreadIndex >= SpreadCount - 1) return;
        _spreadIndex++;
        PlaySFX(flipSound);
        StartCoroutine(FlipAnimation(true));
        RefreshPages();
    }

    private void FlipBack()
    {
        if (_spreadIndex <= 0) return;
        _spreadIndex--;
        PlaySFX(flipSound);
        StartCoroutine(FlipAnimation(false));
        RefreshPages();
    }

    private IEnumerator FlipAnimation(bool forward)
    {
        // Hiá»‡u á»©ng Ä‘Æ¡n giáº£n: scale X nháº¥p nhÃ¡y
        if (_uiRoot == null) yield break;
        Transform book = _uiRoot.transform.Find("BookPanel");
        if (book == null) yield break;

        float dur = 0.12f;
        float t = 0f;
        Vector3 orig = book.localScale;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, forward ? 0.97f : 0.97f, Mathf.Sin(t / dur * Mathf.PI));
            book.localScale = new Vector3(scale, orig.y, orig.z);
            yield return null;
        }
        book.localScale = orig;
    }

    // ============================================================
    //  Cáº¬P NHáº¬T Ná»˜I DUNG TRANG
    // ============================================================
    private void RefreshPages()
    {
        int leftIdx  = _spreadIndex * 2;
        int rightIdx = leftIdx + 1;

        // Trang trÃ¡i
        if (leftIdx < pages.Count)
        {
            _leftTitle.text   = pages[leftIdx].pageTitle;
            _leftContent.text = pages[leftIdx].pageContent;
            _leftPageNum.text = $"Trang {leftIdx + 1}";
        }
        else
        {
            _leftTitle.text   = "";
            _leftContent.text = "";
            _leftPageNum.text = "";
        }

        // Trang pháº£i
        if (rightIdx < pages.Count)
        {
            _rightTitle.text   = pages[rightIdx].pageTitle;
            _rightContent.text = pages[rightIdx].pageContent;
            _rightPageNum.text = $"Trang {rightIdx + 1}";
        }
        else
        {
            // Trang pháº£i trá»‘ng (trang cuá»‘i láº»)
            _rightTitle.text   = "";
            _rightContent.text = "";
            _rightPageNum.text = "";
        }

        // NhÃ£n spread
        _spreadLabel.text = $"â—„ {_spreadIndex + 1} / {SpreadCount} â–º";

        // NÃºt prev/next
        if (_btnPrev != null) _btnPrev.interactable = _spreadIndex > 0;
        if (_btnNext != null) _btnNext.interactable = _spreadIndex < SpreadCount - 1;
    }

    // ============================================================
    //  INPUT LOOP
    // ============================================================
    private IEnumerator InputLoop()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        while (_isOpen)
        {
            // ÄÃ³ng
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { CloseHandbook(); yield break; }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            { CloseHandbook(); yield break; }

            // Láº­t trang
            bool fwd  = (Keyboard.current != null &&
                         (Keyboard.current.dKey.wasPressedThisFrame ||
                          Keyboard.current.rightArrowKey.wasPressedThisFrame));
            bool back = (Keyboard.current != null &&
                         (Keyboard.current.aKey.wasPressedThisFrame ||
                          Keyboard.current.leftArrowKey.wasPressedThisFrame));

            if (fwd)  FlipForward();
            if (back) FlipBack();

            yield return null;
        }
    }

    // ============================================================
    //  BUILD UI
    // ============================================================
    private GameObject BuildUI()
    {
        // â”€â”€ Canvas â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        GameObject canvasGO = new GameObject("HandbookCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // â”€â”€ Overlay tá»‘i â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        MakeImage(canvasGO.transform, "Dim", new Color(0, 0, 0, 0.75f), full: true);

        // â”€â”€ Panel sÃ¡ch (2 trang) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        GameObject bookGO = new GameObject("BookPanel");
        bookGO.transform.SetParent(canvasGO.transform, false);
        Image bookBG = bookGO.AddComponent<Image>();
        bookBG.color = new Color(0.13f, 0.17f, 0.10f, 1f);  // bÃ¬a xanh quÃ¢n sá»±
        RectTransform bookRT = bookGO.GetComponent<RectTransform>();
        bookRT.anchorMin        = new Vector2(0.5f, 0.5f);
        bookRT.anchorMax        = new Vector2(0.5f, 0.5f);
        bookRT.pivot            = new Vector2(0.5f, 0.5f);
        bookRT.sizeDelta        = new Vector2(1100f, 660f);
        bookRT.anchoredPosition = Vector2.zero;

        // â”€â”€ TiÃªu Ä‘á» sá»• (gÃ¡y sÃ¡ch) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        {
            GameObject titleGO = new GameObject("BookTitle");
            titleGO.transform.SetParent(bookGO.transform, false);
            TextMeshProUGUI t = titleGO.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) t.font = fontAsset;
            t.text      = handbookTitle;
            t.fontSize  = 18f;
            t.color     = new Color(0.9f, 0.85f, 0.6f, 1f);
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            RectTransform rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(0f, 36f);
            rt.anchoredPosition = new Vector2(0f, -6f);
        }

        // â”€â”€ ÄÆ°á»ng gÃ¡y giá»¯a â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        {
            GameObject spineGO = new GameObject("Spine");
            spineGO.transform.SetParent(bookGO.transform, false);
            Image img = spineGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.06f, 1f);
            RectTransform rt = spineGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(12f, -36f);
            rt.anchoredPosition = new Vector2(0f, -18f);
        }

        // â”€â”€ Trang TRÃI â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        BuildPage(bookGO.transform, left: true,
            out _leftTitle, out _leftContent, out _leftPageNum);

        // â”€â”€ Trang PHáº¢I â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        BuildPage(bookGO.transform, left: false,
            out _rightTitle, out _rightContent, out _rightPageNum);

        // â”€â”€ Thanh Ä‘iá»u hÆ°á»›ng phÃ­a dÆ°á»›i â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        BuildNavBar(bookGO.transform);

        // â”€â”€ Hint â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        {
            GameObject hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(canvasGO.transform, false);
            TextMeshProUGUI t = hintGO.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) t.font = fontAsset;
            t.text      = "[A / â—„]  Trang trÆ°á»›c      [D / â–º]  Trang sau      [ESC / Chuá»™t pháº£i]  ÄÃ³ng";
            t.fontSize  = 15f;
            t.color     = new Color(0.85f, 0.82f, 0.70f, 0.85f);
            t.fontStyle = FontStyles.Italic;
            t.alignment = TextAlignmentOptions.Center;
            RectTransform rt = hintGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(0f, 36f);
            rt.anchoredPosition = new Vector2(0f, 18f);
        }

        return canvasGO;
    }

    /// <summary>Táº¡o má»™t trang sÃ¡ch (trÃ¡i hoáº·c pháº£i).</summary>
    private void BuildPage(Transform bookParent, bool left,
        out TextMeshProUGUI outTitle,
        out TextMeshProUGUI outContent,
        out TextMeshProUGUI outPageNum)
    {
        float xAnchorMin = left ? 0f    : 0.5f;
        float xAnchorMax = left ? 0.5f  : 1f;

        // Nền trang giấy
        GameObject pageGO = new GameObject(left ? "PageLeft" : "PageRight");
        pageGO.transform.SetParent(bookParent, false);
        Image bg = pageGO.AddComponent<Image>();
        bg.color = paperColor;
        bg.raycastTarget = false; // tắt raycast trên nền tĩnh
        RectTransform pageRT = pageGO.GetComponent<RectTransform>();
        pageRT.anchorMin        = new Vector2(xAnchorMin, 0f);
        pageRT.anchorMax        = new Vector2(xAnchorMax, 1f);
        float offsetMin_x = left ?  8f : 6f;
        float offsetMax_x = left ? -6f : -8f;
        pageRT.offsetMin = new Vector2(offsetMin_x,  42f);
        pageRT.offsetMax = new Vector2(offsetMax_x, -40f);

        // Layout nội dung
        VerticalLayoutGroup vlg = pageGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset(20, 20, 16, 20);
        vlg.spacing              = 8f;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = true;   // để layout tự tính chiều cao
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Váº¡ch mÃ u Ä‘áº§u trang
        AddAccentLine(pageGO.transform);

        // TiÃªu Ä‘á» trang
        outTitle = AddText(pageGO.transform, "", 16f, titleColor, FontStyles.Bold, TextAlignmentOptions.Center, height: 28f);

        // ÄÆ°á»ng káº»
        AddHRule(pageGO.transform);

        // Ná»™i dung
        outContent = AddText(pageGO.transform, "", 14f, contentColor, FontStyles.Normal, TextAlignmentOptions.Left, height: 320f);

        // Sá»‘ trang (absolute, gÃ³c dÆ°á»›i)
        GameObject numGO = new GameObject("PageNum");
        numGO.transform.SetParent(pageGO.transform, false);
        LayoutElement le = numGO.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        outPageNum = numGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) outPageNum.font = fontAsset;
        outPageNum.fontSize  = 12f;
        outPageNum.color     = new Color(0.45f, 0.38f, 0.22f, 0.8f);
        outPageNum.fontStyle = FontStyles.Italic;
        outPageNum.alignment = left ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomRight;
        RectTransform numRT  = numGO.GetComponent<RectTransform>();
        numRT.anchorMin        = new Vector2(0f, 0f);
        numRT.anchorMax        = new Vector2(1f, 0f);
        numRT.pivot            = new Vector2(0.5f, 0f);
        numRT.sizeDelta        = new Vector2(-24f, 22f);
        numRT.anchoredPosition = new Vector2(0f, 4f);
    }

    private void BuildNavBar(Transform bookParent)
    {
        // Spread label á»Ÿ giá»¯a Ä‘Ã¡y sÃ¡ch
        GameObject labelGO = new GameObject("SpreadLabel");
        labelGO.transform.SetParent(bookParent, false);
        _spreadLabel = labelGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) _spreadLabel.font = fontAsset;
        _spreadLabel.fontSize  = 14f;
        _spreadLabel.color     = new Color(0.8f, 0.76f, 0.55f, 1f);
        _spreadLabel.alignment = TextAlignmentOptions.Center;
        RectTransform rt = labelGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(220f, 30f);
        rt.anchoredPosition = new Vector2(0f, 6f);

        // NÃºt â—„ trÃ¡i
        _btnPrev = MakeArrowButton(bookParent, "BtnPrev", "â—„", new Vector2(-430f, 6f), FlipBack);

        // NÃºt â–º pháº£i
        _btnNext = MakeArrowButton(bookParent, "BtnNext", "â–º", new Vector2( 430f, 6f), FlipForward);
    }

    // ============================================================
    //  UI HELPERS
    // ============================================================
    private TextMeshProUGUI AddText(Transform parent, string text, float size,
        Color color, FontStyles style, TextAlignmentOptions align, float height)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) t.font = fontAsset;
        t.text               = text;
        t.fontSize           = size;
        t.color              = color;
        t.fontStyle          = style;
        t.alignment          = align;
        t.enableWordWrapping = true;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth     = 1f;
        le.preferredHeight   = height;
        return t;
    }

    private void AddHRule(Transform parent)
    {
        GameObject go = new GameObject("HRule");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.40f, 0.32f, 0.18f, 0.5f);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 1.5f;
        le.flexibleWidth = 1f;
    }

    private void AddAccentLine(Transform parent)
    {
        GameObject go = new GameObject("AccentLine");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = accentColor;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 4f;
        le.flexibleWidth = 1f;
    }

    private Image MakeImage(Transform parent, string name, Color color, bool full = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        if (full)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        return img;
    }

    private Button MakeArrowButton(Transform parent, string name, string label,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.24f, 0.13f, 0.85f);
        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(44f, 30f);
        rt.anchoredPosition = anchoredPos;

        // Text trÃªn nÃºt
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI t = textGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) t.font = fontAsset;
        t.text      = label;
        t.fontSize  = 16f;
        t.color     = new Color(0.85f, 0.80f, 0.55f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return btn;
    }

    // ============================================================
    //  LOCK CAMERA & CURSOR
    // ============================================================
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

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip,
                Camera.main != null ? Camera.main.transform.position : transform.position);
    }

    private void OnDestroy()
    {
        if (_isOpen) CloseHandbook();
    }
}

