using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Cấu hình chuột")]
    public float mouseSensitivity =100f; //*Độ nhạy của chuột
    
    [Header("Cơ thể nhân vật")]
    [Tooltip("Kéo đối tượng player (cha) vào đây")]
    public Transform playerBody;

    private float xRotation = 0f; //* Lưu góc quay dọc hiện tại

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //*Khoá con trỏ chuột vào giữa màn hình và ẩn nó đi
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //*Lấy dữ liệu di chuyển chuột của người chơi
        float mouseX = Input.GetAxis("Mouse X") *mouseSensitivity *Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") *mouseSensitivity *Time.deltaTime;

        //*Tính toán gọc quay dọc (ngước lên / nhìn xuống)
        xRotation -= mouseY;
        //*Giới hạn góc nhìn lên xuống trong khoảng -90 - 90 (tránh bị lộn ngược đầu)
        xRotation= Mathf.Clamp(xRotation,-90,90);
        //*Áp dụng xoay dọc cho camera (Chính đội tượng gắn script này)
        transform.localRotation = Quaternion.Euler(xRotation,0f,0f);
        //*Áp dụng xoay ngang cho cơ thẻ player (xoay cả người theo chuột trái/phải)
        playerBody.Rotate(Vector3.up * mouseX);
        
    }
}
