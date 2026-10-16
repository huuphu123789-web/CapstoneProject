using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Universal Camera Shake for both Unity Standard Camera and Cinemachine 3.x:
/// - Automatically discovers CinemachineCamera and CinemachineFollow on the Player.
/// - Dynamically applies offset shake via CinemachineCameraOffset so Cinemachine
///   does not overwrite the shake motion.
/// - Shakes both position and subtle pitch/yaw angle for heavy impact feel.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    [Header("=== CINEMACHINE INTEGRATION ===")]
    [Tooltip("Target CinemachineCamera (auto-detected from Player if empty)")]
    public CinemachineCamera virtualCamera;
    public CinemachineCameraOffset cameraOffset;
    public CinemachineFollow followComponent;

    [Header("=== SHAKE MULTIPLIERS ===")]
    [Range(0.1f, 3.0f)]
    public float shakeIntensityMultiplier = 1.0f;
    public float rotationalShakeMultiplier = 4.0f;

    private Coroutine shakeCoroutine;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Vector3 originalFollowOffset;
    private bool hasOriginalFollowOffset = false;

    void Awake()
    {
        if (instance == null) instance = this;
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        FindCinemachineComponents();
    }

    void OnEnable()
    {
        if (virtualCamera == null)
        {
            FindCinemachineComponents();
        }
    }

    public void FindCinemachineComponents()
    {
        // 1. Check on current GameObject
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineCamera>();
            if (virtualCamera == null) virtualCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        // 2. Search on Player
        if (virtualCamera == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                virtualCamera = player.GetComponentInChildren<CinemachineCamera>(true);
            }
        }

        // 3. Search anywhere in the scene
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        // Setup CinemachineCameraOffset on the virtual camera for shake
        if (virtualCamera != null)
        {
            cameraOffset = virtualCamera.GetComponent<CinemachineCameraOffset>();
            if (cameraOffset == null)
            {
                cameraOffset = virtualCamera.gameObject.AddComponent<CinemachineCameraOffset>();
            }

            followComponent = virtualCamera.GetComponent<CinemachineFollow>();
            if (followComponent != null && !hasOriginalFollowOffset)
            {
                originalFollowOffset = followComponent.FollowOffset;
                hasOriginalFollowOffset = true;
            }
        }
    }

    /// <summary>
    /// Shake the camera for the specified duration and magnitude.
    /// Works with Cinemachine and standard Camera automatically.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (virtualCamera == null || cameraOffset == null)
        {
            FindCinemachineComponents();
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(DoShake(duration, magnitude * shakeIntensityMultiplier));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        Vector3 basePos = transform.localPosition;
        Quaternion baseRot = transform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1.0f - Mathf.Clamp01(elapsed / duration);

            // Calculate random shake offsets
            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;
            float z = Random.Range(-0.5f, 0.5f) * (magnitude * 0.5f) * damper;
            Vector3 shakeVector = new Vector3(x, y, z);

            // Rotational jitter
            float pitch = Random.Range(-1f, 1f) * magnitude * rotationalShakeMultiplier * damper;
            float yaw = Random.Range(-1f, 1f) * magnitude * rotationalShakeMultiplier * damper;
            float roll = Random.Range(-0.5f, 0.5f) * magnitude * rotationalShakeMultiplier * damper;

            // 1. Apply to Cinemachine via CinemachineCameraOffset
            if (cameraOffset != null)
            {
                cameraOffset.Offset = shakeVector;
            }

            // 2. Apply to CinemachineFollow if present
            if (followComponent != null && hasOriginalFollowOffset)
            {
                followComponent.FollowOffset = originalFollowOffset + shakeVector;
            }

            // 3. Apply to standard Transform (fallback / local camera shake)
            transform.localPosition = basePos + shakeVector;
            transform.localRotation = baseRot * Quaternion.Euler(pitch, yaw, roll);

            yield return null;
        }

        // Restore everything to default after shake finishes
        if (cameraOffset != null)
        {
            cameraOffset.Offset = Vector3.zero;
        }

        if (followComponent != null && hasOriginalFollowOffset)
        {
            followComponent.FollowOffset = originalFollowOffset;
        }

        transform.localPosition = basePos;
        transform.localRotation = baseRot;
        shakeCoroutine = null;
    }

    void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (cameraOffset != null)
        {
            cameraOffset.Offset = Vector3.zero;
        }

        if (followComponent != null && hasOriginalFollowOffset)
        {
            followComponent.FollowOffset = originalFollowOffset;
        }
    }
}
