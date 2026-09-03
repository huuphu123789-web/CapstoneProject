using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameIntroEvent - Su kien mo dau: dien thoai reo, khach la di vao bot canh.
/// TU DONG tao Dialogue UI + Letterbox trong code — khong can gan gi trong Inspector.
/// Chi can gan: phone, strangerPrefab, 3 waypoint Transform.
/// </summary>
public class GameIntroEvent : MonoBehaviour
{
    [Header("=== Tham chieu ===")]
    [Tooltip("Keo InteractablePhone vao day")]
    public InteractablePhone phone;

    [Header("=== NPC Vi Khach La ===")]
    [Tooltip("Prefab NPC bi an (de trong = tu tao capsule)")]
    public GameObject strangerPrefab;
    [Tooltip("Diem xuat phat NPC (xa ngoai camera)")]
    public Transform strangerSpawnPoint;
    [Tooltip("Diem NPC dung lai truoc bot gac")]
    public Transform strangerStopPoint;
    [Tooltip("Diem NPC roi di (sau khi noi xong)")]
    public Transform strangerExitPoint;
    public float npcMoveSpeed = 1.6f;
    public float npcRotateSpeed = 5f;

    [Header("=== Cai dat ===")]
    public float startDelay = 1.5f;
    public float letterboxHeight = 100f;
    public float letterboxDuration = 0.6f;

    [Header("=== Skip (tuy chon) ===")]
    public Button skipButton;

    [Header("=== Noi dung thoai ===")]
    [TextArea(2,4)] public string line1 = "HE THONG: Phat hien tin hieu khong xac dinh tu ngoai vanh dai...";
    [TextArea(2,4)] public string line2 = "HE THONG: CANH BAO! Di the dang tien den bot gac tu huong bac!";
    [TextArea(2,4)] public string line3 = "NGUOI LA: ...Nguoi la nguoi canh giu noi nay?";
    [TextArea(2,4)] public string line4 = "NGUOI LA: Ta da quan sat nguoi tu rat lau roi. Moi nguy hiem dang den...";
    [TextArea(2,4)] public string line5 = "NGUOI LA: Bot canh nay se la tuyen phong thu cuoi cung cua thung lung.";
    [TextArea(2,4)] public string line6 = "HE THONG: Di the bien mat khoi radar. Ca truc bat dau. Hay nghe dien thoai.";

    // ── Runtime UI (tu tao) ──
    private GameObject       _dlgPanel;
    private TextMeshProUGUI  _dlgText;
    private TextMeshProUGUI  _dlgSpeaker;
    private CanvasGroup      _dlgGroup;
    private RectTransform    _lbT, _lbB;

    // ── NPC & flow ──
    private GameObject _npc;
    private bool _skipped, _running;
    private Coroutine _co, _twCo;

    // ════════════════════════════════════════════
    //  UNITY START
    // ════════════════════════════════════════════
    private void Start()
    {
        BuildDialogueUI();
        BuildLetterboxUI();

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(Skip);
        }

