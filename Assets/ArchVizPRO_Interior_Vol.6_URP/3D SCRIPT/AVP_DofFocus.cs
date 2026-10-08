using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class AVP_DofFocus : MonoBehaviour
{
    private Camera cameraMain;
    public VolumeProfile postFxProfile;
    private DepthOfField dof;
    public MinFloatParameter dofDistanceParametar;

    private Ray ray;
    private RaycastHit hit;
    // private bool isHit = false;

    public float defaultDistance = 5f;
    public float minDistance = 0.5f;
    private float hitDistance;
    public float focusSpeedIn = 1f;
    public float focusSpeedOut = 1f;
    public int updateFrequency = 2;

    private Transform thisTransform;

    private void Awake()
    {
        thisTransform = transform;
    }

    private void Start()
    {
        postFxProfile = GetComponent<Volume>().profile;
        postFxProfile.TryGet<DepthOfField>(out dof);
        dofDistanceParametar = dof.focusDistance;

        dofDistanceParametar.overrideState = true;
        cameraMain = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Time.frameCount % updateFrequency == 0)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, defaultDistance - 0.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                // isHit = true;
                hitDistance = hit.distance;
                if (hitDistance < minDistance)
                {
                    hitDistance = minDistance;
                }
                dofDistanceParametar.value = Mathf.Lerp(dofDistanceParametar.value, hitDistance, focusSpeedIn);
            }
            else
            {
                // isHit = false;
                if (dofDistanceParametar.value < defaultDistance)
                {
                    dofDistanceParametar.value = Mathf.Lerp(dofDistanceParametar.value, defaultDistance, focusSpeedOut);
                }
            }
        }
    }
}