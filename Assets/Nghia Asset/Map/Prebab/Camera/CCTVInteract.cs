using UnityEngine;
using UnityEngine.InputSystem;

public class CCTVInteract : MonoBehaviour
{
    [Header("CCTV UI")]
    public GameObject cctvPanel;

    [Header("CCTV Camera")]
    public Camera cctvCamera;

    private bool playerInside = false;
    private bool cctvActive = false;

    private void Start()
    {
        cctvPanel.SetActive(false);
        cctvCamera.enabled = true;
    }

    private void Update()
    {
        if (playerInside && Keyboard.current.eKey.wasPressedThisFrame)
        {
            cctvActive = !cctvActive;

            cctvPanel.SetActive(cctvActive);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            if (cctvActive)
            {
                cctvActive = false;
                cctvPanel.SetActive(false);
            }
        }
    }
}