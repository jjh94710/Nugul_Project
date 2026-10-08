using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AVP_Controller_Switch : MonoBehaviour
{
    public GameObject FIRST_PERSON_Controller;
    public GameObject ORBIT_Controller;
    public GameObject CINEMATIC_Controller;
    public GameObject EXTERIOR;

    public GameObject Menu;
    public GameObject Menu_FIRST_PERSON;
    public GameObject Menu_ORBIT;
    public GameObject Menu_CINEMATIC;

    public FirstPersonController firstPersonController;
    public StarterAssetsInputs starterAssetsInputs;

    private GameObject[] Roofs;
    private GameObject[] Walls_Cap;

    private void Start()
    {
        Roofs = GameObject.FindGameObjectsWithTag("Roof");
        Walls_Cap = GameObject.FindGameObjectsWithTag("Walls_Cap");
        Show_Exterior();
    }

    void MENU_OPEN()
    {
        Menu.SetActive(true);
    }
    void MENU_CLOSE()
    {
        Menu.SetActive(false);
    }
    public void MENU_FIRST_PERSON() {
        MENU_INFO_HIDE();
        Menu_FIRST_PERSON.SetActive(true);
    }
    public void MENU_ORBIT()
    {
        MENU_INFO_HIDE();
        Menu_ORBIT.SetActive(true);
    }
    public void MENU_CINEMATIC()
    {
        MENU_INFO_HIDE();
        Menu_CINEMATIC.SetActive(true);
    }
    public void MENU_INFO_HIDE()
    {
        Menu_FIRST_PERSON.SetActive(false);
        Menu_ORBIT.SetActive(false);
        Menu_CINEMATIC.SetActive(false);
    }


    void Update()
    {

        if (Input.GetKeyUp("1"))
        {
            FIRST_PERSON();
        }
        if (Input.GetKeyUp("2"))
        {
            ORBIT();
        }
        if (Input.GetKeyUp("3"))
        {
            CINEMATIC();
        }
        if (Input.GetKeyUp("escape"))
        {
            //Application.Quit();
            if (!Menu.activeInHierarchy)
            {
                MENU_OPEN();
                firstPersonController.enabled = false;
                CURSOR_UNLOCK();
            } else
            {
                MENU_CLOSE();
                firstPersonController.enabled = true;
                CURSOR_LOCK();
            }
        }
    }

    public void FIRST_PERSON()
    {
        FIRST_PERSON_Controller.SetActive(true);
        ORBIT_Controller.SetActive(false);
        CINEMATIC_Controller.SetActive(false);
        EXTERIOR.SetActive(true);
        Show_Exterior();

        MENU_CLOSE();
        CURSOR_LOCK();
        firstPersonController.enabled = true;
    }
    public void ORBIT()
    {
        FIRST_PERSON_Controller.SetActive(false);
        ORBIT_Controller.SetActive(true);
        CINEMATIC_Controller.SetActive(false);
        EXTERIOR.SetActive(false);
        Hide_Exterior();

        MENU_CLOSE();
        CURSOR_UNLOCK();
    }
    public void CINEMATIC()
    {
        FIRST_PERSON_Controller.SetActive(false);
        ORBIT_Controller.SetActive(false);
        CINEMATIC_Controller.SetActive(true);
        EXTERIOR.SetActive(true);
        Show_Exterior();

        MENU_CLOSE();
        CURSOR_LOCK();
    }

    public void EXIT()
    {
        Application.Quit();
    }

    public void CURSOR_LOCK()
    {
        starterAssetsInputs.cursorLocked = true;
        SetCursorState(starterAssetsInputs.cursorLocked);
    }
    public void CURSOR_UNLOCK()
    {
        starterAssetsInputs.cursorLocked = false;
        SetCursorState(starterAssetsInputs.cursorLocked);
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(starterAssetsInputs.cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !newState;
    }

    public void Show_Exterior()
    {

        foreach (GameObject go in Roofs)
        {
            go.SetActive(true);
        }
        foreach (GameObject go2 in Walls_Cap)
        {
            go2.SetActive(false);
        }
    }

    public void Hide_Exterior()
    {
        foreach (GameObject go in Roofs)
        {
            go.SetActive(false);
        }
        foreach (GameObject go2 in Walls_Cap)
        {
            go2.SetActive(true);
        }
    }

}

