using UnityEngine;

public class lb_CrowProximity : MonoBehaviour
{
    [Header("Crow Detection")]
    [SerializeField] private bool detectBirds = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!detectBirds)
            return;

        if (!other.CompareTag("lb_bird"))
            return;

        other.SendMessage(
            "CrowIsClose",
            SendMessageOptions.DontRequireReceiver
        );
    }
}