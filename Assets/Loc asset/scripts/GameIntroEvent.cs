using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

// ============================================================
//  KET QUA XU LY NPC KHACH
// ============================================================
public enum NPCOutcome { LetThrough, Eliminate }

// ============================================================
//  CAU HINH MOI NPC KHACH (them bao nhieu tuy thich)
// ============================================================
[System.Serializable]
public class VisitorNPCConfig
{
    [Tooltip("Ten hien thi trong hop thoai (vi du: NGUOI KHACH 1)")]
    public string npcDisplayName = "NGUOI KHACH";

    [Tooltip("Prefab cua NPC nguoi nhap canh")]
    public GameObject npcPrefab;

    [Header("Giay to")]
    [Tooltip("Prefab giay to / chung minh thu cua NPC nay (de trong = khong xuat hien giay to)")]
    public GameObject documentPrefab;

    [Tooltip("Vi tri giay to xuat hien (neu de trong = dung Document Show Point chung)")]
    public Transform documentSpawnPoint;

    [Tooltip("Danh sach loi thoai. Dinh dang 'Ten: Loi' hoac chi viet loi dan truyen.")]
    public List<string> dialogueLines = new List<string>();

    [Tooltip("Ket qua sau khi thoai xong")]
    public NPCOutcome outcome = NPCOutcome.LetThrough;

    [Tooltip("Thoi gian cho (giay) truoc khi NPC tiep theo xuat hien")]
    public float pauseBeforeNext = 0.8f;
}

// ============================================================
//  GAME INTRO EVENT
// ============================================================
/// <summary>
/// He thong cutscene mo dau:
///   1. Entity NPC xuat hien va noi chuyen
///   2. Cac NPC khach lan luot den - xuat trinh giay to - cho vao / thu tieu
///   3. Dien thoai reo - thoai dien thoai
///   4. (Tuy chon) Hien task cho nguoi choi
/// </summary>
public class GameIntroEvent : MonoBehaviour
{
    // --------------------------------------------------------
    //  ENTITY NPC (XUAT HIEN DAU TIEN)
    // --------------------------------------------------------
    [Header("=== ENTITY NPC (Xuat hien dau tien) ===")]
    public GameObject entityPrefab;
    public Transform entitySpawnPoint;
    public Transform entityStopPoint;
    public Transform entityExitPoint;
    public float entityMoveSpeed = 1.6f;
    public float entityRotateSpeed = 5f;

    [Tooltip("Thoai cua Entity (dinh dang 'Ten: Loi' hoac loi dan thuyet)")]
    public List<string> entityDialogue = new List<string>();

    // --------------------------------------------------------
    //  VISITOR NPCs (NPC KHACH)
    // --------------------------------------------------------
    [Header("=== DANH SACH NPC KHACH (Them bao nhieu tuy thich) ===")]
    public List<VisitorNPCConfig> visitorNPCs = new List<VisitorNPCConfig>();

    [Header("=== WAYPOINT CHUNG CHO NPC KHACH ===")]
    [Tooltip("Diem xuat hien cua NPC khach (xa cong bao canh)")]
    public Transform visitorSpawnPoint;

    [Tooltip("Diem NPC khach dung lai de xuat trinh giay to")]
    public Transform visitorStopPoint;

    [Tooltip("Diem NPC khach di den sau khi duoc CHO VAO (se bien mat tai day)")]
    public Transform visitorLetThroughPoint;

    [Tooltip("Vi tri giay to xuat hien mac dinh (dung khi NPC khach khong co Document Spawn Point rieng)")]
    public Transform documentShowPoint;

    [Tooltip("Giay to se bien mat sau bao lau (giay) ke tu khi bat dau thoai. 0 = bien mat khi thoai ket thuc)")]
    public float documentHideDelay = 0f;

    public float visitorMoveSpeed = 1.4f;
    public float visitorRotateSpeed = 5f;

    // --------------------------------------------------------
    //  DIEN THOAI (SAU KHI TAT CA NPC KHACH XONG)
    // --------------------------------------------------------
    [Header("=== DIEN THOAI (Sau toan bo NPC khach) ===")]
    public InteractablePhone phone;

    [Tooltip("Thoai qua dien thoai (dinh dang 'Ten: Loi' hoac loi dan thuyet)")]
    public List<string> phoneDialogue = new List<string>();

    // --------------------------------------------------------
    //  TASK
    // --------------------------------------------------------
    [Header("=== TASK (Sau cutscene) ===")]
    [Tooltip("Tu dong hien danh sach task sau khi phone call ket thuc?")]
    public bool showTasksAfterCutscene = true;

