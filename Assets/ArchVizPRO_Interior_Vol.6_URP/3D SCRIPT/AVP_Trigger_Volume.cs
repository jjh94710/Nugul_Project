using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AVP_Trigger_Volume : MonoBehaviour
{
    private GameObject[] Roofs;
    private GameObject[] Walls_Cap;
    private void Start()
    {
        Roofs = GameObject.FindGameObjectsWithTag("Roof");
        Walls_Cap = GameObject.FindGameObjectsWithTag("Walls_Cap");

        foreach (GameObject go in Roofs)
        {
            go.SetActive(false);
        }
        foreach (GameObject go2 in Walls_Cap)
        {
            go2.SetActive(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag =="Player")
        {
            Hide_Exterior();
        }
    }

    private void OnTriggerExit(Collider other)
        {
            if (other.tag == "Player")
        {
            Show_Exterior();
        }
    }

    public void Hide_Exterior()
    {

        foreach (GameObject go in Roofs)
        {
            go.SetActive(true);
        }
        foreach (GameObject go2 in Walls_Cap)
        {
            go2.SetActive(false);
        }
        Debug.Log("I am inside");
    }

    public void Show_Exterior()
    {
        foreach (GameObject go in Roofs)
        {
            go.SetActive(false);
        }
        foreach (GameObject go2 in Walls_Cap)
        {
            go2.SetActive(true);
        }
        Debug.Log("I am otside");
    }
}
