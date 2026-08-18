
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
        if (mainmenuPanel == null)
        {
            Transform mm = transform.parent != null ? transform.parent.Find("MainMenu") : null;
            if (mm == null) mm = GameObject.Find("MainMenu")?.transform;
            if (mm != null) mainmenuPanel = mm.gameObject;
        }

        if (settingPanel == null)
        {
            Transform sp = transform.parent != null ? transform.parent.Find("SettingPanel") : null;
            if (sp == null) sp = GameObject.Find("SettingPanel")?.transform;
            if (sp != null) settingPanel = sp.gameObject;
        }
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
        if (settingPanel != null) settingPanel.SetActive(true);
        if (mainmenuPanel != null) mainmenuPanel.SetActive(false);
    }

    public void CloseSetting()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainmenuPanel != null) mainmenuPanel.SetActive(true);
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
    public void BackToMainMenu()
{
    // Xóa Player trước khi về menu
    GameObject player = GameObject.FindWithTag("Player");
    if (player != null) Destroy(player);

    // Xóa Main Camera persistent
    Camera cam = Camera.main;
    if (cam != null) Destroy(cam.gameObject);

    SceneManager.LoadScene("MainMenu");
}
}
