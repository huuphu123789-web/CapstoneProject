using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraPersist : MonoBehaviour
{
    private static CameraPersist instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.ToLower().Contains("menu") || scene.buildIndex == 0)
        {
            if (instance == this) instance = null;
            Destroy(gameObject);
        }
    }
}