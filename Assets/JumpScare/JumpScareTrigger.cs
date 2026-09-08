using System.Collections;
using UnityEngine;

public class JumpScareTrigger : MonoBehaviour
{
    [Header("Elements")]
    public GameObject jumpscareObject;
    public Transform monsterFace;       // Vị trí HeadTarget trên mặt quái
    public Transform playerCamera;      // Main Camera của Player
    public SimplePlayerController playerController;
    public AudioSource audioSource;
    public CameraShake cameraShake;

    [Header("Settings")]
    public float displayDuration = 1.5f;
    public float rotateSpeed = 15f;
    public bool destroyAfterTrigger = true;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(ShowJumpscare());
        }
    }

    private IEnumerator ShowJumpscare()
    {
        // 1. Khóa di chuyển của Player
        if (playerController != null) playerController.canMove = false;

        // 2. Lưu lại Parent gốc và Vị trí tương đối (Local Position) ban đầu của Camera
        Transform originalParent = null;
        Vector3 originalLocalPos = Vector3.zero;

        if (playerCamera != null)
        {
            originalParent = playerCamera.parent;
            originalLocalPos = playerCamera.localPosition; // Lưu vị trí mắt Player
            playerCamera.SetParent(null, true); // Giữ nguyên vị trí thế giới khi gỡ parent
        }

        // 3. Bật quái, phát tiếng và rung camera
        if (jumpscareObject != null) jumpscareObject.SetActive(true);
        if (audioSource != null) audioSource.Play();
        if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);

        // 4. Xoay Camera mượt nhìn trực diện vào mặt quái
        float elapsedTime = 0f;
        while (elapsedTime < displayDuration)
        {
            if (playerCamera != null && monsterFace != null)
            {
                Vector3 targetDirection = monsterFace.position - playerCamera.position;

                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    playerCamera.rotation = Quaternion.Slerp(
                        playerCamera.rotation,
                        targetRotation,
                        Time.deltaTime * rotateSpeed
                    );
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 5. Kết thúc Jumpscare: Trả Camera và đồng bộ lại góc nhìn cho Player
        if (playerCamera != null && originalParent != null)
        {
            // Xoay thân Player theo hướng Camera vừa nhìn
            Vector3 lookDir = playerCamera.forward;
            lookDir.y = 0; // Giữ thân Player đứng thẳng trên mặt sàn
            if (lookDir != Vector3.zero && playerController != null)
            {
                playerController.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Gắn lại Camera vào Player và reset về đúng vị trí mắt gốc
            playerCamera.SetParent(originalParent);
            playerCamera.localPosition = originalLocalPos;
        }

        // 6. Ẩn quái & trả lại quyền di chuyển cho Player
        if (jumpscareObject != null) jumpscareObject.SetActive(false);
        if (playerController != null) playerController.canMove = true;

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }
}