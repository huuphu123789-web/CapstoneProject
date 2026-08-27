using UnityEngine;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void OnEnable()
    {
        if (GameStateEvent.Instance != null)
        {
            GameStateEvent.Instance.OnPaused += ShowPause;
            GameStateEvent.Instance.OnResumed += HidePause;
        }
    }

    private void OnDisable()
    {
        if (GameStateEvent.Instance != null)
        {
            GameStateEvent.Instance.OnPaused -= ShowPause;
            GameStateEvent.Instance.OnResumed -= HidePause;
        }
    }

    private void ShowPause()
    {
        pausePanel.SetActive(true);
    }

    private void HidePause()
    {
        pausePanel.SetActive(false);
    }

    public void Resume()
    {
        GameStateEvent.Instance.ResumeGame();
    }
}