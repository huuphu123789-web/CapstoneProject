using UnityEngine;

public class GhostWalk : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    public float speed = 5f;

    private bool walking = false;

    void Update()
    {
        if (!walking)
            return;

        // Di chuyển từ điểm bắt đầu tới điểm kết thúc
        transform.position = Vector3.MoveTowards(
            transform.position,
            endPoint.position,
            speed * Time.deltaTime
        );

        // Xoay theo hướng di chuyển
        Vector3 direction = endPoint.position - transform.position;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Đã tới cuối
        if (Vector3.Distance(transform.position, endPoint.position) < 0.1f)
        {
            gameObject.SetActive(false);
        }
    }

    public void StartWalking()
    {
        transform.position = startPoint.position;
        walking = true;
    }
}