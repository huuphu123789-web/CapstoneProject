using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên nút Back trong PauseMenuPanel.
/// Tự động tìm PlayerHUDManager và gọi ResumeGame() khi click.
/// </summary>
[RequireComponent(typeof(Button))]
public class BackButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnBackClicked);
    }

    private void OnBackClicked()
    {
        if (PauseMenuController.instance != null)
        {
            PauseMenuController.instance.BackToGame();
        }
    }
}
