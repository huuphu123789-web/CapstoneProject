using UnityEngine;

/// <summary>
/// Script tạo hiệu ứng đung đưa theo gió cho cây cối (Tree Wind Sway).
/// - Hoạt động trực tiếp trên bất kỳ Prefab / GameObject cây nào trong Scene.
/// - Tự động tính độ lệch pha (Phase Offset) theo vị trí thế giới để gió thổi thành từng làn sóng qua rừng cây.
/// - Kết hợp hàm Sin kép + Perlin Noise tạo cảm giác gió giật tự nhiên, lúc mạnh lúc nhẹ.
/// - Tự động tối ưu hiệu năng: Tạm dừng khi người chơi đứng quá xa (Culling Distance).
/// </summary>
public class TreeWindSway : MonoBehaviour
{
    [Header("=== CẤU HÌNH GIÓ (WIND SETTINGS) ===")]
    [Tooltip("Tốc độ gió thổi (càng cao cây đung đưa càng nhanh)")]
    [Range(0.2f, 5.0f)]
    public float windSpeed = 1.6f;

    [Tooltip("Góc nghiêng tối đa của thân cây (độ) - Khuyên dùng 1.5 - 3.5 độ để nhìn chân thực")]
    [Range(0.5f, 10.0f)]
    public float maxSwayAngle = 2.2f;

    [Tooltip("Độ hỗn loạn / gió giật ngẫu nhiên (Perlin Noise)")]
    [Range(0f, 1f)]
    public float turbulence = 0.4f;

    [Header("=== HƯỚNG GIÓ (WIND DIRECTION) ===")]
    [Tooltip("Hướng gió chính thổi trên mặt đất (X, Z)")]
    public Vector2 windDirection = new Vector2(1f, 0.5f);

    [Header("=== TỐI ƯU HIỆU NĂNG (PERFORMANCE) ===")]
    [Tooltip("Khoảng cách tối đa so với Camera để đung đưa cây (mét)")]
    public float maxDistanceToCamera = 80f;

    private Quaternion initialRotation;
    private float worldPhaseOffset;
    private Vector3 swayAxis;
    private Transform mainCameraTransform;
    private float distanceCheckTimer = 0f;
    private bool isVisibleOrClose = true;

    void Start()
    {
        // Lưu lại góc xoay ban đầu của cây
        initialRotation = transform.localRotation;

        // Chuẩn hóa hướng gió
        Vector2 normWind = windDirection.normalized;
        if (normWind == Vector2.zero) normWind = Vector2.right;

        // Trục quay vuông góc với hướng gió trên mặt phẳng nằm ngang
        swayAxis = new Vector3(-normWind.y, 0f, normWind.x);

        // Tạo độ lệch pha dựa trên tọa độ X và Z trong Scene
        // Giúp gió thổi thành làn sóng tự nhiên: cây này lắc xong mới truyền tới cây bên cạnh
        worldPhaseOffset = (transform.position.x * 0.35f) + (transform.position.z * 0.25f);

        // Tìm Camera
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. Kiểm tra khoảng cách định kỳ (mỗi 0.5s) để tối ưu CPU cho cả khu rừng
        distanceCheckTimer -= Time.deltaTime;
        if (distanceCheckTimer <= 0f)
        {
            distanceCheckTimer = 0.5f;
            if (mainCameraTransform == null && Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }

            if (mainCameraTransform != null)
            {
                float sqrDist = (transform.position - mainCameraTransform.position).sqrMagnitude;
                isVisibleOrClose = sqrDist <= (maxDistanceToCamera * maxDistanceToCamera);
            }
        }

        if (!isVisibleOrClose) return;

        // 2. Tính toán nhịp đung đưa bằng sóng hình Sin kép
        float time = Time.time * windSpeed + worldPhaseOffset;
        
        // Sóng chính (nhịp thở chậm của cây)
        float mainWave = Mathf.Sin(time);
        
        // Sóng phụ (nhịp rung nhẹ hơn đan xen)
        float secondaryWave = Mathf.Sin(time * 1.83f + 1.2f) * 0.35f;

        // 3. Hiệu ứng gió giật ngẫu nhiên bằng Perlin Noise (lúc gió mạnh, lúc lặng gió)
        float gustNoise = Mathf.PerlinNoise(transform.position.x * 0.05f + Time.time * 0.2f, transform.position.z * 0.05f);
        float currentTurbulence = Mathf.Lerp(1.0f - turbulence, 1.0f + turbulence, gustNoise);

        // 4. Tổng hợp góc nghiêng
        float swayAngle = (mainWave + secondaryWave) * maxSwayAngle * currentTurbulence;

        // Thêm một chút dao động lệch trục phụ để cây nghiêng tự nhiên 3D
        float sideSwayAngle = Mathf.Cos(time * 0.75f) * (maxSwayAngle * 0.25f) * currentTurbulence;
        Vector3 sideAxis = new Vector3(windDirection.x, 0f, windDirection.y).normalized;

        // 5. Cập nhật góc xoay mượt mà cho cây
        Quaternion swayRot = Quaternion.AngleAxis(swayAngle, swayAxis) * Quaternion.AngleAxis(sideSwayAngle, sideAxis);
        transform.localRotation = initialRotation * swayRot;
    }

    void OnDisable()
    {
        // Trả lại góc ban đầu khi tắt
        transform.localRotation = initialRotation;
    }
}
