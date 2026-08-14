using UnityEngine;

public class GhostWalkTrigger : MonoBehaviour
{
    public GhostWalk ghost;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ghost.StartWalking();

        Destroy(gameObject);
    }
}