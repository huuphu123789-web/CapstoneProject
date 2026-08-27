using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneEvent : MonoBehaviour
{
    public static SceneEvent Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Events")]
    public UnityEvent OnSceneStart;
    public UnityEvent OnSceneLoaded;
    public UnityEvent OnSceneUnloaded;
    public UnityEvent OnSceneLoadComplete;

    // C# Events
    public event Action<string> SceneLoadStarted;
    public event Action<string> SceneLoaded;
    public event Action<string> SceneUnloaded;
    public event Action<float> LoadingProgress;

    private bool isLoading;

    public bool IsLoading => isLoading;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Unity Scene Events
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        OnSceneStart?.Invoke();

        Debug.Log(
            "Scene Start: " +
            SceneManager.GetActiveScene().name
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }
    }


    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name);

        OnSceneLoaded?.Invoke();

        SceneLoaded?.Invoke(scene.name);
    }


    private void HandleSceneUnloaded(Scene scene)
    {
        Debug.Log("Scene Unloaded: " + scene.name);

        OnSceneUnloaded?.Invoke();

        SceneUnloaded?.Invoke(scene.name);
    }


    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneAsync(sceneName));
    }


    public void LoadScene(int sceneIndex)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneAsync(sceneIndex));
    }


    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;

        SceneLoadStarted?.Invoke(sceneName);

        Debug.Log("Start Loading: " + sceneName);

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError(
                "Cannot load scene: " + sceneName
            );

            isLoading = false;
            yield break;
        }

        while (!operation.isDone)
        {
            float progress =
                Mathf.Clamp01(operation.progress / 0.9f);

            LoadingProgress?.Invoke(progress);

            yield return null;
        }

        LoadingProgress?.Invoke(1f);

        isLoading = false;

        OnSceneLoadComplete?.Invoke();

        Debug.Log(
            "Scene Load Complete: " + sceneName
        );
    }


    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        isLoading = true;

        string sceneName =
            SceneUtility.GetScenePathByBuildIndex(sceneIndex);

        SceneLoadStarted?.Invoke(sceneName);

        Debug.Log(
            "Start Loading Scene Index: " +
            sceneIndex
        );

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneIndex);

        if (operation == null)
        {
            Debug.LogError(
                "Cannot load scene index: " +
                sceneIndex
            );

            isLoading = false;
            yield break;
        }

        while (!operation.isDone)
        {
            float progress =
                Mathf.Clamp01(operation.progress / 0.9f);

            LoadingProgress?.Invoke(progress);

            yield return null;
        }

        LoadingProgress?.Invoke(1f);

        isLoading = false;

        OnSceneLoadComplete?.Invoke();

        Debug.Log(
            "Scene Load Complete: " +
            sceneIndex
        );
    }


    public void ReloadCurrentScene()
    {
        if (isLoading)
            return;

        Scene currentScene =
            SceneManager.GetActiveScene();

        LoadScene(currentScene.name);
    }


    public void LoadNextScene()
    {
        if (isLoading)
            return;

        int currentIndex =
            SceneManager.GetActiveScene().buildIndex;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                "No next scene available."
            );

            return;
        }

        LoadScene(nextIndex);
    }


    public void LoadPreviousScene()
    {
        if (isLoading)
            return;

        int currentIndex =
            SceneManager.GetActiveScene().buildIndex;

        int previousIndex = currentIndex - 1;

        if (previousIndex < 0)
        {
            Debug.LogWarning(
                "No previous scene available."
            );

            return;
        }

        LoadScene(previousIndex);
    }


    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }


    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }
}