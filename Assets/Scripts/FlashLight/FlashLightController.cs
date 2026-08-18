using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [SerializeField] private Light flashlight;

    void Start()
    {
        // Tự tìm Spotlight nếu chưa gán
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        // Nhấn F để bật/tắt đèn pin
        if (Input.GetKeyDown(KeyCode.F))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}