    [Tooltip("Thoi gian cho (giay) truoc khi hien task")]
    public float taskShowDelay = 0.5f;

    // --------------------------------------------------------
    //  CAI DAT CHUNG
    // --------------------------------------------------------
    [Header("=== CAI DAT ===")]
    public float startDelay = 1.5f;
    public float letterboxHeight = 100f;
    public float letterboxDuration = 0.6f;
    public float charsPerSecond = 38f;
    public float minReadTime = 2.0f;
    public bool enableCutscene = true;
    public bool allowClickToAdvance = true;

    [Header("=== SKIP ===")]
    public Button skipButton;

    // --------------------------------------------------------
    //  GROUND SNAP
    // --------------------------------------------------------
    [Header("=== GROUND SNAP ===")]
    [Tooltip("LayerMask mat dat (de NPC khong bay). Chon tat ca cac layer la mat dat.")]
    public LayerMask groundLayer = ~0;

    [Tooltip("Offset Y sau khi snap xuong dat (chinh lai neu NPC con chim hoac con bay)")]
    public float groundOffset = 0f;

    // --------------------------------------------------------
    //  BACKWARD COMPAT (an di, giu du lieu cu)
    // --------------------------------------------------------
    [SerializeField, HideInInspector] private string line1;
    [SerializeField, HideInInspector] private string line2;
    [SerializeField, HideInInspector] private string line3;
    [SerializeField, HideInInspector] private string line4;
    [SerializeField, HideInInspector] private string line5;
    [SerializeField, HideInInspector] private string line6;

    // --------------------------------------------------------
    //  RUNTIME UI
    // --------------------------------------------------------
    private GameObject      _dlgPanel;
    private TextMeshProUGUI _dlgText;
    private TextMeshProUGUI _dlgSpeaker;
    private CanvasGroup     _dlgGroup;
    private RectTransform   _lbT, _lbB;

    // --------------------------------------------------------
    //  TRANG THAI
    // --------------------------------------------------------
    private bool      _skipped, _running;
    private Coroutine _co, _twCo;

    // ============================================================
    //  UNITY
    // ============================================================
    private void Start()
    {
        BuildDialogueUI();
        BuildLetterboxUI();

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(Skip);
        }

        // An task trong cutscene
        if (TaskManager.instance != null)
            TaskManager.instance.SetTaskUIVisible(false);

