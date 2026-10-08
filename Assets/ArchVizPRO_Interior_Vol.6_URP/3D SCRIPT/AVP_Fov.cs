using UnityEngine;
using System.Collections;

public class AVP_Fov : MonoBehaviour
{
    //[Tooltip("CinemachineCamera da controllare")]
    //public Camera virtualCamera;   // nuovo tipo

    //private float originalFOV;
    //public float zoomSpeed = 5f;

    //private bool isZooming;

    //private void Awake()
    //{
    //    // fallback automatico se lo script ?sullo stesso GameObject
    //    if (virtualCamera == null)
    //        virtualCamera = GetComponent<virtualCamera>();
    //}

    //private void Start()
    //{
    //    originalFOV = virtualCamera.Lens.FieldOfView;   // nuovo campo Lens
    //}

    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(1))
    //    {
    //        isZooming = true;
    //        StopAllCoroutines();
    //        StartCoroutine(ZoomIn());
    //    }

    //    if (Input.GetMouseButtonUp(1))
    //    {
    //        isZooming = false;
    //        StopAllCoroutines();
    //        StartCoroutine(ZoomOut());
    //    }
    //}

    //private IEnumerator ZoomIn()
    //{
    //    while (isZooming && virtualCamera.Lens.FieldOfView > 28f)
    //    {
    //        virtualCamera.Lens.FieldOfView -= zoomSpeed * Time.deltaTime;
    //        yield return null;
    //    }
    //}

    //private IEnumerator ZoomOut()
    //{
    //    while (virtualCamera.Lens.FieldOfView < originalFOV)
    //    {
    //        virtualCamera.Lens.FieldOfView += zoomSpeed * Time.deltaTime;
    //        yield return null;
    //    }

    //    // assicura che la FOV torni esattamente al valore originale
    //    virtualCamera.Lens.FieldOfView = originalFOV;
    //}
}
