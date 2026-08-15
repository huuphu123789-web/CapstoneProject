using UnityEngine;

public class GhostRushTrigger : MonoBehaviour
{
    public GhostRush ghost;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ghost.StartRush();
            Destroy(gameObject);
        }
    }
}