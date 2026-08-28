using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý danh sách nhiệm vụ (Quest List) & Hiệu ứng hù dọa tăng tiến cho Night-1.
/// Tự động hiển thị bảng nhiệm vụ trên màn hình và mở khóa giường ngủ khi hoàn thành.
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager instance;

    [Header("=== CẤU HÌNH SỐ LƯỢNG NHIỆM VỤ ===")]
    [Tooltip("Số đống lá cần quét ngoài sân")]
    public int totalLeaves = 5;

    [Tooltip("Số điểm hàng rào cần tuần tra")]
    public int totalFencePoints = 6;

    [Header("=== GIAO DIỆN HIỂN THỊ (TASK HUD) ===")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị danh sách nhiệm vụ vào đây (nếu để trống script tự động tạo góc trái màn hình)")]
    public TextMeshProUGUI taskTextUI;

    [Header("=== ÂM THANH HOÀN THÀNH NHIỆM VỤ ===")]
    public AudioClip taskCompleteSound;

    // Tiến độ hiện tại
    [HideInInspector] public int leavesSwept = 0;
    [HideInInspector] public int fencePointsChecked = 0;

    private Coroutine cameraShakeCoroutine;

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
        EnsureTaskUI();
        UpdateTaskUI();
    }

    // ================= XỬ LÝ NHIỆM VỤ 1: QUÉT 5 ĐỐNG LÁ =================
    public void CompleteLeafPile(int leafIndex)
    {
        leavesSwept++;
        Debug.Log($"[TaskManager] Đã quét đống lá: {leavesSwept}/{totalLeaves}");

        // Kích hoạt hù dọa tăng tiến theo thứ tự đống lá (1 -> 5)
        switch (leafIndex)
        {
            case 1:
                // Đống 1: Bình thường, tiếng lá xào xạc
                Debug.Log("[Horror] Đống lá 1: Quét nhẹ nhàng.");
                break;
            case 2:
                // Đống 2: Tiếng cành cây gãy khẽ sau lưng
                Debug.Log("[Horror] Đống lá 2: Cành cây gãy khẽ.");
                break;
            case 3:
                // Đống 3: Đèn pin chớp tắt 1 nhịp
                Debug.Log("[Horror] Đống lá 3: Đèn pin chớp tắt 1 nhịp.");
                FlickerFlashlight(1, 0.12f);
                break;
            case 4:
                // Đống 4: Tiếng thì thầm ma quái bên tai
                Debug.Log("[Horror] Đống lá 4: Tiếng thì thầm ma quái!");
                FlickerFlashlight(2, 0.1f);
                break;
            case 5:
            default:
                // Đống 5: Đèn pin chớp tắt liên tục + tiếng động lạ đập mạnh
                Debug.Log("[Horror] Đống lá 5: Đèn chớp liên hồi + tiếng đập mạnh!");
                FlickerFlashlight(4, 0.08f);
                break;
        }

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    // ================= XỬ LÝ NHIỆM VỤ 2: TUẦN TRA 6 ĐIỂM HÀNG RÀO =================
    public void CompleteFencePatrol(int fenceIndex)
    {
        fencePointsChecked++;
        Debug.Log($"[TaskManager] Đã tuần tra hàng rào: {fencePointsChecked}/{totalFencePoints}");

        // Kích hoạt hù dọa tăng tiến theo thứ tự điểm rào (1 -> 6)
        switch (fenceIndex)
        {
            case 1:
                // Điểm 1: Gió lạnh rít nhẹ
                Debug.Log("[Horror] Điểm rào 1: Yên ắng, gió rít.");
                break;
            case 2:
                // Điểm 2: Tiếng rung rào sắt ở xa
                Debug.Log("[Horror] Điểm rào 2: Tiếng rung rào sắt đằng xa.");
                break;
            case 3:
                // Điểm 3: Tiếng cào xước nhẹ vào lưới rào
                Debug.Log("[Horror] Điểm rào 3: Tiếng cào xước rào sắt.");
                break;
            case 4:
                // Điểm 4: Đèn pin chớp tắt và tối om trong 1 giây
                Debug.Log("[Horror] Điểm rào 4: Đèn pin tắt tối om 1 giây!");
                FlickerFlashlight(3, 0.12f, 1.0f);
                break;
            case 5:
                // Điểm 5: Tiếng cào rào sắt dữ dội + tiếng thở dốc
                Debug.Log("[Horror] Điểm rào 5: Cào rào dữ dội + tiếng thở dốc!");
                FlickerFlashlight(4, 0.08f);
                break;
            case 6:
            default:
                // Điểm 6: Đỉnh điểm bất ngờ! Camera Shake + tiếng gầm gừ + đèn chớp
                Debug.Log("[Horror] Điểm rào 6: Đỉnh điểm bất ngờ! Camera Shake + Jumpscare!");
                TriggerCameraShake(0.6f, 0.15f);
                FlickerFlashlight(5, 0.07f);
                break;
        }

        UpdateTaskUI();
        CheckAllTasksDone();
    }

    public bool AreAllTasksCompleted()
    {
        return (leavesSwept >= totalLeaves) && (fencePointsChecked >= totalFencePoints);
    }

    private void CheckAllTasksDone()
    {
        if (AreAllTasksCompleted())
        {
            Debug.Log("[TaskManager] ĐÃ HOÀN THÀNH TẤT CẢ NHIỆM VỤ! Mở khóa giường ngủ.");
            PlayTaskCompleteSound();
        }
    }

    private void PlayTaskCompleteSound()
    {
        AudioClip clipToPlay = taskCompleteSound;
        if (clipToPlay == null)
        {
            clipToPlay = GenerateTaskCompleteChime();
        }

        if (clipToPlay != null)
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(clipToPlay);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clipToPlay, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }
    }

    /// <summary>
    /// Tự động tạo âm thanh chuông ngân hoàn thành nhiệm vụ trong trẻo chuẩn 44.1kHz
    /// </summary>
    private AudioClip GenerateTaskCompleteChime()
    {
        int sampleRate = 44100;
        float duration = 1.8f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Hợp âm ngân vang: E4, G#4, B4, E5, B5
        float[] freqs = { 329.63f, 415.30f, 493.88f, 659.25f, 987.77f };
        float[] startTimes = { 0.0f, 0.12f, 0.24f, 0.36f, 0.48f };
        float[] amps = { 0.45f, 0.55f, 0.65f, 0.85f, 0.40f };

        for (int n = 0; n < freqs.Length; n++)
        {
            float freq = freqs[n];
            int startIdx = (int)(startTimes[n] * sampleRate);
            int noteSamples = sampleCount - startIdx;

            for (int i = 0; i < noteSamples; i++)
            {
                int idx = startIdx + i;
                if (idx >= sampleCount) break;

                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 2.8f);
                float val = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f
                          + Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.25f * Mathf.Exp(-t * 5.0f)
                          + Mathf.Sin(2f * Mathf.PI * freq * 3.01f * t) * 0.1f * Mathf.Exp(-t * 7.0f);

                samples[idx] += val * env * amps[n];
            }
        }

        // Chuẩn hóa âm lượng (Normalize)
        float maxVal = 0.001f;
        for (int i = 0; i < sampleCount; i++)
        {
            if (Mathf.Abs(samples[i]) > maxVal) maxVal = Mathf.Abs(samples[i]);
        }
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = (samples[i] / maxVal) * 0.85f;
        }

        AudioClip clip = AudioClip.Create("TaskCompleteChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ================= CẬP NHẬT GIAO DIỆN TASK HUD =================
    public void UpdateTaskUI()
    {
        if (taskTextUI == null) return;

        bool leafDone = (leavesSwept >= totalLeaves);
        bool fenceDone = (fencePointsChecked >= totalFencePoints);
        bool allDone = leafDone && fenceDone;

        string leafStatus = leafDone ? $"<color=#00FF88>[v] Sweep the leaves ({leavesSwept}/{totalLeaves})</color>" : $"[ ] Sweep the leaves ({leavesSwept}/{totalLeaves})";
        string fenceStatus = fenceDone ? $"<color=#00FF88>[v] Patrol perimeter fence ({fencePointsChecked}/{totalFencePoints})</color>" : $"[ ] Patrol perimeter fence ({fencePointsChecked}/{totalFencePoints})";
        
        string bedStatus = allDone 
            ? "<color=#FFFF00>[ ] Go to sleep in bedroom</color>" 
            : "<color=#888888>[ ] Go to sleep (Complete chores first)</color>";

        taskTextUI.text = $"<b>NIGHT 1 TASKS:</b>\n" +
                          $"{leafStatus}\n" +
                          $"{fenceStatus}\n" +
                          $"{bedStatus}";
    }

    // ================= CÁC HIỆU ỨNG HÙ DỌA (HORROR EFFECTS) =================

    /// <summary>
    /// Làm đèn pin nhấp nháy chập chờn và có thể tắt tối om 1 lát
    /// </summary>
    public void FlickerFlashlight(int flickerCount, float speed, float blackoutDuration = 0f)
    {
        StartCoroutine(FlickerFlashlightRoutine(flickerCount, speed, blackoutDuration));
    }

    private IEnumerator FlickerFlashlightRoutine(int flickerCount, float speed, float blackoutDuration)
    {
        FlashlightController fController = FindObjectOfType<FlashlightController>();
        if (fController == null) yield break;

        Light flLight = fController.GetComponentInChildren<Light>();
        if (flLight == null || !flLight.enabled) yield break;

        bool originalState = flLight.enabled;

        for (int i = 0; i < flickerCount; i++)
        {
            flLight.enabled = !flLight.enabled;
            yield return new WaitForSeconds(speed);
            flLight.enabled = !flLight.enabled;
            yield return new WaitForSeconds(speed);
        }

        if (blackoutDuration > 0f)
        {
            flLight.enabled = false;
            yield return new WaitForSeconds(blackoutDuration);
            flLight.enabled = originalState;
        }
        else
        {
            flLight.enabled = originalState;
        }
    }

    /// <summary>
    /// Rung lắc màn hình nhẹ tạo cảm giác giật mình (Camera Shake)
    /// </summary>
    public void TriggerCameraShake(float duration, float magnitude)
    {
        if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
        cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }

    // Tự động tìm hoặc tạo Task Text góc trên trái màn hình
    private void EnsureTaskUI()
    {
        if (taskTextUI != null) return;

        // 1. Tìm xem bên trong Player hoặc các con có sẵn TextMeshProUGUI chưa
        taskTextUI = GetComponentInChildren<TextMeshProUGUI>();
        if (taskTextUI != null) return;

        // 2. Tìm Canvas con bên trong Player hoặc trong Scene
        Canvas targetCanvas = GetComponentInChildren<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        // 3. Nếu chưa có Canvas nào thì tự tạo mới
        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("TaskCanvas");
            targetCanvas = canvasGO.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GameObject textGO = new GameObject("TaskTextUI");
        textGO.transform.SetParent(targetCanvas.transform, false);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        // Tự động nạp Font Roboto-Bold SDF
        TMP_FontAsset robotoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (robotoFont != null)
        {
            tmp.font = robotoFont;
        }

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f);
        rt.sizeDelta = new Vector2(450f, 200f);

        taskTextUI = tmp;
    }
}
