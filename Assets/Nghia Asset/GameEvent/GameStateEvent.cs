using System;
using UnityEngine;
using UnityEngine.Events;

public class GameStateEvent : MonoBehaviour
{
    public static GameStateEvent Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Header("Current State")]
    [SerializeField]
    private GameState currentState = GameState.MainMenu;

    public GameState CurrentState => currentState;

    [Header("State Events")]

    public UnityEvent OnGameStart;

    public UnityEvent OnGamePause;

    public UnityEvent OnGameResume;

    public UnityEvent OnGameOver;

    public UnityEvent OnVictory;

    public UnityEvent OnGameRestart;


    public event Action<GameState> OnStateChanged;

    public event Action OnStarted;

    public event Action OnPaused;

    public event Action OnResumed;

    public event Action OnGameOverEvent;

    public event Action OnVictoryEvent;

    public event Action OnRestart;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(currentState);
    }


    public void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        GameState oldState = currentState;

        currentState = newState;

        Debug.Log(
            "Game State: " +
            oldState +
            " → " +
            newState
        );

        OnStateChanged?.Invoke(newState);

        HandleState(newState);
    }


    private void HandleState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:

                Time.timeScale = 1f;

                break;

            case GameState.Loading:

                Time.timeScale = 1f;

                break;

            case GameState.Playing:

                Time.timeScale = 1f;

                OnStarted?.Invoke();
                OnGameStart?.Invoke();

                break;

            case GameState.Paused:

                Time.timeScale = 0f;

                OnPaused?.Invoke();
                OnGamePause?.Invoke();

                break;

            case GameState.GameOver:

                Time.timeScale = 0f;

                OnGameOverEvent?.Invoke();
                OnGameOver?.Invoke();

                break;

            case GameState.Victory:

                Time.timeScale = 0f;

                OnVictoryEvent?.Invoke();
                OnVictory?.Invoke();

                break;
        }
    }


    public void StartGame()
    {
        SetState(GameState.Playing);
    }


    public void PauseGame()
    {
        if (currentState != GameState.Playing)
            return;

        SetState(GameState.Paused);
    }


    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
            return;

        SetState(GameState.Playing);
    }


    public void GameOver()
    {
        if (currentState == GameState.GameOver)
            return;

        SetState(GameState.GameOver);
    }


    public void Victory()
    {
        if (currentState == GameState.Victory)
            return;

        SetState(GameState.Victory);
    }


    public void RestartGame()
    {
        Time.timeScale = 1f;

        OnRestart?.Invoke();
        OnGameRestart?.Invoke();

        SetState(GameState.Playing);
    }


    public void StartLoading()
    {
        SetState(GameState.Loading);
    }


    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }

    public bool IsPaused()
    {
        return currentState == GameState.Paused;
    }

    public bool IsGameOver()
    {
        return currentState == GameState.GameOver;
    }

    public bool IsVictory()
    {
        return currentState == GameState.Victory;
    }
}