        if (phone != null) phone.StopRinging();
        if (enableCutscene) { LockPlayer(true); _co = StartCoroutine(RunCutscene()); } else { Finish(); }
        // Cutscene started conditionally
    }

    // ============================================================
    //  MAIN CUTSCENE
    // ============================================================
    private IEnumerator RunCutscene()
    {
        _running = true;
        yield return new WaitForSeconds(startDelay);
        if (_skipped) { Finish(); yield break; }

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        // Mo letterbox
        yield return StartCoroutine(LB(true));
        yield return new WaitForSeconds(0.3f);

        // ---- ENTITY NPC ----
        if (entityPrefab != null || entitySpawnPoint != null)
        {
            yield return StartCoroutine(RunEntityNPC());
            if (_skipped) { Finish(); yield break; }
        }

        // ---- VISITOR NPCs ----
        foreach (var cfg in visitorNPCs)
        {
            if (_skipped) { Finish(); yield break; }
            yield return StartCoroutine(RunVisitorNPC(cfg));
        }

        if (_skipped) { Finish(); yield break; }

        // ---- PHONE CALL ----
        yield return StartCoroutine(RunPhoneCall());
        if (_skipped) { Finish(); yield break; }

        // Dong letterbox
        yield return StartCoroutine(FadeDlg(false));
        yield return StartCoroutine(LB(false));
        yield return new WaitForSeconds(0.3f);

        // Hien task
        if (showTasksAfterCutscene)
        {
            yield return new WaitForSeconds(taskShowDelay);
            if (TaskManager.instance != null)
                TaskManager.instance.SetTaskUIVisible(true);
        }

        Finish();
    }

    // ============================================================
    //  ENTITY NPC SEQUENCE
    // ============================================================
    private IEnumerator RunEntityNPC()
    {
        Vector3 spawnPos = entitySpawnPoint != null ? entitySpawnPoint.position : transform.position + Vector3.back * 20f;
        GameObject entity = SpawnNPC(entityPrefab, spawnPos, entityStopPoint);

        yield return StartCoroutine(MoveNPC(entity, entityStopPoint, entityMoveSpeed, entityRotateSpeed));
        if (_skipped) { if (entity) Destroy(entity); yield break; }

        LookAtCamera(entity);
        yield return new WaitForSeconds(0.5f);

        Flash(0.4f);
        yield return StartCoroutine(PlayDialogueList(entityDialogue));
        if (_skipped) { if (entity) Destroy(entity); yield break; }

        Flash(0.6f);
        yield return new WaitForSeconds(0.2f);
        if (entityExitPoint != null)
            yield return StartCoroutine(MoveNPC(entity, entityExitPoint, entityMoveSpeed, entityRotateSpeed));

        if (entity) Destroy(entity);
        yield return new WaitForSeconds(0.5f);
    }

    // ============================================================
    //  VISITOR NPC SEQUENCE
    // ============================================================
    private IEnumerator RunVisitorNPC(VisitorNPCConfig cfg)
    {
        // Spawn NPC
        Vector3 spawnPos = visitorSpawnPoint != null ? visitorSpawnPoint.position : transform.position + Vector3.back * 15f;
        GameObject npc = SpawnNPC(cfg.npcPrefab, spawnPos, visitorStopPoint);

        // Di den diem dung
        yield return StartCoroutine(MoveNPC(npc, visitorStopPoint, visitorMoveSpeed, visitorRotateSpeed));
        if (_skipped) { if (npc) Destroy(npc); yield break; }

        // Nhin ve camera (xuat trinh giay to)
        LookAtCamera(npc);
        yield return new WaitForSeconds(0.3f);

        // --- Xuat hien giay to ---
        GameObject docObj = null;
        Coroutine docHideCo = null;

        if (cfg.documentPrefab != null)
        {
            // Uu tien vi tri rieng cua NPC, neu khong co thi dung vi tri chung
            Transform spawnTf = (cfg.documentSpawnPoint != null) ? cfg.documentSpawnPoint : documentShowPoint;

            if (spawnTf != null)
            {
                docObj = Instantiate(cfg.documentPrefab, spawnTf.position, spawnTf.rotation);
            }
            else
            {
                // Khong co Transform -> xuat hien truoc mat NPC
                Vector3 docPos = npc != null
                    ? npc.transform.position + npc.transform.forward * 0.6f + Vector3.up * 1.2f
                    : spawnPos + Vector3.up * 1.2f;
                docObj = Instantiate(cfg.documentPrefab, docPos, Quaternion.identity);
            }

            // Neu co delay bien mat rieng thi hen gio
            if (documentHideDelay > 0f && docObj != null)
            {
                docHideCo = StartCoroutine(DestroyAfterDelay(docObj, documentHideDelay));
            }
        }

        // --- Thoai ---
        if (cfg.dialogueLines != null && cfg.dialogueLines.Count > 0)
        {
            yield return StartCoroutine(PlayDialogueList(cfg.dialogueLines));
        }

        // An giay to khi thoai xong (neu chua tu bien mat)
        if (docObj != null)
        {
            if (docHideCo != null) StopCoroutine(docHideCo);
            yield return StartCoroutine(FadeOutAndDestroyNPC(docObj, 0.25f));
        }

        if (_skipped) { if (npc) Destroy(npc); yield break; }

        // --- Xu ly ket qua ---
        if (cfg.outcome == NPCOutcome.LetThrough)
        {
            yield return new WaitForSeconds(0.3f);
            if (visitorLetThroughPoint != null)
                yield return StartCoroutine(MoveNPC(npc, visitorLetThroughPoint, visitorMoveSpeed * 1.2f, visitorRotateSpeed));
            yield return StartCoroutine(FadeOutAndDestroyNPC(npc, 0.5f));
        }
        else // Eliminate
        {
            yield return new WaitForSeconds(0.2f);
            Flash(0.9f);
            yield return new WaitForSeconds(0.15f);
            Flash(0.6f);
            if (npc) Destroy(npc);
        }

        yield return new WaitForSeconds(cfg.pauseBeforeNext);
    }

    // ============================================================
    //  PHONE CALL SEQUENCE
    // ============================================================
    private IEnumerator RunPhoneCall()
    {
        if (phone != null) phone.StartRinging();
        yield return new WaitForSeconds(0.6f);

        if (phoneDialogue != null && phoneDialogue.Count > 0)
            yield return StartCoroutine(PlayDialogueList(phoneDialogue));

        if (phone != null) phone.StopRinging();
    }

    // ============================================================
    //  DIALOGUE HELPERS
    // ============================================================
    private IEnumerator PlayDialogueList(List<string> lines)
    {
        if (lines == null) yield break;
        foreach (string rawLine in lines)
        {
            if (_skipped) yield break;
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            yield return StartCoroutine(ShowDialogueLine(rawLine));
        }
    }

    private IEnumerator ShowDialogueLine(string rawLine)
    {
        ParseLine(rawLine, out string speaker, out string text);

        if (_dlgSpeaker != null)
        {
            _dlgSpeaker.text = speaker;
            _dlgSpeaker.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        }
        if (_dlgText != null) _dlgText.text = "";

        yield return StartCoroutine(FadeDlg(true));

        if (_twCo != null) StopCoroutine(_twCo);
        _twCo = StartCoroutine(TypewriterWithSkip(text));
        yield return _twCo;
        _twCo = null;

        float readWait = Mathf.Max(minReadTime, text.Length * 0.045f);
        float elapsed = 0f;
        while (elapsed < readWait && !_skipped)
        {
            if (allowClickToAdvance && WasAdvancePressed()) { yield return new WaitForSeconds(0.1f); break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ParseLine(string rawLine, out string speaker, out string text)
    {
        speaker = "";
        text = rawLine != null ? rawLine.Trim() : "";
        if (string.IsNullOrEmpty(text)) return;

        int firstNewline = text.IndexOf('\n');
        int colon = text.IndexOf(':');

        if (colon > 0 && (firstNewline == -1 || colon < firstNewline) && colon <= 45)
        {
            string candidate = text.Substring(0, colon).Trim();
            bool isTime = true;
            foreach (char c in candidate)
                if (!char.IsDigit(c) && !char.IsWhiteSpace(c)) { isTime = false; break; }
            if (!isTime) { speaker = candidate; text = text.Substring(colon + 1).Trim(); }
        }
    }

    private IEnumerator TypewriterWithSkip(string text)
    {
        if (_dlgText == null) yield break;
        _dlgText.text = text;
        _dlgText.maxVisibleCharacters = 0;
        _dlgText.ForceMeshUpdate();

        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i < text.Length; i++)
        {
            if (_skipped) yield break;
            if (allowClickToAdvance && i > 1 && WasAdvancePressed())
            {
                _dlgText.maxVisibleCharacters = text.Length;
                yield return new WaitForSeconds(0.12f);
                yield break;
            }
            _dlgText.maxVisibleCharacters = i + 1;
            char c = text[i];
            float wait = (c == '.' || c == '!' || c == '?') ? delay * 5f : (c == ',') ? delay * 2.5f : delay;
            float timer = 0f;
            while (timer < wait)
            {
                if (allowClickToAdvance && i > 1 && WasAdvancePressed())
                {
                    _dlgText.maxVisibleCharacters = text.Length;
                    yield return new WaitForSeconds(0.12f);
                    yield break;
                }
                timer += Time.deltaTime;
                yield return null;
            }
        }
        _dlgText.maxVisibleCharacters = text.Length;
    }

    private bool WasAdvancePressed()
    {
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    // ============================================================
    //  NPC SPAWN & MOVE (CO GROUND SNAP)
    // ============================================================
    private GameObject SpawnNPC(GameObject prefab, Vector3 pos, Transform lookTarget)
    {
        pos = SnapToGround(pos);
        GameObject npc = prefab != null
            ? Instantiate(prefab, pos, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        npc.transform.position = pos;
        npc.name = "CutsceneNPC_" + (prefab != null ? prefab.name : "Capsule");

        foreach (var col in npc.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Rigidbody rb = npc.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (lookTarget != null)
        {
            Vector3 d = lookTarget.position - pos; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) npc.transform.rotation = Quaternion.LookRotation(d);
        }
        return npc;
    }

    private IEnumerator MoveNPC(GameObject npc, Transform target, float speed, float rotSpeed)
    {
        if (npc == null || target == null) yield break;
        float timeout = 8f;
        float timer = 0f;
        while (!_skipped && npc != null && timer < timeout)
        {
            timer += Time.deltaTime;
            float groundY  = SnapToGround(npc.transform.position).y;
            float targetY  = SnapToGround(target.position).y;
            Vector3 dest   = new Vector3(target.position.x, targetY, target.position.z);
            Vector3 myPos  = new Vector3(npc.transform.position.x, groundY, npc.transform.position.z);

            if (Vector3.Distance(myPos, dest) < 0.18f)
            {
                npc.transform.position = new Vector3(dest.x, groundY, dest.z);
                break;
            }

            Vector3 moveDir = (dest - myPos).normalized;
            Vector3 newPos  = myPos + moveDir * speed * Time.deltaTime;
            newPos.y = groundY;
            npc.transform.position = newPos;

            Vector3 dir2D = new Vector3(moveDir.x, 0f, moveDir.z);
            if (dir2D.sqrMagnitude > 0.01f)
                npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(dir2D), rotSpeed * Time.deltaTime);

            yield return null;
        }
    }

    private Vector3 SnapToGround(Vector3 pos)
    {
        Vector3 rayOrigin = pos + Vector3.up * 3f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            return new Vector3(pos.x, hit.point.y + groundOffset, pos.z);
        return pos;
    }

    private void LookAtCamera(GameObject npc)
    {
        if (npc == null) return;
        Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam == null) return;
        Vector3 d = cam.transform.position - npc.transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.001f) npc.transform.rotation = Quaternion.LookRotation(d);
    }

    private IEnumerator FadeOutAndDestroyNPC(GameObject obj, float duration)
    {
        if (obj == null) yield break;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
            foreach (var m in r.materials)
                mats.Add(m);

        foreach (var m in mats)
        {
            if (m.HasProperty("_Color"))
            {
                Color c = m.color; c.a = 1f; m.color = c;
                m.SetFloat("_Mode", 2);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            foreach (var m in mats)
                if (m.HasProperty("_Color")) { Color c = m.color; c.a = alpha; m.color = c; }
            yield return null;
        }
        if (obj) Destroy(obj);
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FadeOutAndDestroyNPC(obj, 0.25f));
    }

    // ============================================================
    //  BUILD UI
    // ============================================================
    private void BuildDialogueUI()
    {
        GameObject cgo = new GameObject("GameEvent_DialogueCanvas");
        Canvas canvas  = cgo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cgo.AddComponent<GraphicRaycaster>();

        _dlgPanel = new GameObject("DlgPanel");
        _dlgPanel.transform.SetParent(cgo.transform, false);
        Image bg = _dlgPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f); bg.raycastTarget = false;
        _dlgGroup = _dlgPanel.AddComponent<CanvasGroup>();
        _dlgGroup.alpha = 0f;

        RectTransform panelRT = _dlgPanel.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0f, 0f);
        panelRT.anchorMax        = new Vector2(1f, 0f);
        panelRT.pivot            = new Vector2(0.5f, 0f);
        panelRT.sizeDelta        = new Vector2(0f, 130f);
        panelRT.anchoredPosition = Vector2.zero;

        GameObject speakerGo = new GameObject("SpeakerName");
        speakerGo.transform.SetParent(_dlgPanel.transform, false);
        _dlgSpeaker = speakerGo.AddComponent<TextMeshProUGUI>();
        _dlgSpeaker.fontSize  = 18f;
        _dlgSpeaker.fontStyle = FontStyles.Bold;
        _dlgSpeaker.color     = new Color(1f, 0.85f, 0.3f);
        _dlgSpeaker.text      = "";
        RectTransform spRT    = speakerGo.GetComponent<RectTransform>();
        spRT.anchorMin        = new Vector2(0f, 1f); spRT.anchorMax = new Vector2(1f, 1f);
        spRT.pivot            = new Vector2(0.5f, 0f);
        spRT.anchoredPosition = new Vector2(0f, 6f);
        spRT.sizeDelta        = new Vector2(-80f, 26f);

        GameObject textGo = new GameObject("DlgText");
        textGo.transform.SetParent(_dlgPanel.transform, false);
        _dlgText = textGo.AddComponent<TextMeshProUGUI>();
        _dlgText.fontSize = 22f; _dlgText.color = Color.white;
        _dlgText.text = ""; _dlgText.enableWordWrapping = true;
        RectTransform txRT = textGo.GetComponent<RectTransform>();
        txRT.anchorMin = Vector2.zero; txRT.anchorMax = Vector2.one;
        txRT.offsetMin = new Vector2(40f, 12f); txRT.offsetMax = new Vector2(-40f, -6f);
    }

    private void BuildLetterboxUI()
    {
        GameObject cgo = new GameObject("GameEvent_LetterboxCanvas");
        Canvas c = cgo.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 997;
        cgo.AddComponent<CanvasScaler>(); cgo.AddComponent<GraphicRaycaster>();
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

    // ============================================================
    //  UTILS
    // ============================================================
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
        while (t < letterboxDuration)
        {
            t += Time.deltaTime;
            SetLBH(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / letterboxDuration)));
            yield return null;
        }
        SetLBH(to);
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

    // ============================================================
    //  SKIP & FINISH
    // ============================================================
    public void Skip()
    {
        if (!_running) return;
        _skipped = true;
        if (_co   != null) StopCoroutine(_co);
        if (_twCo != null) StopCoroutine(_twCo);
        if (phone != null) phone.StopRinging();
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
        if (showTasksAfterCutscene && TaskManager.instance != null)
            TaskManager.instance.SetTaskUIVisible(true);
    }

    private void OnDestroy() { if (phone != null) phone.StopRinging(); }
}
