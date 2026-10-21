using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Quan ly danh sach nhiem vu don gian.
/// Singleton - dat script nay len bat ky GameObject nao trong scene.
/// </summary>
public class TaskManager_Old : MonoBehaviour
{
    public static TaskManager_Old instance;

    [Header("=== NHIEM VU ===")]
    [Tooltip("Bat buoc doc bao truoc khi hoan thanh nhiem vu?")]
    public bool requireNewspaper = true;

    [Header("=== GIAO DIEN ===")]
    [Tooltip("TextMeshProUGUI hien thi task. De trong = tu dong tao.")]
    public TextMeshProUGUI taskTextUI;

    [Header("=== AM THANH ===")]
    public AudioClip taskCompleteSound;

    // Tien do
    [HideInInspector] public bool newspaperRead = false;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        EnsureTaskUI();
        UpdateTaskUI();
    }

    // ============================================================
    //  API CONG KHAI
    // ============================================================
    public void SetTaskUIVisible(bool visible)
    {
        if (taskTextUI != null)
            taskTextUI.gameObject.SetActive(visible);
    }

    public void CompleteNewspaper()
    {
        if (newspaperRead) return;
        newspaperRead = true;
        Debug.Log("[TaskManager] Da doc bao!");
        PlayCompleteSound();
        UpdateTaskUI();
    }

    public bool AreAllTasksDone()
    {
        if (requireNewspaper && !newspaperRead) return false;
        return true;
    }

    // ============================================================
    //  CAP NHAT HUD
    // ============================================================
    public void UpdateTaskUI()
    {
        if (taskTextUI == null) return;

        string paperLine = "";
        if (requireNewspaper)
        {
            paperLine = newspaperRead
                ? "<color=#00FF88>[v] Doc to bao trong bot</color>\n"
                : "[ ] Doc to bao trong bot\n";
        }

        taskTextUI.text = "<b>NHIEM VU:</b>\n" + paperLine;
    }

    // ============================================================
    //  TU DONG TAO UI
    // ============================================================
    private void EnsureTaskUI()
    {
        if (taskTextUI != null) return;

        GameObject cgo = new GameObject("TaskCanvas");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        UnityEngine.UI.CanvasScaler cs = cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject textGO = new GameObject("TaskText");
        textGO.transform.SetParent(cgo.transform, false);
        taskTextUI = textGO.AddComponent<TextMeshProUGUI>();
        taskTextUI.fontSize = 22f;
        taskTextUI.color    = Color.white;
        taskTextUI.textWrappingMode = TextWrappingModes.Normal;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, -16f);
        rt.sizeDelta        = new Vector2(400f, 300f);
    }

    private void PlayCompleteSound()
    {
        if (taskCompleteSound != null)
            AudioSource.PlayClipAtPoint(taskCompleteSound,
                Camera.main != null ? Camera.main.transform.position : transform.position);
    }
}