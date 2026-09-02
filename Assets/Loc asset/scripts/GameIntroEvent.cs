using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameIntroEvent - Su kien mo dau game: dien thoai reo, khach la di vao.
/// Gan vao 1 Empty GameObject trong Scene.
/// </summary>
public class GameIntroEvent : MonoBehaviour
{
    [Header("=== Tham chieu bat buoc ===")]
    public DialogueUI dialogueUI;
    public InteractablePhone phone;

    [Header("=== NPC Vi Khach La ===")]
    public GameObject strangerPrefab;
    public Transform strangerSpawnPoint;
    public Transform strangerStopPoint;
    public Transform strangerExitPoint;
    public float npcMoveSpeed = 1.6f;
    public float npcRotateSpeed = 5f;

    [Header("=== Do tre bat dau (giay) ===")]
    public float startDelay = 1.5f;

    [Header("=== Letterbox ===")]
    public float letterboxHeight = 100f;
    public float letterboxDuration = 0.6f;

    [Header("=== Skip ===")]
    public Button skipButton;

    [Header("=== Noi dung thoai ===")]
    [TextArea(2,4)] public string line1 = "HE THONG: Phat hien tin hieu la tu ngoai vanh dai...";
    [TextArea(2,4)] public string line2 = "HE THONG: CANH BAO! Di the dang tien den bot gac!";
    [TextArea(2,4)] public string line3 = "NGUOI LA: ...Nguoi la nguoi canh giu noi nay?";
    [TextArea(2,4)] public string line4 = "NGUOI LA: Ta da quan sat nguoi tu rat lau roi...";
    [TextArea(2,4)] public string line5 = "NGUOI LA: Bot canh nay se la tuyen phong thu cuoi cung.";
    [TextArea(2,4)] public string line6 = "HE THONG: Di the bien mat. Ca truc bat dau. Hay nghe dien thoai.";

    private GameObject _npc;
    private bool _skipped;
    private bool _running;
    private Coroutine _co;
    private RectTransform _lbT, _lbB;

    private void Start()
    {
        EnsureLetterbox();
        if (skipButton != null) { skipButton.gameObject.SetActive(false); skipButton.onClick.AddListener(Skip); }
        if (phone != null) phone.StopRinging();
        LockPlayer(true);
        _co = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _running = true;
        yield return new WaitForSeconds(startDelay);
        if (_skipped) { Finish(); yield break; }

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        yield return StartCoroutine(LB(true));
        yield return new WaitForSeconds(0.3f);

        if (phone != null) phone.StartRinging();
        Say(line1, "HE THONG", 3.5f);
        yield return new WaitForSeconds(3.8f);
        if (_skipped) { Finish(); yield break; }

        if (phone != null) phone.StopRinging();
        Flash(0.5f);
        yield return new WaitForSeconds(0.3f);

        Say(line2, "HE THONG", 3.0f);
        yield return new WaitForSeconds(1.0f);
        if (_skipped) { Finish(); yield break; }

        SpawnNPC();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MoveNPC(strangerStopPoint));
        if (_skipped) { Finish(); yield break; }

        if (_npc != null && Camera.main != null)
        {
            Vector3 d = Camera.main.transform.position - _npc.transform.position;
            d.y = 0; if (d != Vector3.zero) _npc.transform.rotation = Quaternion.LookRotation(d);
        }
        yield return new WaitForSeconds(0.5f);
        if (_skipped) { Finish(); yield break; }

        Flash(0.4f);
        Say(line3, "NGUOI LA", 3.5f); yield return new WaitForSeconds(4f);
        if (_skipped) { Finish(); yield break; }
        Say(line4, "NGUOI LA", 4f);   yield return new WaitForSeconds(4.5f);
        if (_skipped) { Finish(); yield break; }
        Say(line5, "NGUOI LA", 4f);   yield return new WaitForSeconds(4.5f);
        if (_skipped) { Finish(); yield break; }

        Flash(0.7f); yield return new WaitForSeconds(0.2f); Flash(0.5f);
        if (strangerExitPoint != null) yield return StartCoroutine(MoveNPC(strangerExitPoint));
        if (_npc != null) Destroy(_npc);

