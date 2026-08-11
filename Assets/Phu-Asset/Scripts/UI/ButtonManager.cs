
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
   [SerializeField] public GameObject settingPanel;
   [SerializeField] private GameObject mainmenuPanel;
   [SerializeField] private TextMeshProUGUI jumpScareText;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
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
    public void JumpScare()
    {
        jumpScareText.text="Off";
        StartCoroutine(ChangeText());
    }
    IEnumerator ChangeText()
    {
        yield return new WaitForSeconds(0.5f);
        jumpScareText.text="On";
    }
}
