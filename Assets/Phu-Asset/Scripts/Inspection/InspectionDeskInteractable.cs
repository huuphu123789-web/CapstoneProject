using UnityEngine;

/// <summary>
/// Vật thể bàn/bảng điều khiển kiểm tra giấy tờ (Secure Control Panel / Guard Booth Desk).
/// Khi có NPC đang chờ kiểm tra, người chơi nhìn vào và bấm [E] để hiển thị
/// ảnh CCCD và Surrender Confirm lên 2 màn hình trên Secure Control Panel (và/hoặc mở UI phóng to).
/// </summary>
public class InspectionDeskInteractable : Interactable
{
    public static InspectionDeskInteractable instance;

    [Header("=== HIỂN THỊ TẬP HỒ SƠ (TÙY CHỌN) ===")]
    [Tooltip("Mô hình tập giấy tờ trên bàn (ẩn khi không có NPC nào cần kiểm tra)")]
    public GameObject documentVisualProp;

    [Header("=== 2 MÀN HÌNH TRÊN SECURE CONTROL PANEL ===")]
    [Tooltip("Ô màn hình hiển thị CCCD (tự động tìm object tên 'CCCD' nếu để trống)")]
    public MeshRenderer cccdScreenRenderer;

    [Tooltip("Ô màn hình hiển thị Surrender Confirm (tự động tìm 'SurrentderConfirm' / 'SurrenderConfirm' nếu để trống)")]
    public MeshRenderer surrenderScreenRenderer;

    [Tooltip("Vị trí Material mặt màn hình trên Mesh (mặc định 0 = OffGrid, 1 = viền đen JustBlack)")]
    public int screenMaterialIndex = 0;

    [Tooltip("Độ sáng phát quang của màn hình để đọc rõ giấy tờ vào ban đêm")]
    [Range(0f, 2f)] public float screenEmissionIntensity = 0.85f;

    [Tooltip("Chỉnh Scale/Offset nếu cần lật chiều ảnh trên mặt màn hình (ví dụ Y = -1 nếu bị ngược)")]
    public Vector2 screenTextureScale = Vector2.one;
    public Vector2 screenTextureOffset = Vector2.zero;

    [Tooltip("Góc xoay Z (độ) của mặt ảnh trên màn hình nếu cần xoay ngang/dọc (0, 90, 180, -90)")]
    public float screenRotationZ = 0f;

    [Tooltip("Bật (tick) nếu muốn vừa hiện lên 2 ô trên Secure Control Panel vừa mở thêm bảng UI phóng to toàn màn hình")]
    public bool alsoOpenFullscreenUI = false;

    [Header("=== CÔNG TẮC ON / OFF (TỰ ĐỘNG TÌM NẾU ĐỂ TRỐNG) ===")]
    [Tooltip("GameObject trạng thái BẬT (On) của công tắc Inspect")]
    public GameObject switchOnObject;

    [Tooltip("GameObject trạng thái TẮT (Off) của công tắc Inspect")]
    public GameObject switchOffObject;

    [Tooltip("GameObject đèn báo trạng thái BẬT (On) cạnh công tắc Inspect")]
    public GameObject lightOnObject;

    [Tooltip("GameObject đèn báo trạng thái TẮT (Off) cạnh công tắc Inspect")]
    public GameObject lightOffObject;

    private GameObject cccdDisplayQuad;
    private GameObject surrenderDisplayQuad;
    private Material runtimeCccdMaterial;
    private Material runtimeSurrenderMaterial;
    private bool isScreenShowing = false;

