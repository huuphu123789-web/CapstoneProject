
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
   [SerializeField] public GameObject settingPanel;
   [SerializeField] private GameObject mainmenuPanel;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Day1");
    }

    public void Continue()
    {
        
    }

     public void OpenSetting()
    {
        settingPanel.SetActive(true);
        mainmenuPanel.SetActive(false);
    }

    public void CloseSetting()
    {
        settingPanel.SetActive(false);
        mainmenuPanel.SetActive(true);
    }
    public void ExitGame()
    {
        Debug.Log("Thoát game!");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Tắt chế độ Play trong Unity Editor
        #else
            Application.Quit(); // Thoát game thực tế trên máy tính
        #endif
    }
}
