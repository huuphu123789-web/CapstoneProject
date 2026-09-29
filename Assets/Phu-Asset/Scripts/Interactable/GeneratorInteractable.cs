using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Script dùng cho Máy Phát Điện (Generator).
/// Kế thừa Interactable (Phú-Asset) - Nhấn phím E để kiểm tra / khởi động máy phát điện.
/// Hỗ trợ:
/// 1. Âm thanh 3D Spatial Audio: Lại gần nghe to rõ, đi xa tắt ngấm.
/// 2. Tự động bật hệ thống ĐÈN CHIẾU SÁNG HÀNG RÀO (Fence Lights / Floodlights) với hiệu ứng chớp nháy điện giật khi nổ máy.
/// 3. Hiệu ứng rung lắc (GeneratorShake), đèn tín hiệu, icon chỉ đường và cập nhật TaskManager.
/// </summary>
public class GeneratorInteractable : Interactable
{
    [Header("=== CẤU HÌNH NHIỆM VỤ ===")]
    [Tooltip("Số thứ tự máy phát điện (mặc định 1)")]
    public int generatorIndex = 1;

    [Tooltip("Máy có đang nổ sẵn từ đầu game hay không (Bật = nổ sẵn, Tắt = phải lại gần bấm E để khởi động)")]
    public bool isRunningAtStart = false;

    [Header("=== HỆ THỐNG ĐÈN CHIẾU SÁNG HÀNG RÀO ===")]
    [Tooltip("Tự động tìm kiếm các đèn hàng rào (FenceLightController / Light) trong Scene nếu để trống")]
    public bool autoFindFenceLights = true;

    [Tooltip("Danh sách các Controller đèn hàng rào")]
    public FenceLightController[] fenceLightControllers;

    [Tooltip("Danh sách các component Light trực tiếp (Spotlight / Point Light gắn trên rào)")]
    public Light[] directFenceLights;

    [Tooltip("Danh sách các GameObject đèn / cột đèn bật sáng khi có điện")]
    public GameObject[] fenceLightGameObjects;

    [Tooltip("Hiệu ứng đèn chớp nháy khởi động khi máy phát điện nổ máy")]
    public bool enablePowerFlicker = true;

    [Tooltip("Độ trễ trước khi đèn bật sáng sau khi máy nổ (giây)")]
    public float powerOnDelay = 0.5f;

    [Header("=== TRẠNG THÁI & HIỆU ỨNG MÁY PHÁT ĐIỆN ===")]
    [Tooltip("Script rung lắc máy phát điện (tự động tìm nếu để trống)")]
    public GeneratorShake generatorShake;

    [Tooltip("GameObject đèn bật (Lamp_On)")]
    public GameObject lampOnObject;

    [Tooltip("GameObject đèn tắt (Lamp_Off)")]
    public GameObject lampOffObject;

    [Tooltip("Nguồn sáng Point Light của máy phát điện (tùy chọn)")]
    public Light indicatorLight;

    [Header("=== ÂM THANH 3D (SPATIAL AUDIO) ===")]
    [Tooltip("Tiếng giật nổ máy / kiểm tra máy phát điện")]
    public AudioClip startSound;

    [Tooltip("Tiếng động cơ máy nổ chạy liên tục rù rù (Loop)")]
    public AudioClip runningLoopSound;

    public AudioSource localAudioSource;

    [Tooltip("Khoảng cách gần nhất giữ âm lượng to nhất 100% (mét)")]
    public float soundMinDistance = 2.5f;

    [Tooltip("Khoảng cách xa nhất nghe thấy tiếng máy nổ, đi xa hơn sẽ hoàn toàn im lặng (mét)")]
    public float soundMaxDistance = 22.0f;

