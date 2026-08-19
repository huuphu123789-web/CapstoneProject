using UnityEngine;

public class GhostRush : MonoBehaviour
{
    public Transform player;

    public float rushSpeed = 12f;
    public float disappearDistance = 1.5f;

    public AudioSource rushSound;

    private bool rushing = false;

    void Update()
    {
        if (!rushing)
            return;

        // Ma lao về phía người chơi
        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position += direction.normalized * rushSpeed * Time.deltaTime;

        // Khi tới gần thì biến mất
        if (Vector3.Distance(transform.position, player.position) <= disappearDistance)
        {
            gameObject.SetActive(false);
        }
    }

    public void StartRush()
    {
        if (rushing)
            return;

        rushing = true;

        if (rushSound != null)
            rushSound.Play();
    }
}