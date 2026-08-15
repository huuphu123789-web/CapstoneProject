using UnityEngine;

public class GhostLookAt : MonoBehaviour
{
    public Transform player;
    public float rotateSpeed = 3f;

    void Update()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;

        // Không cúi hoặc ngẩng đầu
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}