    [Tooltip("Âm lượng động cơ (0 -> 1)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.85f;

    [Header("=== ICON ĐÁNH DẤU (OBJECTIVE MARKER) ===")]
    [Tooltip("Bật/tắt icon lơ lửng phát sáng chỉ đường")]
    public bool showMarker = true;

    [Tooltip("Màu sắc Icon phát sáng trong đêm")]
    public Color markerColor = new Color(0.2f, 0.8f, 1f, 1f); // Màu xanh dương cyan sáng

    [Tooltip("Chiều cao Icon so với máy")]
    public float markerHeight = 1.8f;

    [Tooltip("Kích thước Icon")]
    public float markerScale = 0.55f;

    [Header("=== HIỆU ỨNG HÙ DỌA ===")]
    [Tooltip("Tùy chọn: GameObject hù dọa xuất hiện thoáng qua")]
    public GameObject spookyVisualObject;

    private bool isChecked = false;
    private GameObject markerObj;
    private TextMeshPro markerText;
    private SpriteRenderer markerSpriteRenderer;
    private Transform mainCameraTransform;
    private Vector3 initialMarkerLocalPos;

    void Awake()
    {
        Setup3DAudioSource();

        if (generatorShake == null)
        {
            generatorShake = GetComponentInChildren<GeneratorShake>();
        }

        // Tự động tìm Lamp_On / Lamp_Off nếu chưa gán
        if (lampOnObject == null)
        {
            Transform onT = transform.Find("Lamp_On") ?? transform.Find("Generator/Lamp_On");
            if (onT != null) lampOnObject = onT.gameObject;
        }

        if (lampOffObject == null)
        {
            Transform offT = transform.Find("Lamp_Off") ?? transform.Find("Generator/Lamp_Off");
            if (offT != null) lampOffObject = offT.gameObject;
        }
    }

    void Start()
    {
        promptMessage = isRunningAtStart ? "" : "Inspect Generator";

        // Tự động tìm kiếm các đèn hàng rào nếu danh sách đang trống
        if (autoFindFenceLights)
        {
            if (fenceLightControllers == null || fenceLightControllers.Length == 0)
            {
                fenceLightControllers = FindObjectsOfType<FenceLightController>(true);
            }
        }

        // Khởi tạo trạng thái ban đầu
        if (isRunningAtStart)
        {
            isChecked = true;
            if (lampOnObject != null) lampOnObject.SetActive(true);
            if (lampOffObject != null) lampOffObject.SetActive(false);
            if (indicatorLight != null) indicatorLight.enabled = true;
            if (generatorShake != null) generatorShake.isRunning = true;
            StartRunningEngineSound();
            TurnOnAllFenceLights(false);
        }
        else
        {
            if (lampOnObject != null) lampOnObject.SetActive(false);
            if (lampOffObject != null) lampOffObject.SetActive(true);
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (generatorShake != null) generatorShake.isRunning = false;
            TurnOffAllFenceLights();
        }

        if (spookyVisualObject != null) spookyVisualObject.SetActive(false);

        FindPlayerCamera();

        if (showMarker && !isRunningAtStart)
        {
            CreateObjectiveMarker();
        }
    }

    /// <summary>
    /// Cài đặt AudioSource theo chuẩn 3D Spatial Sound (âm thanh vòm theo khoảng cách)
    /// </summary>
    private void Setup3DAudioSource()
    {
        if (localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        localAudioSource.spatialBlend = 1.0f; // 100% 3D Sound (nghe theo không gian 3D, trái/phải, gần/xa)
        localAudioSource.rolloffMode = AudioRolloffMode.Linear; // Giảm âm lượng tuyến tính mượt mà theo khoảng cách
        localAudioSource.minDistance = soundMinDistance; // Gần máy (<= 2.5m) nghe to và rõ nhất
        localAudioSource.maxDistance = soundMaxDistance; // Đi xa (> 22m) âm thanh tắt ngấm hoàn toàn
        localAudioSource.volume = soundVolume;
        localAudioSource.dopplerLevel = 0f; // Tắt méo tiếng khi chạy
        localAudioSource.playOnAwake = false;
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
        if (isChecked || !showMarker) return;

        if (markerObj == null)
        {
            CreateObjectiveMarker();
        }

        if (markerObj == null) return;

        if (mainCameraTransform == null)
        {
            FindPlayerCamera();
            if (mainCameraTransform == null) return;
        }

        // 1. Billboard: Luôn hướng về phía Camera người chơi
        Vector3 dirToCam = markerObj.transform.position - mainCameraTransform.position;
        if (dirToCam.sqrMagnitude > 0.001f)
        {
            markerObj.transform.rotation = Quaternion.LookRotation(dirToCam);
        }

        // 2. Hiệu ứng nhấp nhô lơ lửng (Bobbing)
        float bobOffset = Mathf.Sin(Time.time * 2.5f) * 0.08f;
        markerObj.transform.localPosition = initialMarkerLocalPos + new Vector3(0, bobOffset, 0);

        // 3. Hiệu ứng nhấp nháy phát sáng (Pulsing Glow)
        float pulseAlpha = Mathf.PingPong(Time.time * 1.5f, 0.35f) + 0.65f;
        if (markerSpriteRenderer != null)
        {
            Color c = markerColor;
            c.a = markerColor.a * pulseAlpha;
            markerSpriteRenderer.color = c;
        }
    }

    public override void Interact()
    {
        if (isChecked) return;

        isChecked = true;
        promptMessage = ""; // Ẩn câu nhắc tương tác

        StartCoroutine(InspectGeneratorRoutine());
    }

    private IEnumerator InspectGeneratorRoutine()
    {
        // 1. Ẩn Icon Marker
        if (markerObj != null)
        {
            StartCoroutine(FadeOutMarker());
        }

        // 2. Phát âm thanh khởi động / kiểm tra (3D)
        AudioClip soundToPlay = (startSound != null) ? startSound : interactSound;
        if (soundToPlay != null && localAudioSource != null)
        {
            localAudioSource.PlayOneShot(soundToPlay);
        }

        // 3. Bật rung máy nếu có
        if (generatorShake != null)
        {
            generatorShake.isRunning = true;
        }

        // 4. Đổi trạng thái đèn trên máy phát điện
        if (lampOffObject != null) lampOffObject.SetActive(false);
        if (lampOnObject != null) lampOnObject.SetActive(true);
        if (indicatorLight != null) indicatorLight.enabled = true;

        // 5. Chuyển sang âm thanh động cơ nổ liên tục (Loop 3D)
        StartRunningEngineSound();

        // 6. BẬT HỆ THỐNG ĐÈN CHIẾU SÁNG HÀNG RÀO (có độ trễ & chớp nháy chân thực)
        if (powerOnDelay > 0f)
        {
            yield return new WaitForSeconds(powerOnDelay);
        }
        TurnOnAllFenceLights(enablePowerFlicker);

        // 7. Hiệu ứng ma quái nếu có
        if (spookyVisualObject != null)
        {
            spookyVisualObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            spookyVisualObject.SetActive(false);
        }

        // 8. Báo về TaskManager
        if (TaskManager.instance != null)
        {
            TaskManager.instance.CompleteGenerator(generatorIndex);
        }

        Debug.Log("[Generator] Đã khởi động máy phát điện và bật sáng toàn bộ hệ thống đèn hàng rào!");
    }

    /// <summary>
    /// Bật toàn bộ đèn hàng rào kết nối với máy phát điện
    /// </summary>
    public void TurnOnAllFenceLights(bool withFlicker = true)
    {
        // 1. Bật FenceLightController
        if (fenceLightControllers != null)
        {
            foreach (var ctrl in fenceLightControllers)
            {
                if (ctrl != null) ctrl.TurnOn(withFlicker);
            }
        }

        // 2. Bật các Light trực tiếp
        if (directFenceLights != null)
        {
            foreach (var l in directFenceLights)
            {
                if (l != null)
                {
                    if (withFlicker && gameObject.activeInHierarchy)
                    {
                        StartCoroutine(FlickerDirectLight(l));
                    }
                    else
                    {
                        l.enabled = true;
                    }
                }
            }
        }

        // 3. Bật các GameObject đèn
        if (fenceLightGameObjects != null)
        {
            foreach (var go in fenceLightGameObjects)
            {
                if (go != null) go.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Tắt toàn bộ đèn hàng rào
    /// </summary>
    public void TurnOffAllFenceLights()
    {
        if (fenceLightControllers != null)
        {
            foreach (var ctrl in fenceLightControllers)
            {
                if (ctrl != null) ctrl.TurnOff();
            }
        }

        if (directFenceLights != null)
        {
            foreach (var l in directFenceLights)
            {
                if (l != null) l.enabled = false;
            }
        }

        if (fenceLightGameObjects != null)
        {
            foreach (var go in fenceLightGameObjects)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }

    private IEnumerator FlickerDirectLight(Light l)
    {
        if (l == null) yield break;

        float origIntensity = l.intensity;
        int flickers = 3;

        for (int i = 0; i < flickers; i++)
        {
            l.enabled = true;
            l.intensity = origIntensity * Random.Range(0.3f, 0.7f);
            yield return new WaitForSeconds(Random.Range(0.06f, 0.12f));

            l.enabled = false;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
        }

        l.enabled = true;
        l.intensity = origIntensity;
    }

    private void StartRunningEngineSound()
    {
        if (runningLoopSound != null && localAudioSource != null)
        {
            localAudioSource.clip = runningLoopSound;
            localAudioSource.loop = true;
            localAudioSource.spatialBlend = 1.0f; // 3D Audio
            localAudioSource.rolloffMode = AudioRolloffMode.Linear;
            localAudioSource.minDistance = soundMinDistance;
            localAudioSource.maxDistance = soundMaxDistance;
            localAudioSource.volume = soundVolume;
            localAudioSource.Play();
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

    public void CreateObjectiveMarker()
    {
        if (markerObj != null) return;

        markerObj = new GameObject($"Marker_Generator_{generatorIndex}");
        markerObj.transform.SetParent(transform, false);
        initialMarkerLocalPos = new Vector3(0, markerHeight, 0);
        markerObj.transform.localPosition = initialMarkerLocalPos;
        markerObj.transform.localScale = Vector3.one * markerScale;

        markerSpriteRenderer = markerObj.AddComponent<SpriteRenderer>();

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (spriteShader == null) spriteShader = Shader.Find("Unlit/Transparent");
        if (spriteShader != null)
        {
            markerSpriteRenderer.material = new Material(spriteShader);
        }

        markerSpriteRenderer.sprite = CreateDefaultMarkerSprite();
        markerSpriteRenderer.color = markerColor;
        markerSpriteRenderer.sortingOrder = 50;

        // Tạo Text "GEN" hoặc số
        GameObject textObj = new GameObject("MarkerText");
        textObj.transform.SetParent(markerObj.transform, false);
        textObj.transform.localPosition = new Vector3(0, 0, -0.02f);

        markerText = textObj.AddComponent<TextMeshPro>();
        markerText.alignment = TextAlignmentOptions.Center;
        markerText.fontSize = 4.2f;
        markerText.fontStyle = FontStyles.Bold;
        markerText.color = Color.white;
        markerText.text = "GEN";
        markerText.outlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        markerText.outlineWidth = 0.3f;

        RectTransform rt = markerText.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(2f, 2f);
    }

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

    private void OnDrawGizmos()
    {
        // 1. Vẽ phạm vi âm thanh 3D trong Scene view
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, soundMinDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, soundMaxDistance);

        // 2. Vẽ Marker
        Vector3 markerPos = transform.position + Vector3.up * markerHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, markerPos);
        Gizmos.DrawWireSphere(markerPos, 0.25f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(markerPos + Vector3.up * 0.3f, "[Generator]");
        UnityEditor.Handles.Label(transform.position + Vector3.right * soundMaxDistance, $"Audio Max: {soundMaxDistance}m");
#endif
    }
}
