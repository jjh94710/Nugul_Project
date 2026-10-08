using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AVP_Audio_Manager : MonoBehaviour {

    public AudioSource audioClip;

    public float fadeOutFactor = 0.08f;
    public float fadeInFactor = 0.08f;

    public float Max_Volume = 1.0f;
    public float Min_Volume = 0.2f;

    public bool fadeInOut = false;

void Update()
{
    if (fadeInOut == false)
    {
        if (audioClip.volume < Max_Volume)
        {
                audioClip.volume += fadeInFactor * Time.deltaTime;
        }
    }

    if (fadeInOut == true)
    {
        if (audioClip.volume > Min_Volume)
        {
                audioClip.volume -= fadeOutFactor * Time.deltaTime;
        }
    }

}

void OnTriggerEnter(Collider other)
{
    if (other.gameObject.CompareTag("Player"))
    {
        fadeInOut = false;
            Debug.Log("Sono Entrato");
        }
}

    void OnTriggerExit(Collider other)
{
    if (other.gameObject.CompareTag("Player"))
    {
        fadeInOut = true;
            Debug.Log("Sono Fuori");
        }
}    
 }