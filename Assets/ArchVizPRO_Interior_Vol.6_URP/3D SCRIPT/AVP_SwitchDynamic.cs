using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AVP_SwitchDynamic : MonoBehaviour
{
    public GameObject Realtime;

    void Start()
    {
        gameObject.SetActive(false);
        Realtime.SetActive(true);
    }
}
