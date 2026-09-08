
using System;
using System.Collections;
using BloodlinesUI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
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

        DisableButtonNavigation();
    }

    private void DisableButtonNavigation()
    {
        if (mainmenuPanel != null)
        {
            UnityEngine.UI.Selectable[] selectables = mainmenuPanel.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
            foreach (var s in selectables)
            {
                var nav = s.navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.None;
                s.navigation = nav;
            }
        }
        if (settingPanel != null)
        {
            UnityEngine.UI.Selectable[] selectables = settingPanel.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
            foreach (var s in selectables)
            {
                var nav = s.navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.None;
                s.navigation = nav;
            }
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
         ResetSelectables(mainmenuPanel);
         if (settingPanel != null) settingPanel.SetActive(true);
         if (mainmenuPanel != null) mainmenuPanel.SetActive(false);
     }

     public void CloseSetting()
     {
         if (settingPanel != null) settingPanel.SetActive(false);
         if (mainmenuPanel != null) mainmenuPanel.SetActive(true);
         ResetSelectables(mainmenuPanel);
         StartCoroutine(ClearSelectionNextFrame());
     }

     private void ResetSelectables(GameObject panel)
     {
         if (panel == null) return;
         UnityEngine.UI.Selectable[] selectables = panel.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
         foreach (var s in selectables)
         {
             s.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
             
             Animator anim = s.GetComponent<Animator>();
             if (anim != null && anim.isActiveAndEnabled)
             {
                 anim.Play("Normal", 0, 0f);
                 anim.Update(0f);
             }
             
             if (s.targetGraphic != null)
             {
                 s.targetGraphic.canvasRenderer.SetColor(s.colors.normalColor);
                 s.targetGraphic.CrossFadeColor(s.colors.normalColor, 0f, true, true);
             }
         }
     }

     private IEnumerator ClearSelectionNextFrame()
     {
         yield return null;
         if (UnityEngine.EventSystems.EventSystem.current != null)
         {
             UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
         }
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
