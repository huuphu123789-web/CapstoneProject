using UnityEngine;

public class GhostDisappear : MonoBehaviour
{
    public float disappearDistance = 4f;
    public AudioSource disappearSound;

    private Transform player;
    private bool disappeared = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (disappeared) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= disappearDistance)
        {
            disappeared = true;

            if (disappearSound != null)
                disappearSound.Play();

            GetComponentInChildren<Renderer>().enabled = false;

            Destroy(gameObject, 1f);
        }
    }
}