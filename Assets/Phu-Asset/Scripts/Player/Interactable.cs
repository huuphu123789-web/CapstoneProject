using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Cài Đặt Tương Tác")]
    [Tooltip("Dòng chữ hướng dẫn hiện lên màn hình")]
    public string  promptMessage = "Interact";

    [Header("Âm Thanh Tương Tác")]
    public AudioClip interactSound; //*Mỗi vật thể tự kéo âm thanh riêng vào đây

    //* Hàm này sẽ gọi khi người chơi bấm E
    //* Virtual cho phép ghi đè nội dung
    public virtual void Interact()
    {
        if(interactSound != null &&  AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(interactSound);
        }
        Debug.Log("Đã tương tác với: " + gameObject.name);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
