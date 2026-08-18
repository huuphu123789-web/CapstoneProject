using UnityEngine;

public class PlayerBodyRotator : MonoBehaviour
{
    void LateUpdate()
    {
         Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Lấy góc Y (trái/phải) của Main Camera
        float cameraYaw = Camera.main.transform.eulerAngles.y;
        
        // Xoay player body theo hướng camera
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
    }
}