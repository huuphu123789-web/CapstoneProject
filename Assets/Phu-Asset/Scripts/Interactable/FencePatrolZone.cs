using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Script dùng cho Điểm Tuần Tra Hàng Rào (Fence Patrol Checkpoint).
/// Kế thừa Interactable (Phú-Asset) - Hỗ trợ cả đi vào vùng Trigger hoặc bấm E để kiểm tra hàng rào.
/// Tự động tạo Icon / Marker 3D phát sáng Unlit lơ lửng (Billboard) để người chơi luôn thấy rõ trong bóng đêm.
/// Kích hoạt hù dọa tăng tiến và báo về TaskManager khi hoàn thành.
/// </summary>
public class FencePatrolZone : Interactable
{
    public enum TriggerMode { WalkIntoZone, PressEToInspect }

    [Header("=== CẤU HÌNH ĐIỂM TUẦN TRA ===")]
    [Tooltip("Số thứ tự điểm tuần tra (1 -> 6 theo danh sách của TaskManager)")]
    public int fenceIndex = 1;

    [Tooltip("Cách kích hoạt: Đi vào vùng (Trigger) hoặc Bấm E để kiểm tra")]
    public TriggerMode triggerMode = TriggerMode.PressEToInspect;

    [Header("=== ICON ĐÁNH DẤU (OBJECTIVE MARKER) ===")]
    [Tooltip("Bật/tắt hiển thị icon lơ lửng đánh dấu vị trí")]
    public bool showMarker = true;

    [Tooltip("Tùy chọn Sprite tùy chỉnh (nếu để trống sẽ tự tạo Icon hình thoi phát sáng đẹp mắt)")]
    public Sprite customMarkerSprite;

    [Tooltip("Màu sắc của Icon phát sáng trong đêm")]
    public Color markerColor = new Color(1f, 0.85f, 0.2f, 1f); // Màu vàng cam phát sáng rực rỡ

    [Tooltip("Chiều cao của Icon so với điểm rào")]
    public float markerHeight = 1.6f;

    [Tooltip("Kích thước của Icon")]
    public float markerScale = 0.55f;

    [Tooltip("Hiển thị chữ số thứ tự (ví dụ: #1, #2...) trên Icon")]
    public bool showPointNumber = true;

    [Tooltip("Hiển thị khoảng cách theo mét tới người chơi")]
    public bool showDistanceText = false;

    [Tooltip("Khoảng cách tối đa nhìn thấy Icon (0 = không giới hạn khoảng cách, luôn nhìn thấy xuyên đêm)")]
    public float maxVisibleDistance = 0f;

    [Header("=== ÂM THANH HÙ DỌA ===")]
    [Tooltip("Âm thanh riêng cho điểm rào này (tiếng rung rào, tiếng cào xước kim loại, tiếng gầm gừ...)")]
    public AudioClip fenceSound;
    public AudioSource localAudioSource;

    [Header("=== BÓNG MA / VẬT THỂ XUẤT HIỆN ===")]
    [Tooltip("Tùy chọn: GameObject bóng đen lướt qua hàng rào rồi biến mất")]
    public GameObject spookyVisualObject;

    private bool isTriggered = false;
    private GameObject markerObj;
    private TextMeshPro markerText;
    private SpriteRenderer markerSpriteRenderer;
    private Transform mainCameraTransform;
    private Vector3 initialMarkerLocalPos;

