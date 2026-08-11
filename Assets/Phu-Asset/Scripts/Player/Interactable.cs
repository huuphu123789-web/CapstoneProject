using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Cài Đặt Tương Tác")]
    [Tooltip("Dòng chữ hướng dẫn hiện lên màn hình")]
    public string  promptMessage = "Interact";

    //* Hàm này sẽ gọi khi người chơi bấm E
    //* Virtual cho phép ghi đè nội dung
    public virtual void Interact()
    {
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