        if (phone != null) phone.StopRinging();
        LockPlayer(true);
        _co = StartCoroutine(Run());
    }

    // ════════════════════════════════════════════
    //  BUILD UI (tu dong)
    // ════════════════════════════════════════════
    private void BuildDialogueUI()
    {
        // Canvas
        GameObject cgo = new GameObject("GameEvent_DialogueCanvas");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cgo.AddComponent<GraphicRaycaster>();

        // Panel nen mo
        _dlgPanel = new GameObject("DlgPanel");
        _dlgPanel.transform.SetParent(cgo.transform, false);
        Image bg = _dlgPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;
        _dlgGroup = _dlgPanel.AddComponent<CanvasGroup>();
        _dlgGroup.alpha = 0f;

        RectTransform panelRT = _dlgPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot     = new Vector2(0.5f, 0f);
        panelRT.sizeDelta = new Vector2(0f, 130f);
        panelRT.anchoredPosition = new Vector2(0f, 0f);

        // Ten nguoi noi (nho, mau vang)
        GameObject speakerGo = new GameObject("SpeakerName");
        speakerGo.transform.SetParent(_dlgPanel.transform, false);
        _dlgSpeaker = speakerGo.AddComponent<TextMeshProUGUI>();
        _dlgSpeaker.fontSize   = 18f;
        _dlgSpeaker.fontStyle  = FontStyles.Bold;
        _dlgSpeaker.color      = new Color(1f, 0.85f, 0.3f);
        _dlgSpeaker.text       = "";
        RectTransform spRT = speakerGo.GetComponent<RectTransform>();
        spRT.anchorMin        = new Vector2(0f, 1f);
        spRT.anchorMax        = new Vector2(1f, 1f);
        spRT.pivot            = new Vector2(0.5f, 0f);
        spRT.anchoredPosition = new Vector2(0f, 6f);
        spRT.sizeDelta        = new Vector2(-80f, 26f);

        // Noi dung thoai
        GameObject textGo = new GameObject("DlgText");
        textGo.transform.SetParent(_dlgPanel.transform, false);
        _dlgText = textGo.AddComponent<TextMeshProUGUI>();
        _dlgText.fontSize  = 22f;
        _dlgText.color     = Color.white;
        _dlgText.text      = "";
        _dlgText.enableWordWrapping = true;
        RectTransform txRT = textGo.GetComponent<RectTransform>();
        txRT.anchorMin        = new Vector2(0f, 0f);
        txRT.anchorMax        = new Vector2(1f, 1f);
        txRT.offsetMin        = new Vector2(40f, 12f);
        txRT.offsetMax        = new Vector2(-40f, -6f);
    }

    private void BuildLetterboxUI()
    {
        GameObject cgo = new GameObject("GameEvent_LetterboxCanvas");
        Canvas c = cgo.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 997;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();
        _lbT = MakeBar(cgo.transform, true);
        _lbB = MakeBar(cgo.transform, false);
        SetLBH(0f);
    }

    private RectTransform MakeBar(Transform p, bool top)
    {
        GameObject go = new GameObject(top ? "LB_Top" : "LB_Bot");
        go.transform.SetParent(p, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.black; img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot     = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    // ════════════════════════════════════════════
    //  MAIN EVENT SEQUENCE
    // ════════════════════════════════════════════
    private IEnumerator Run()
    {
        _running = true;
        yield return new WaitForSeconds(startDelay);
        if (_skipped) { Finish(); yield break; }

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        yield return StartCoroutine(LB(true));
        yield return new WaitForSeconds(0.3f);

        // Dien thoai reo
        if (phone != null) phone.StartRinging();
        yield return StartCoroutine(ShowDlg("HE THONG", line1, 3.5f));
        if (_skipped) { Finish(); yield break; }

        // Chuong ngung - flash - canh bao
        if (phone != null) phone.StopRinging();
        Flash(0.5f); yield return new WaitForSeconds(0.3f); Flash(0.35f);
        yield return StartCoroutine(ShowDlg("HE THONG", line2, 3.0f));
        if (_skipped) { Finish(); yield break; }

        // NPC xuat hien, di vao
        SpawnNPC();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MoveNPC(strangerStopPoint));
        if (_skipped) { Finish(); yield break; }

        // NPC nhin ve camera
        if (_npc != null && Camera.main != null)
        {
            Vector3 d = Camera.main.transform.position - _npc.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) _npc.transform.rotation = Quaternion.LookRotation(d);
        }
        yield return new WaitForSeconds(0.6f);
        if (_skipped) { Finish(); yield break; }

        // NPC noi chuyen
        Flash(0.4f);
        yield return StartCoroutine(ShowDlg("NGUOI LA", line3, 3.5f)); if (_skipped) { Finish(); yield break; }
        yield return StartCoroutine(ShowDlg("NGUOI LA", line4, 4.0f)); if (_skipped) { Finish(); yield break; }
        yield return StartCoroutine(ShowDlg("NGUOI LA", line5, 4.0f)); if (_skipped) { Finish(); yield break; }

        // NPC bien mat
        Flash(0.7f); yield return new WaitForSeconds(0.2f); Flash(0.5f);
        if (strangerExitPoint != null) yield return StartCoroutine(MoveNPC(strangerExitPoint));
        if (_npc != null) Destroy(_npc);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(ShowDlg("HE THONG", line6, 4.0f));

        // Dong letterbox
        yield return StartCoroutine(FadeDlg(false));
        yield return StartCoroutine(LB(false));
        yield return new WaitForSeconds(0.5f);
        Finish();
    }

    // ════════════════════════════════════════════
    //  DIALOGUE HELPERS
    // ════════════════════════════════════════════
    private IEnumerator ShowDlg(string speaker, string text, float duration)
    {
        if (_dlgSpeaker != null) _dlgSpeaker.text = speaker;
        if (_dlgText    != null) _dlgText.text    = "";

        // Fade in
        yield return StartCoroutine(FadeDlg(true));

        // Typewriter
        if (_twCo != null) StopCoroutine(_twCo);
        _twCo = StartCoroutine(Typewriter(text, 38f));
        yield return _twCo;

        // Doi
        float typeTime  = text.Length / 38f;
        float remaining = duration - typeTime;
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
    }

    private IEnumerator Typewriter(string text, float charsPerSec)
    {
        if (_dlgText == null) yield break;
        _dlgText.text = text;
        _dlgText.maxVisibleCharacters = 0;
        _dlgText.ForceMeshUpdate();
        float delay = 1f / charsPerSec;
        for (int i = 0; i < text.Length; i++)
        {
            _dlgText.maxVisibleCharacters = i + 1;
            char c = text[i];
            if (c == '.' || c == '!' || c == '?') yield return new WaitForSeconds(delay * 5f);
            else if (c == ',') yield return new WaitForSeconds(delay * 2.5f);
            else yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator FadeDlg(bool fadeIn)
    {
        if (_dlgGroup == null) yield break;
        float dur = 0.3f, t = 0f;
        float from = fadeIn ? 0f : 1f, to = fadeIn ? 1f : 0f;
        if (fadeIn && _dlgPanel != null) _dlgPanel.SetActive(true);
        while (t < dur) { t += Time.deltaTime; _dlgGroup.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
        _dlgGroup.alpha = to;
        if (!fadeIn && _dlgPanel != null) _dlgPanel.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  NPC
    // ════════════════════════════════════════════
    private void SpawnNPC()
    {
        Vector3 pos = strangerSpawnPoint != null ? strangerSpawnPoint.position : new Vector3(0f, 0f, -25f);
        _npc = strangerPrefab != null
            ? Instantiate(strangerPrefab, pos, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _npc.transform.position = pos;
        _npc.name = "StrangerNPC_Event";
        Collider col = _npc.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (strangerStopPoint != null)
        {
            Vector3 d = strangerStopPoint.position - pos; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) _npc.transform.rotation = Quaternion.LookRotation(d);
        }
    }

    private IEnumerator MoveNPC(Transform target)
    {
        if (_npc == null || target == null) yield break;
        while (!_skipped && _npc != null)
        {
            Vector3 dest = new Vector3(target.position.x, _npc.transform.position.y, target.position.z);
            if (Vector3.Distance(_npc.transform.position, dest) < 0.18f) break;
            _npc.transform.position = Vector3.MoveTowards(_npc.transform.position, dest, npcMoveSpeed * Time.deltaTime);
            Vector3 dir = dest - _npc.transform.position;
            if (dir.sqrMagnitude > 0.01f)
                _npc.transform.rotation = Quaternion.Slerp(_npc.transform.rotation, Quaternion.LookRotation(dir), npcRotateSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // ════════════════════════════════════════════
    //  UTILS
    // ════════════════════════════════════════════
    private void Flash(float intensity) { if (ScreenFlash.Instance != null) ScreenFlash.Instance.TriggerFlash(intensity); }

    private void LockPlayer(bool lockIt)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>() ?? FindObjectOfType<PlayerMovement>();
        MouseLook ml      = FindFirstObjectByType<MouseLook>()      ?? FindObjectOfType<MouseLook>();
        if (pm != null) pm.enabled = !lockIt;
        if (ml != null) ml.enabled = !lockIt;
        Cursor.lockState = lockIt ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = lockIt;
    }

    private void SetLBH(float h) { if (_lbT) _lbT.sizeDelta = new Vector2(0f,h); if (_lbB) _lbB.sizeDelta = new Vector2(0f,h); }

    private IEnumerator LB(bool open)
    {
        float from = open ? 0f : letterboxHeight, to = open ? letterboxHeight : 0f, t = 0f;
        while (t < letterboxDuration) { t += Time.deltaTime; SetLBH(Mathf.Lerp(from, to, Mathf.SmoothStep(0f,1f,t/letterboxDuration))); yield return null; }
        SetLBH(to);
    }

    // ════════════════════════════════════════════
    //  SKIP & FINISH
    // ════════════════════════════════════════════
    public void Skip()
    {
        if (!_running) return;
        _skipped = true;
        if (_co  != null) StopCoroutine(_co);
        if (_twCo != null) StopCoroutine(_twCo);
        if (phone != null) phone.StopRinging();
        if (_npc  != null) Destroy(_npc);
        if (_dlgPanel != null) _dlgPanel.SetActive(false);
        SetLBH(0f);
        Finish();
    }

    private void Finish()
    {
        _running = false;
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        if (phone != null) phone.StartRinging();
        LockPlayer(false);
    }

    private void OnDestroy() { if (phone != null) phone.StopRinging(); }
}