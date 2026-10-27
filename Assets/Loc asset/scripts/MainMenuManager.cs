using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Main Menu chuyen nghiep cho "Bot Canh".
/// Tao toan bo UI runtime â€” chi can gan script len mot GameObject trong scene Menu.
/// Font kieu may danh chu: chi co chu, khong nen, monospace-like.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ============================================================
    //  CAU HINH INSPECTOR
    // ============================================================
    [Header("=== SCENE ===")]
    public string gameSceneName = "Night-1";

    [Header("=== TIEU DE ===")]
    public string gameTitle    = "BOT CANH";
    public string gameSubtitle = "[ dem thu 1 ]";

    [Header("=== MAU SAC ===")]
    public Color bgColor        = new Color(0.04f, 0.04f, 0.05f, 1f);
    public Color titleColor     = new Color(0.92f, 0.88f, 0.78f, 1f);
    public Color subtitleColor  = new Color(0.45f, 0.42f, 0.36f, 1f);
    public Color btnNormalColor = new Color(0.65f, 0.62f, 0.56f, 1f);
    public Color btnHoverColor  = new Color(0.95f, 0.92f, 0.82f, 1f);
    public Color btnDisabledColor = new Color(0.30f, 0.28f, 0.25f, 1f);
    public Color accentColor    = new Color(0.75f, 0.18f, 0.14f, 1f);

    [Header("=== AM THANH ===")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip bgMusic;

    // ============================================================
    //  RUNTIME INTERNAL
    // ============================================================
    private Canvas      _canvas;
    private GameObject  _mainPanel, _settingsPanel;
    private CanvasGroup _mainGroup, _settingsGroup;
    private AudioSource _audioSrc;
    private bool        _hasSave;
    private bool        _transitioning;

    private struct MenuButton
    {
        public RectTransform   rt;
        public TextMeshProUGUI label;
        public Image           accent;
        public Button          btn;
    }

    // ============================================================
    //  AWAKE â€” dam bao EventSystem
    // ============================================================
    private void Awake()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ============================================================
    //  START
    // ============================================================
    private void Start()
    {
        _hasSave = PlayerPrefs.GetInt("SaveExists", 0) == 1;

        _audioSrc = gameObject.AddComponent<AudioSource>();
        _audioSrc.spatialBlend = 0f;

        if (bgMusic != null)
        {
            AudioSource music = gameObject.AddComponent<AudioSource>();
            music.clip        = bgMusic;
            music.loop        = true;
            music.spatialBlend= 0f;
            music.volume      = PlayerPrefs.GetFloat("MusicVol", 0.45f);
            music.Play();
        }

        BuildUI();
        StartCoroutine(FadeIn(_mainGroup, 1.4f));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ============================================================
    //  BUILD UI
    // ============================================================
    private void BuildUI()
    {
        // --- Canvas ---
        GameObject cgo = new GameObject("MenuCanvas");
        cgo.transform.SetParent(transform, false);
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 0;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cgo.AddComponent<GraphicRaycaster>();

        // Background toan man hinh
        MakeImage(cgo.transform, "BG", Vector2.zero, Vector2.one, bgColor, Vector2.zero, Vector2.zero);

        // Gradient top/bottom de tao chieu sau
        MakeGradient(cgo.transform, true);
        MakeGradient(cgo.transform, false);

        // Duong ket noi doc chia doi man hinh (rat mo)
        MakeDivider(cgo.transform);

        // --- MAIN PANEL ---
        _mainPanel = new GameObject("MainPanel");
        _mainPanel.transform.SetParent(cgo.transform, false);
        SetFull(_mainPanel);
        _mainGroup = _mainPanel.AddComponent<CanvasGroup>();
        _mainGroup.alpha = 0f;

        BuildTitle(_mainPanel.transform);
        BuildButtons(_mainPanel.transform);
        BuildFooter(_mainPanel.transform);

        // --- SETTINGS PANEL ---
        _settingsPanel = new GameObject("SettingsPanel");
        _settingsPanel.transform.SetParent(cgo.transform, false);
        SetFull(_settingsPanel);
        _settingsGroup = _settingsPanel.AddComponent<CanvasGroup>();
        _settingsGroup.alpha          = 0f;
        _settingsGroup.interactable   = false;
        _settingsGroup.blocksRaycasts = false;
        BuildSettings(_settingsPanel.transform);
    }

    // ============================================================
    //  TITLE
    // ============================================================
    private void BuildTitle(Transform parent)
    {
        // Container trai
        GameObject block = new GameObject("TitleBlock");
        block.transform.SetParent(parent, false);
        RectTransform rt = block.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0.58f);
        rt.anchorMax        = new Vector2(0.46f, 0.90f);
        rt.offsetMin        = new Vector2(90f, 0f);
        rt.offsetMax        = new Vector2(0f, 0f);

        // Ten game â€” font nhu may danh chu: bold, spacing rong
        TextMeshProUGUI title = MakeTMP(block.transform, gameTitle,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 30f), Vector2.zero);
        title.fontSize          = 88f;
        title.fontStyle         = FontStyles.Bold;
        title.color             = titleColor;
        title.alignment         = TextAlignmentOptions.BottomLeft;
        title.characterSpacing  = 16f;
        title.wordSpacing       = 0f;

        // Duong gach ngang do duoi tieu de
        GameObject line = new GameObject("Line");
        line.transform.SetParent(block.transform, false);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = accentColor;
        lineImg.raycastTarget = false;
        RectTransform lineRT = lineImg.rectTransform;
        lineRT.anchorMin        = new Vector2(0f, 0f);
        lineRT.anchorMax        = new Vector2(0.55f, 0f);
        lineRT.pivot            = new Vector2(0f, 1f);
        lineRT.anchoredPosition = new Vector2(0f, 28f);
        lineRT.sizeDelta        = new Vector2(0f, 2f);

        // Phu de â€” kieu may danh chu, chu thuong, co nho
        TextMeshProUGUI sub = MakeTMP(block.transform, gameSubtitle,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 22f), new Vector2(0f, 24f));
        sub.fontSize         = 18f;
        sub.fontStyle        = FontStyles.Normal;
        sub.color            = subtitleColor;
        sub.alignment        = TextAlignmentOptions.BottomLeft;
        sub.characterSpacing = 10f;
    }

    // ============================================================
    //  BUTTONS â€” chi co chu, khong nen
    // ============================================================
    private void BuildButtons(Transform parent)
    {
        // Container ben trai
        GameObject block = new GameObject("BtnBlock");
        block.transform.SetParent(parent, false);
        RectTransform blockRT = block.AddComponent<RectTransform>();
        blockRT.anchorMin = new Vector2(0f,    0.14f);
        blockRT.anchorMax = new Vector2(0.44f, 0.55f);
        blockRT.offsetMin = new Vector2(90f, 0f);
        blockRT.offsetMax = Vector2.zero;

        // Data cac button
        string[] labels  = { "> bat dau",     "> tiep tuc",   "> cai dat",     "> thoat" };
        bool[]   enabled = { true,              _hasSave,       true,            true };
        System.Action[] acts = {
            () => StartGame(),
            () => ContinueGame(),
            () => OpenSettings(),
            () => QuitGame()
        };

        float btnH    = 56f;
        float spacing = 8f;
        int   count   = labels.Length;
        float totalH  = count * btnH + (count - 1) * spacing;
        float startY  = totalH * 0.5f - btnH * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float yPos = startY - i * (btnH + spacing);
            AddTextButton(block.transform, labels[i], yPos, btnH, enabled[i], acts[i]);
        }
    }

    private void AddTextButton(Transform parent, string label, float yPos, float height,
                                bool isEnabled, System.Action onClick)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        // Click area (transparent)
        Image area = go.AddComponent<Image>();
        area.color = Color.clear;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = area;
        btn.transition    = Selectable.Transition.None;
        btn.interactable  = isEnabled;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.sizeDelta        = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, yPos);

        // Vach do ben trai (an ban dau)
        GameObject acGo = new GameObject("Accent");
        acGo.transform.SetParent(go.transform, false);
        Image acImg = acGo.AddComponent<Image>();
        acImg.color = Color.clear;
        RectTransform acRT = acImg.rectTransform;
        acRT.anchorMin        = new Vector2(0f, 0.15f);
        acRT.anchorMax        = new Vector2(0f, 0.85f);
        acRT.pivot            = new Vector2(0f, 0.5f);
        acRT.sizeDelta        = new Vector2(3f, 0f);
        acRT.anchoredPosition = Vector2.zero;

        // Text chinh â€” kieu may danh chu: khong nen, khong bold
        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text             = label;
        txt.fontSize         = 34f;
        txt.fontStyle        = FontStyles.Normal;
        txt.color            = isEnabled ? btnNormalColor : btnDisabledColor;
        txt.alignment        = TextAlignmentOptions.MidlineLeft;
        txt.characterSpacing = 6f;         // deu nhu may danh chu
        txt.wordSpacing      = 0f;
        txt.raycastTarget    = false;
        RectTransform txtRT = txtGo.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(14f, 0f);
        txtRT.offsetMax = Vector2.zero;

        if (!isEnabled) return;  // ko can event

        // EventTrigger hover
        var trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerEnter, (_) => {
            StartCoroutine(HoverAnim(rt, acImg, txt, true));
            PlaySFX(hoverSound);
        });
        AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerExit, (_) => {
            StartCoroutine(HoverAnim(rt, acImg, txt, false));
        });

        btn.onClick.AddListener(() => {
            if (_transitioning) return;
            PlaySFX(clickSound);
            onClick?.Invoke();
        });
    }

    // ============================================================
    //  HOVER ANIMATION
    // ============================================================
    private IEnumerator HoverAnim(RectTransform rt, Image accent, TextMeshProUGUI txt, bool enter)
    {
        float dur = 0.10f, t = 0f;
        float fromX = rt.anchoredPosition.x, toX = enter ? 20f : 0f;
        Color fromTxt = txt.color,   toTxt = enter ? btnHoverColor : btnNormalColor;
        Color fromAcc = accent.color, toAcc = enter ? accentColor  : Color.clear;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, p), rt.anchoredPosition.y);
            txt.color    = Color.Lerp(fromTxt, toTxt, p);
            accent.color = Color.Lerp(fromAcc, toAcc, p);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(toX, rt.anchoredPosition.y);
        txt.color    = toTxt;
        accent.color = toAcc;
    }

    // ============================================================
    //  FOOTER
    // ============================================================
    private void BuildFooter(Transform parent)
    {
        TextMeshProUGUI ver = MakeTMP(parent, "v0.1 early access",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(-30f, 30f), new Vector2(0f, 30f));
        ver.fontSize         = 13f;
        ver.fontStyle        = FontStyles.Normal;
        ver.color            = new Color(0.28f, 0.26f, 0.22f, 0.8f);
        ver.alignment        = TextAlignmentOptions.BottomRight;
        ver.characterSpacing = 6f;
        ver.raycastTarget    = false;
    }

    // ============================================================
    //  SETTINGS PANEL
    // ============================================================
    private void BuildSettings(Transform parent)
    {
        // Backdrop toi
        MakeImage(parent, "Backdrop", Vector2.zero, Vector2.one,
            new Color(0f, 0f, 0f, 0.88f), Vector2.zero, Vector2.zero);

        // Container giua man hinh
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.07f, 0.97f);
        RectTransform panelRT = panelImg.rectTransform;
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(620f, 460f);
        panelRT.anchoredPosition = Vector2.zero;

        // Vach do trai
        GameObject brd = new GameObject("LeftBar");
        brd.transform.SetParent(panel.transform, false);
        Image brdImg = brd.AddComponent<Image>();
        brdImg.color = accentColor;
        RectTransform brdRT = brdImg.rectTransform;
        brdRT.anchorMin = new Vector2(0f, 0f); brdRT.anchorMax = new Vector2(0f, 1f);
        brdRT.pivot     = new Vector2(0f, 0.5f); brdRT.sizeDelta = new Vector2(3f, 0f);

        // Header
        TextMeshProUGUI header = MakeTMP(panel.transform, "[ cai dat ]",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(0f, 60f));
        header.fontSize  = 30f;
        header.fontStyle = FontStyles.Normal;
        header.color     = titleColor;
        header.alignment = TextAlignmentOptions.Center;
        header.characterSpacing = 10f;

        // Sliders
        string[] keys    = { "MasterVol", "MusicVol", "SFXVol", "Sensitivity" };
        string[] names   = { "am luong",  "nhac nen", "hieu ung", "do nhay chuot" };
        float[]  mins    = { 0f, 0f, 0f, 0.5f };
        float[]  maxs    = { 1f, 1f, 1f, 10f };
        float[]  defs    = { 0.8f, 0.45f, 1f, 2.5f };

        float yStart = 100f, yStep = 78f;
        for (int i = 0; i < keys.Length; i++)
            MakeSlider(panel.transform, names[i], keys[i], mins[i], maxs[i], defs[i],
                yStart - i * yStep);

        // Back button â€” text only
        AddTextButton(panel.transform, "< quay lai",
            -165f, 50f, true, () => CloseSettings());
    }

    // ============================================================
    //  SLIDER
    // ============================================================
    private void MakeSlider(Transform parent, string name, string prefKey,
        float min, float max, float def, float yPos)
    {
        float saved = PlayerPrefs.GetFloat(prefKey, def);

        // Label
        TextMeshProUGUI lbl = MakeTMP(parent, name + " :",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-270f, yPos), new Vector2(230f, 36f));
        lbl.fontSize         = 18f;
        lbl.fontStyle        = FontStyles.Normal;
        lbl.color            = btnNormalColor;
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.characterSpacing = 6f;

        // Slider GO
        GameObject sGo = new GameObject("Sl_" + prefKey);
        sGo.transform.SetParent(parent, false);
        Slider slider = sGo.AddComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = saved;

        RectTransform sRT = sGo.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.5f, 0.5f); sRT.anchorMax = new Vector2(0.5f, 0.5f);
        sRT.pivot     = new Vector2(0f, 0.5f);
        sRT.sizeDelta = new Vector2(250f, 18f);
        sRT.anchoredPosition = new Vector2(30f, yPos);

        // Track background
        GameObject bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(sGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.20f, 1f);
        RectTransform bgRT = bgImg.rectTransform;
        bgRT.anchorMin = new Vector2(0f, 0.4f); bgRT.anchorMax = new Vector2(1f, 0.6f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        slider.targetGraphic = bgImg;

        // Fill
        GameObject faGo = new GameObject("FillArea");
        faGo.transform.SetParent(sGo.transform, false);
        RectTransform faRT = faGo.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.4f); faRT.anchorMax = new Vector2(1f, 0.6f);
        faRT.offsetMin = new Vector2(5f, 0f); faRT.offsetMax = new Vector2(-5f, 0f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(faGo.transform, false);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = accentColor;
        RectTransform fillRT = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        slider.fillRect = fillRT;

        // Handle
        GameObject haGo = new GameObject("HandleArea");
        haGo.transform.SetParent(sGo.transform, false);
        RectTransform haRT = haGo.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(8f, 0f); haRT.offsetMax = new Vector2(-8f, 0f);

        GameObject hGo = new GameObject("Handle");
        hGo.transform.SetParent(haGo.transform, false);
        Image hImg = hGo.AddComponent<Image>();
        hImg.color = titleColor;
        RectTransform hRT = hImg.rectTransform;
        hRT.sizeDelta = new Vector2(14f, 14f);
        slider.handleRect = hRT;

        slider.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat(prefKey, v);
            PlayerPrefs.Save();
        });
    }

    // ============================================================
    //  ACTIONS
    // ============================================================
    private void StartGame()
    {
        PlayerPrefs.SetInt("SaveExists", 1);
        PlayerPrefs.SetString("LastScene", gameSceneName);
        PlayerPrefs.Save();
        StartCoroutine(FadeToScene(gameSceneName));
    }

    private void ContinueGame()
    {
        string scene = PlayerPrefs.GetString("LastScene", gameSceneName);
        StartCoroutine(FadeToScene(scene));
    }

    private void OpenSettings()    => StartCoroutine(Swap(_mainGroup, _settingsGroup));
    private void CloseSettings()   => StartCoroutine(Swap(_settingsGroup, _mainGroup));

    private void QuitGame()
    {
        StartCoroutine(FadeOut(_mainGroup, 0.5f, () => {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }));
    }

    // ============================================================
    //  TRANSITIONS
    // ============================================================
    private IEnumerator FadeIn(CanvasGroup cg, float dur)
    {
        cg.alpha = 0f; cg.interactable = false;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; cg.alpha = t / dur; yield return null; }
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
    }

    private IEnumerator FadeOut(CanvasGroup cg, float dur, System.Action onDone = null)
    {
        cg.interactable = false; cg.blocksRaycasts = false;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; cg.alpha = 1f - t / dur; yield return null; }
        cg.alpha = 0f;
        onDone?.Invoke();
    }

    private IEnumerator Swap(CanvasGroup from, CanvasGroup to)
    {
        if (_transitioning) yield break;
        _transitioning = true;
        yield return StartCoroutine(FadeOut(from, 0.18f));
        yield return StartCoroutine(FadeIn(to, 0.22f));
        _transitioning = false;
    }

    private IEnumerator FadeToScene(string sceneName)
    {
        if (_transitioning) yield break;
        _transitioning = true;

        GameObject fadeGo = new GameObject("Fade");
        fadeGo.transform.SetParent(_canvas.transform, false);
        Image fade = fadeGo.AddComponent<Image>();
        fade.color = Color.clear;
        SetFull(fadeGo);

        float t = 0f, dur = 0.65f;
        while (t < dur)
        {
            t += Time.deltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
    }

    // ============================================================
    //  HELPERS
    // ============================================================
    private void SetFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private Image MakeImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return img;
    }

    private TextMeshProUGUI MakeTMP(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject("T_" + text.Substring(0, Mathf.Min(8, text.Length)));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return t;
    }

    private void MakeGradient(Transform parent, bool top)
    {
        Image img = MakeImage(parent, top ? "GradTop" : "GradBot",
            new Vector2(0f, top ? 0.72f : 0f),
            new Vector2(1f, top ? 1f   : 0.28f),
            new Color(0f, 0f, 0f, top ? 0.45f : 0.65f),
            Vector2.zero, Vector2.zero);
        img.raycastTarget = false;
    }

    private void MakeDivider(Transform parent)
    {
        Image img = MakeImage(parent, "VDiv",
            new Vector2(0.455f, 0.04f), new Vector2(0.455f, 0.96f),
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f),
            Vector2.zero, new Vector2(1f, 0f));
        img.raycastTarget = false;
    }

    private void AddTrigger(UnityEngine.EventSystems.EventTrigger trigger,
        UnityEngine.EventSystems.EventTriggerType type,
        UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> action)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audioSrc != null)
            _audioSrc.PlayOneShot(clip, PlayerPrefs.GetFloat("SFXVol", 1f));
    }
}