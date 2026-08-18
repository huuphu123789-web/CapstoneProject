using UnityEngine;

public class PlayerBodyRotator : MonoBehaviour
{
    void Start()
    {
        // Khóa chuột 1 lần duy nhất khi bắt đầu game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Dừng hoàn toàn khi game đang Pause
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
            return;

        // Lấy góc Y (trái/phải) của Main Camera
        float cameraYaw = Camera.main.transform.eulerAngles.y;
        
        // Xoay player body theo hướng camera
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
    }
}