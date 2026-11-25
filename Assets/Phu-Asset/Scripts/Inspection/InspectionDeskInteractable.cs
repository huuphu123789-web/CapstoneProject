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

    [Tooltip("Bật (tick) nếu muốn vừa hiện lên 2 ô trên Secure Control Panel vừa mở thêm bảng UI phóng to toàn màn hình")]
    public bool alsoOpenFullscreenUI = false;

    private Material originalCccdMaterial;
    private Material originalSurrenderMaterial;
    private Material runtimeCccdMaterial;
    private Material runtimeSurrenderMaterial;
    private OnOff toggleSwitch;
    private OnOff indicatorLight;

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
        CacheOriginalScreenMaterials();
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
        toggleSwitch = GetComponent<OnOff>();
        if (transform.parent != null)
        {
            OnOff[] siblingOnOffs = transform.parent.GetComponentsInChildren<OnOff>(true);
            for (int i = 0; i < siblingOnOffs.Length; i++)
            {
                if (siblingOnOffs[i] != null && siblingOnOffs[i] != toggleSwitch &&
                    siblingOnOffs[i].name.ToLowerInvariant().Contains("light"))
                {
                    indicatorLight = siblingOnOffs[i];
                    break;
                }
            }
        }
    }

    private void SetSwitchVisualState(bool isOn)
    {
        try
        {
            if (toggleSwitch != null) toggleSwitch.On = isOn;
            if (indicatorLight != null) indicatorLight.On = isOn;
        }
        catch { }
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

    private void CacheOriginalScreenMaterials()
    {
        if (cccdScreenRenderer != null)
        {
            Material[] mats = cccdScreenRenderer.sharedMaterials;
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                originalCccdMaterial = mats[screenMaterialIndex];
            }
        }

        if (surrenderScreenRenderer != null)
        {
            Material[] mats = surrenderScreenRenderer.sharedMaterials;
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                originalSurrenderMaterial = mats[screenMaterialIndex];
            }
        }
    }

    /// <summary>
    /// Hiển thị 2 ảnh CCCD và Surrender Confirm lên 2 ô màn hình của Secure Control Panel
    /// </summary>
    public void DisplayDocumentsOnScreens(Texture cccdTex, Texture surrenderTex)
    {
        AutoFindControlPanelScreens();
        SetSwitchVisualState(true);

        if (cccdScreenRenderer != null)
        {
            ApplyTextureToScreen(cccdScreenRenderer, cccdTex, originalCccdMaterial, ref runtimeCccdMaterial);
        }

        if (surrenderScreenRenderer != null)
        {
            ApplyTextureToScreen(surrenderScreenRenderer, surrenderTex, originalSurrenderMaterial, ref runtimeSurrenderMaterial);
        }
    }

    /// <summary>
    /// Tắt 2 màn hình về trạng thái chờ (OffGrid) khi chưa có NPC hoặc đã duyệt xong
    /// </summary>
    public void ClearScreens()
    {
        SetSwitchVisualState(false);
        RestoreOriginalMaterial(cccdScreenRenderer, originalCccdMaterial);
        RestoreOriginalMaterial(surrenderScreenRenderer, originalSurrenderMaterial);
    }

    private void ApplyTextureToScreen(MeshRenderer targetRenderer, Texture tex, Material originalMat, ref Material runtimeMat)
    {
        if (targetRenderer == null) return;
        Material[] mats = targetRenderer.materials;
        if (screenMaterialIndex < 0 || screenMaterialIndex >= mats.Length) return;

        if (tex == null)
        {
            if (originalMat != null)
            {
                mats[screenMaterialIndex] = originalMat;
                targetRenderer.materials = mats;
            }
            return;
        }

        if (runtimeMat == null)
        {
            Material baseMat = originalMat != null ? originalMat : mats[screenMaterialIndex];
            runtimeMat = new Material(baseMat);
            runtimeMat.name = targetRenderer.name + "_DisplayMat";
        }

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
            runtimeMat.SetColor("_BaseColor", Color.white);
        }
        if (runtimeMat.HasProperty("_Color"))
        {
            runtimeMat.SetColor("_Color", Color.white);
        }

        if (runtimeMat.HasProperty("_EmissionMap") && runtimeMat.HasProperty("_EmissionColor"))
        {
            runtimeMat.EnableKeyword("_EMISSION");
            runtimeMat.SetTexture("_EmissionMap", tex);
            runtimeMat.SetTextureScale("_EmissionMap", screenTextureScale);
            runtimeMat.SetTextureOffset("_EmissionMap", screenTextureOffset);
            runtimeMat.SetColor("_EmissionColor", Color.white * screenEmissionIntensity);
        }

        mats[screenMaterialIndex] = runtimeMat;
        targetRenderer.materials = mats;
    }

    private void RestoreOriginalMaterial(MeshRenderer targetRenderer, Material originalMat)
    {
        if (targetRenderer == null || originalMat == null) return;
        Material[] mats = targetRenderer.sharedMaterials;
        if (screenMaterialIndex < 0 || screenMaterialIndex >= mats.Length) return;

        mats[screenMaterialIndex] = originalMat;
        targetRenderer.sharedMaterials = mats;
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

        if (NPCInspectionManager.instance != null)
        {
            NPCInspectionManager.instance.OpenCurrentNPCDocuments();
        }
    }
}
