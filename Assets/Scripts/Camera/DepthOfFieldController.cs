using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class DepthOfFieldController : MonoBehaviour
{
    [SerializeField, Tooltip("The speed in which the camera focuses on an object."), Range(1f, 10f)] private float focusSpeed;
    [SerializeField, Tooltip("The maximum amount of distance the camera will take into account when focusing on objects.")] private float maxFocusDistance;
    public GameObject focusObject;

    private Ray raycast;
    private RaycastHit hit;
    private bool isHit;
    private float hitDistance;

    private PostProcessVolume volume;
    private DepthOfField depthOfField;

    private void Start()
    {
        volume = GetComponent<PostProcessVolume>();

        if (volume == null)
            Destroy(gameObject);

        volume.profile.TryGetSettings(out depthOfField);
    }

    private void Update()
    {
        raycast = new Ray(transform.position, transform.forward * maxFocusDistance);

        isHit = false;

        if(focusObject != null)
        {
            hitDistance = Vector3.Distance(transform.position, focusObject.transform.position);
        }
        else
        {
            if (Physics.Raycast(raycast, out hit, maxFocusDistance))
            {
                isHit = true;
                hitDistance = Vector3.Distance(transform.position, hit.point);
            }
            else
            {
                if (hitDistance < maxFocusDistance)
                {
                    hitDistance++;
                }
            }
        }

        SetFocus();
    }

    private void SetFocus()
    {
        depthOfField.focusDistance.value = Mathf.Lerp(depthOfField.focusDistance.value, hitDistance, Time.deltaTime * focusSpeed);
    }

    private void OnDrawGizmos()
    {
        if (isHit)
        {
            Gizmos.DrawSphere(hit.point, 0.1f);
            Debug.DrawRay(transform.position, transform.forward * Vector3.Distance(transform.position, hit.point));
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * maxFocusDistance);
        }
    }
}
