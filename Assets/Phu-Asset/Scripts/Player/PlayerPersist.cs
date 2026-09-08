using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersist : MonoBehaviour
{
    private static PlayerPersist instance;

    void Awake()
    {
        // Nếu đã có Player rồi → xóa cái mới (tránh bị trùng)
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
        // Nếu quay về màn hình MainMenu thì hủy Player để tránh đè UI và camera
        if (scene.name.ToLower().Contains("menu") || scene.buildIndex == 0)
        {
            if (instance == this) instance = null;
            Destroy(gameObject);
        }
    }
}