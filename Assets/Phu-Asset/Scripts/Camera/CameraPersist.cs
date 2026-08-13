using UnityEngine;

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
    }
}