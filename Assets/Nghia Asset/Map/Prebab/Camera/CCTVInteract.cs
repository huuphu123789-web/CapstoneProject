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
        if (cctvPanel != null) cctvPanel.SetActive(false);
        if (cctvCamera != null) cctvCamera.enabled = true;
    }

    private void Update()
    {
        bool ePressed = Input.GetKeyDown(KeyCode.E);
        if (!ePressed && Keyboard.current != null)
        {
            try { ePressed = Keyboard.current.eKey.wasPressedThisFrame; } catch { }
        }

        if (playerInside && ePressed)
        {
            cctvActive = !cctvActive;

            if (cctvPanel != null)
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
                if (cctvPanel != null)
                    cctvPanel.SetActive(false);
            }
        }
    }
}