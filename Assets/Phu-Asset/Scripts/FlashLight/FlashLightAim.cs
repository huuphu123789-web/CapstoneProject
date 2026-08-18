using UnityEngine;

/// <summary>
/// Xoay đèn pin (gắn ở tay) theo hướng camera nhìn.
/// Gắn vào GameObject chứa model đèn pin.
/// </summary>
public class FlashlightAim : MonoBehaviour
{
    [Tooltip("Tốc độ xoay đèn pin theo camera (cao = nhanh, thấp = mượt)")]
    [SerializeField] private float aimSpeed = 15f;

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Xoay đèn pin hướng theo camera (mượt)
        Quaternion targetRotation = cam.rotation;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            aimSpeed * Time.deltaTime
        );
    }
}