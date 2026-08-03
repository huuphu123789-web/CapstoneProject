using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
public class ButtonHover : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    //*1. Hàm này tự động chạy khi chuột RÊ VÀO nút
    public void OnPointerEnter(PointerEventData envenData)
    {
        //*Kiem tra AudioManager da duoc bat chua  va co file am thanh chua
        if(AudioManager.instance != null && AudioManager.instance.buttonHoverSFX != null)
        {
            //*Goi Audiomanager phat am thanh re chuot
            AudioManager.instance.PlaySFX(AudioManager.instance.buttonHoverSFX);
        }
    }
    //* 2. Hàm này tự động chạy khi chuột RỜI KHỎI nút
    public void OnPointerExit(PointerEventData eventData)
{
    // Để trống
}
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.instance.PlaySFX(AudioManager.instance.buttonclickSFX);
    }
}