        yield return new WaitForSeconds(0.4f);
        Say(line6, "HE THONG", 4f);
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(LB(false));
        yield return new WaitForSeconds(0.5f);
        Finish();
    }

    private void SpawnNPC()
    {
        Vector3 pos = strangerSpawnPoint != null ? strangerSpawnPoint.position : new Vector3(0,0,-25f);
        _npc = strangerPrefab != null
            ? Instantiate(strangerPrefab, pos, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _npc.transform.position = pos;
        _npc.name = "StrangerNPC_Event";
        Collider col = _npc.GetComponent<Collider>(); if (col != null) col.enabled = false;
        if (strangerStopPoint != null) {
            Vector3 d = strangerStopPoint.position - pos; d.y = 0;
            if (d.sqrMagnitude > 0.001f) _npc.transform.rotation = Quaternion.LookRotation(d);
        }
    }

    private IEnumerator MoveNPC(Transform target)
    {
        if (_npc == null || target == null) yield break;
        while (!_skipped && _npc != null) {
            Vector3 dest = new Vector3(target.position.x, _npc.transform.position.y, target.position.z);
            if (Vector3.Distance(_npc.transform.position, dest) < 0.18f) break;
            _npc.transform.position = Vector3.MoveTowards(_npc.transform.position, dest, npcMoveSpeed * Time.deltaTime);
            Vector3 dir = dest - _npc.transform.position;
            if (dir.sqrMagnitude > 0.01f)
                _npc.transform.rotation = Quaternion.Slerp(_npc.transform.rotation, Quaternion.LookRotation(dir), npcRotateSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void Say(string text, string speaker, float dur) { if (dialogueUI != null) dialogueUI.ShowDialogue(text, dur, speaker); }
    private void Flash(float i) { if (ScreenFlash.Instance != null) ScreenFlash.Instance.TriggerFlash(i); }

    private void LockPlayer(bool lockIt)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>() ?? FindObjectOfType<PlayerMovement>();
        MouseLook ml = FindFirstObjectByType<MouseLook>() ?? FindObjectOfType<MouseLook>();
        if (pm != null) pm.enabled = !lockIt;
        if (ml != null) ml.enabled = !lockIt;
        Cursor.lockState = lockIt ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = lockIt;
    }

    public void Skip()
    {
        if (!_running) return;
        _skipped = true;
        if (_co != null) StopCoroutine(_co);
        if (phone != null) phone.StopRinging();
        if (_npc != null) Destroy(_npc);
        if (dialogueUI != null) dialogueUI.HideDialogue();
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

    private void EnsureLetterbox()
    {
        GameObject cgo = new GameObject("LetterboxCanvas");
        Canvas c = cgo.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 997;
        cgo.AddComponent<CanvasScaler>(); cgo.AddComponent<GraphicRaycaster>();
        _lbT = MakeBar(cgo.transform, true);
        _lbB = MakeBar(cgo.transform, false);
        SetLBH(0f);
    }

    private RectTransform MakeBar(Transform p, bool top)
    {
        GameObject go = new GameObject(top ? "LB_T" : "LB_B"); go.transform.SetParent(p, false);
        Image img = go.AddComponent<Image>(); img.color = Color.black; img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0, top ? 1 : 0); rt.anchorMax = new Vector2(1, top ? 1 : 0);
        rt.pivot = new Vector2(0.5f, top ? 1 : 0); rt.sizeDelta = Vector2.zero;
        return rt;
    }

    private void SetLBH(float h) { if (_lbT) _lbT.sizeDelta = new Vector2(0,h); if (_lbB) _lbB.sizeDelta = new Vector2(0,h); }

    private IEnumerator LB(bool open)
    {
        float from = open ? 0 : letterboxHeight, to = open ? letterboxHeight : 0, t = 0;
        while (t < letterboxDuration) { t += Time.deltaTime; SetLBH(Mathf.Lerp(from, to, Mathf.SmoothStep(0,1,t/letterboxDuration))); yield return null; }
        SetLBH(to);
    }

    private void OnDestroy() { if (phone != null) phone.StopRinging(); }
}