    void Awake()
    {
        // Tự động chuẩn hóa fenceIndex nếu nhập 0
        if (fenceIndex <= 0) fenceIndex = 1;

        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Start()
    {
        promptMessage = (triggerMode == TriggerMode.PressEToInspect) ? $"Inspect Fence #{fenceIndex}" : "";
        if (spookyVisualObject != null) spookyVisualObject.SetActive(false);

        FindPlayerCamera();

        if (showMarker)
        {
            CreateObjectiveMarker();
        }
    }

    private void FindPlayerCamera()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Camera cam = FindObjectOfType<Camera>();
            if (cam != null) mainCameraTransform = cam.transform;
        }
    }

    void LateUpdate()
    {
        if (isTriggered) return;

        // Nếu chưa tạo marker thì tạo ngay
        if (showMarker && markerObj == null)
        {
            CreateObjectiveMarker();
        }

        if (markerObj == null) return;

        // Cập nhật tìm camera nếu bị mất
        if (mainCameraTransform == null)
        {
            FindPlayerCamera();
            if (mainCameraTransform == null) return;
        }

        float distToPlayer = Vector3.Distance(transform.position, mainCameraTransform.position);

        // Kiểm tra khoảng cách hiển thị (0 = luôn hiện)
        bool shouldBeVisible = (maxVisibleDistance <= 0f || distToPlayer <= maxVisibleDistance);
        if (markerObj.activeSelf != shouldBeVisible)
        {
            markerObj.SetActive(shouldBeVisible);
        }

        if (!shouldBeVisible) return;

        // 1. Billboard: Luôn quay mặt 100% về phía Camera người chơi
        Vector3 dirToCam = markerObj.transform.position - mainCameraTransform.position;
        if (dirToCam.sqrMagnitude > 0.001f)
        {
            markerObj.transform.rotation = Quaternion.LookRotation(dirToCam);
        }

        // 2. Hiệu ứng nhấp nhô lơ lửng (Bobbing)
        float bobOffset = Mathf.Sin(Time.time * 2.5f + fenceIndex) * 0.08f;
        markerObj.transform.localPosition = initialMarkerLocalPos + new Vector3(0, bobOffset, 0);

        // 3. Hiệu ứng nhấp nháy phát sáng (Pulsing Glow)
        float pulseAlpha = Mathf.PingPong(Time.time * 1.5f, 0.35f) + 0.65f;
        if (markerSpriteRenderer != null)
        {
            Color c = markerColor;
            c.a = markerColor.a * pulseAlpha;
            markerSpriteRenderer.color = c;
        }

        // 4. Cập nhật chữ hiển thị
        if (markerText != null)
        {
            if (showDistanceText)
            {
                string label = showPointNumber ? $"#{fenceIndex}\n{Mathf.RoundToInt(distToPlayer)}m" : $"{Mathf.RoundToInt(distToPlayer)}m";
                markerText.text = label;
            }
            else if (showPointNumber)
            {
                markerText.text = $"#{fenceIndex}";
            }
        }
    }

    /// <summary>
    /// Tự động tạo Icon Marker 3D phát sáng Unlit lơ lửng phía trên điểm rào
    /// </summary>
    public void CreateObjectiveMarker()
    {
        if (markerObj != null) return;

        markerObj = new GameObject($"Marker_Fence_{fenceIndex}");
        markerObj.transform.SetParent(transform, false);
        initialMarkerLocalPos = new Vector3(0, markerHeight, 0);
        markerObj.transform.localPosition = initialMarkerLocalPos;
        markerObj.transform.localScale = Vector3.one * markerScale;

        // 1. Tạo SpriteRenderer cho Icon với Shader Unlit để luôn phát sáng trong đêm
        markerSpriteRenderer = markerObj.AddComponent<SpriteRenderer>();
        
        // Gán Shader Unlit cho Sprite để không bị bóng tối làm đen thui
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (spriteShader == null) spriteShader = Shader.Find("Unlit/Transparent");
        if (spriteShader != null)
        {
            markerSpriteRenderer.material = new Material(spriteShader);
        }

        if (customMarkerSprite != null)
        {
            markerSpriteRenderer.sprite = customMarkerSprite;
        }
        else
        {
            markerSpriteRenderer.sprite = CreateDefaultMarkerSprite();
        }
        markerSpriteRenderer.color = markerColor;
        markerSpriteRenderer.sortingOrder = 50; // Luôn ưu tiên hiển thị trên cùng

        // 2. Tạo Text hiển thị số thứ tự điểm kiểm tra (#1, #2...)
        if (showPointNumber || showDistanceText)
        {
            GameObject textObj = new GameObject("MarkerText");
            textObj.transform.SetParent(markerObj.transform, false);
            textObj.transform.localPosition = new Vector3(0, 0, -0.02f);

            markerText = textObj.AddComponent<TextMeshPro>();
            markerText.alignment = TextAlignmentOptions.Center;
            markerText.fontSize = 4.8f;
            markerText.fontStyle = FontStyles.Bold;
            markerText.color = Color.white;
            markerText.text = $"#{fenceIndex}";
            markerText.outlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            markerText.outlineWidth = 0.3f;

            RectTransform rt = markerText.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(2f, 2f);
        }
    }

    /// <summary>
    /// Tạo Sprite hình thoi phát sáng procedural với độ sắc nét cao
    /// </summary>
    private Sprite CreateDefaultMarkerSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float radius = size / 2.1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float diamondDist = (dx + dy) / radius;

                if (diamondDist <= 1f)
                {
                    // Lớp phát sáng viền và tâm trong suốt nhẹ để chữ nổi bật
                    float alpha = (diamondDist > 0.65f) ? 1f : 0.85f;
                    float brightness = (diamondDist > 0.8f) ? 1f : 0.9f;
                    tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Kích hoạt khi đi vào vùng Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || triggerMode != TriggerMode.WalkIntoZone) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            TriggerPatrolPoint();
        }
    }

    // Kích hoạt khi bấm E
    public override void Interact()
    {
        if (isTriggered || triggerMode != TriggerMode.PressEToInspect) return;

        TriggerPatrolPoint();
    }

    private void TriggerPatrolPoint()
    {
        if (isTriggered) return;
        isTriggered = true;
        promptMessage = ""; // Ẩn gợi ý tương tác

        // Tắt Collider ngay lập tức để Raycast / Trigger không quét trúng nữa
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        // 1. Hiệu ứng Icon biến mất mượt mà (thu nhỏ và mờ dần)
        if (markerObj != null)
        {
            StartCoroutine(FadeOutMarker());
        }

        // 2. Phát âm thanh hù dọa của hàng rào
        PlaySound(fenceSound != null ? fenceSound : interactSound);

        // 3. Kích hoạt bóng ma / mắt đỏ lướt qua rào nếu có
        if (spookyVisualObject != null)
        {
            spookyVisualObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            spookyVisualObject.SetActive(false);
        }

        // 4. Báo về TaskManager để cập nhật tiến độ (1/6, 2/6, ...)
        if (TaskManager.instance != null)
        {
            int reportedIndex = (fenceIndex <= 0) ? 1 : fenceIndex;
            TaskManager.instance.CompleteFencePatrol(reportedIndex);
        }
    }

    private IEnumerator FadeOutMarker()
    {
        if (markerObj == null) yield break;

        float elapsed = 0f;
        float duration = 0.35f;
        Vector3 origScale = markerObj.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            markerObj.transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(markerObj);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (localAudioSource != null)
        {
            localAudioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmos()
    {
        // Vẽ vùng quét Trigger màu vàng trong Scene view
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Gizmos.color = isTriggered ? new Color(0.2f, 1f, 0.2f, 0.2f) : new Color(1f, 0.8f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = isTriggered ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else
        {
            Gizmos.color = isTriggered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        // Vẽ vị trí Icon Marker trên Scene view
        Vector3 markerPos = transform.position + Vector3.up * markerHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, markerPos);
        Gizmos.DrawWireSphere(markerPos, 0.25f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(markerPos + Vector3.up * 0.3f, $"[Fence #{fenceIndex}]");
#endif
    }
}
