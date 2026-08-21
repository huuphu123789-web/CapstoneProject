using TMPro;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask  interactableLayer;
    [SerializeField] private TextMeshProUGUI hitText;
    
    [SerializeField] private Animator armAnimator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseMenuController.instance != null && PauseMenuController.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            return;
        }
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
        {
            if (hitText != null) hitText.gameObject.SetActive(false);
            return;
        }

        PlayerInteraction();
    }

    public void PlayerInteraction()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit ,interactDistance,interactableLayer))
        {
            Interactable interactable =  hit.collider.GetComponent<Interactable>();

            if (interactable != null)
            {
                //*Hiện gợi ý lên màn hình
                if(hitText != null)
                {
                    hitText.text = "[E] - " + interactable.promptMessage;
                    hitText.gameObject.SetActive(true);
                }
                if(Input.GetKeyDown(KeyCode.E))
                {
                    if(armAnimator !=null)
                    {
                        armAnimator.SetTrigger("Interact");
                    }
                    //*Thực hiện tương tác
                    interactable.Interact();
                  
                }
                return;
            } 
        }

        //*Nếu không thấy vật thể nào hoặc ở quá xa, ẩn đi
        if(hitText != null)
        {
            hitText.gameObject.SetActive(false);
        }
    }
}