    public bool HasControlPanelScreens => (cccdScreenRenderer != null || surrenderScreenRenderer != null);

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        promptMessage = "Inspect Documents";
        EnsureInteractCollider();
        AutoFindToggleVisuals();
        AutoFindControlPanelScreens();
    }

    private void EnsureInteractCollider()
    {
        if (GetComponent<Collider>() != null) return;

        // Nếu gắn trên ToggleOnOff (có con Off/On nhưng chưa gắn BoxCollider), tự thêm BoxCollider chuẩn cỡ công tắc
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(0.03f, 0.03f, 0.03f);
        box.center = new Vector3(0f, 0.012f, 0f);
    }

    private void AutoFindToggleVisuals()
    {
        if (switchOnObject == null)
        {
            Transform onTf = transform.Find("On");
            if (onTf != null) switchOnObject = onTf.gameObject;
        }
        if (switchOffObject == null)
        {
            Transform offTf = transform.Find("Off");
            if (offTf != null) switchOffObject = offTf.gameObject;
        }

        if ((lightOnObject == null || lightOffObject == null) && transform.parent != null)
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                Transform sibling = transform.parent.GetChild(i);
                if (sibling == transform) continue;
                if (!sibling.name.ToLowerInvariant().Contains("light")) continue;

                Transform lOn = sibling.Find("On");
                Transform lOff = sibling.Find("Off");
                if (lightOnObject == null && lOn != null) lightOnObject = lOn.gameObject;
                if (lightOffObject == null && lOff != null) lightOffObject = lOff.gameObject;
                break;
            }
        }
    }

    private void SetSwitchVisualState(bool isOn)
    {
        isScreenShowing = isOn;
        if (switchOnObject != null) switchOnObject.SetActive(isOn);
        if (switchOffObject != null) switchOffObject.SetActive(!isOn);
        if (lightOnObject != null) lightOnObject.SetActive(isOn);
        if (lightOffObject != null) lightOffObject.SetActive(!isOn);
    }

    void Start()
    {
        SetDocumentsAvailable(false);
    }

    void OnDestroy()
    {
        if (runtimeCccdMaterial != null) Destroy(runtimeCccdMaterial);
        if (runtimeSurrenderMaterial != null) Destroy(runtimeSurrenderMaterial);
    }

    private void AutoFindControlPanelScreens()
    {
        if (cccdScreenRenderer != null && surrenderScreenRenderer != null) return;

        // 1. Ưu tiên tìm trong cùng gốc Secure Control Panel (khi gắn trên nút ToggleOnOff (2) hoặc root)
        MeshRenderer[] rootRenderers = transform.root.GetComponentsInChildren<MeshRenderer>(true);
        TryAssignScreensFrom(rootRenderers);

        // 2. Nếu vẫn chưa thấy, tìm toàn Scene
        if (cccdScreenRenderer == null || surrenderScreenRenderer == null)
        {
            MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            TryAssignScreensFrom(allRenderers);
        }
    }

    private void TryAssignScreensFrom(MeshRenderer[] renderers)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];
            if (mr == null) continue;
            string n = mr.name.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

            if (cccdScreenRenderer == null && n == "cccd")
            {
                cccdScreenRenderer = mr;
            }
            else if (surrenderScreenRenderer == null && (n == "surrentderconfirm" || n == "surrenderconfirm"))
            {
                surrenderScreenRenderer = mr;
            }
        }
    }

    /// <summary>
    /// Hiển thị 2 ảnh CCCD và Surrender Confirm lên 2 ô màn hình của Secure Control Panel
    /// bằng mặt Quad phẳng nằm gọn trong lòng màn hình (UV 0..1 chuẩn, không bị méo theo mesh vỏ).
    /// </summary>
    public void DisplayDocumentsOnScreens(Texture cccdTex, Texture surrenderTex)
    {
        AutoFindControlPanelScreens();
        SetSwitchVisualState(true);

        if (cccdScreenRenderer != null)
        {
            ShowTextureOnOverlayQuad(cccdScreenRenderer, "CCCD_DisplayQuad", cccdTex, ref cccdDisplayQuad, ref runtimeCccdMaterial);
        }

        if (surrenderScreenRenderer != null)
        {
            ShowTextureOnOverlayQuad(surrenderScreenRenderer, "Surrender_DisplayQuad", surrenderTex, ref surrenderDisplayQuad, ref runtimeSurrenderMaterial);
        }
    }

    /// <summary>
    /// Tắt 2 màn hình về trạng thái chờ khi chưa có NPC hoặc đã duyệt xong
    /// </summary>
    public void ClearScreens()
    {
        SetSwitchVisualState(false);
        if (cccdDisplayQuad != null) cccdDisplayQuad.SetActive(false);
        if (surrenderDisplayQuad != null) surrenderDisplayQuad.SetActive(false);
    }

    private void ShowTextureOnOverlayQuad(MeshRenderer parentScreen, string quadName, Texture tex, ref GameObject quadObj, ref Material runtimeMat)
    {
        if (parentScreen == null) return;

        if (tex == null)
        {
            if (quadObj != null) quadObj.SetActive(false);
            Debug.LogWarning($"[InspectionDesk] Thiếu ảnh Texture cho màn hình '{parentScreen.name}'! Hãy kiểm tra ô Cccd Texture / Surrender Confirm Texture trong NPCInspectionManager.");
            return;
        }

        if (quadObj == null)
        {
            quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObj.name = quadName;
            Collider col = quadObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            quadObj.transform.SetParent(parentScreen.transform, false);

            // Tự tính kích thước khớp lòng màn hình của MovingScope.mesh (hoặc Mesh bất kỳ)
            MeshFilter mf = parentScreen.GetComponent<MeshFilter>();
            Vector3 localPos = new Vector3(0f, 0.0028f, 0f);
            Vector3 localScale = new Vector3(0.104f, 0.084f, 1f);
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                // Đặt mặt ảnh nổi nhẹ ngay trên mặt phẳng màn hình bên trong viền
                float topY = b.min.y + b.size.y * 0.35f;
                localPos = new Vector3(b.center.x, Mathf.Max(0.0028f, topY), b.center.z);
                localScale = new Vector3(b.size.x * 0.84f, b.size.z * 0.82f, 1f);
            }

            quadObj.transform.localPosition = localPos;
            quadObj.transform.localRotation = Quaternion.Euler(90f, screenRotationZ, 0f);
            quadObj.transform.localScale = localScale;
        }
        else
        {
            quadObj.transform.localRotation = Quaternion.Euler(90f, screenRotationZ, 0f);
        }

        if (runtimeMat == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");
            if (unlitShader == null) unlitShader = Shader.Find("Standard");
            runtimeMat = new Material(unlitShader);
            runtimeMat.name = quadName + "_Mat";
            if (runtimeMat.HasProperty("_Cull"))
            {
                runtimeMat.SetFloat("_Cull", 0f); // Double-sided để không bao giờ bị ẩn mặt sau
            }
        }

        float brightness = Mathf.Clamp(0.65f + screenEmissionIntensity * 0.35f, 0.5f, 1.5f);
        Color tint = new Color(brightness, brightness, brightness, 1f);

        if (runtimeMat.HasProperty("_BaseMap"))
        {
            runtimeMat.SetTexture("_BaseMap", tex);
            runtimeMat.SetTextureScale("_BaseMap", screenTextureScale);
            runtimeMat.SetTextureOffset("_BaseMap", screenTextureOffset);
        }
        if (runtimeMat.HasProperty("_MainTex"))
        {
            runtimeMat.SetTexture("_MainTex", tex);
            runtimeMat.SetTextureScale("_MainTex", screenTextureScale);
            runtimeMat.SetTextureOffset("_MainTex", screenTextureOffset);
        }
        if (runtimeMat.HasProperty("_BaseColor"))
        {
            runtimeMat.SetColor("_BaseColor", tint);
        }
        if (runtimeMat.HasProperty("_Color"))
        {
            runtimeMat.SetColor("_Color", tint);
        }

        MeshRenderer quadRenderer = quadObj.GetComponent<MeshRenderer>();
        if (quadRenderer != null)
        {
            quadRenderer.sharedMaterial = runtimeMat;
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
        }

        quadObj.SetActive(true);
    }

    /// <summary>
    /// Bật/Tắt khả năng tương tác và hiển thị tập hồ sơ trên bàn
    /// </summary>
    public void SetDocumentsAvailable(bool available)
    {
        promptMessage = available ? "Inspect Documents" : "";
        if (documentVisualProp != null)
        {
            documentVisualProp.SetActive(available);
        }

        if (!available)
        {
            ClearScreens();
        }
    }

    public override void Interact()
    {
        base.Interact();

        if (HasControlPanelScreens && !alsoOpenFullscreenUI && isScreenShowing)
        {
            ClearScreens();
            return;
        }

        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.OpenCurrentNPCDocuments();
        }
    }
}
