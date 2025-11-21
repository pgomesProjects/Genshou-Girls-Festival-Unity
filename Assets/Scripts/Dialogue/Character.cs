using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField, Tooltip("The position for the camera when this character speaks.")] private Transform cameraTransform;
    [SerializeField, Tooltip("If true, the character rotates towards the camera.")] private bool rotateWithCamera = true;

    private WorldCamera worldCamera;

    private void Start()
    {
        worldCamera = FindFirstObjectByType<WorldCamera>();
    }

    private void Update()
    {
        if (rotateWithCamera)
            RotateWithCamera();
    }

    /// <summary>
    /// Rotates the y-axis of the character to match the camera.
    /// </summary>
    private void RotateWithCamera()
    {
        //If there is no camera, return
        if (worldCamera == null)
            return;

        //Rotate the character along the y-axis to match the camera
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.y = worldCamera.transform.localEulerAngles.y;
        transform.localEulerAngles = currentRotation;
    }

    public Transform GetCameraTransform() => cameraTransform;
}
