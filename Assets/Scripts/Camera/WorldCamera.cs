using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldCamera : MonoBehaviour
{
    public static WorldCamera main;

    [SerializeField, Tooltip("The virtual camera to control movement.")] private CinemachineCamera virtualCam;
    [SerializeField, Tooltip("The speed in which the camera moves towards a position.")] private float moveSpeed = 0.2f;
    [SerializeField, Tooltip("The offset of the camera position based on whether the protagonist camera is in view.")] private Vector3 protagCamOffset;

    private bool protagCamActive;
    private Volume characterCamVolume;
    private bool isFocused;
    private Vector3 currentLocation;

    private Vector3 cameraOffset;

    private void Awake()
    {
        //Singleton-ize script
        if (main != null && main != this)
            Destroy(gameObject);
        else
            main = this;

        characterCamVolume = GetComponentInChildren<Volume>();
        characterCamVolume.weight = 0f;
    }

    /// <summary>
    /// Moves the camera to a set position.
    /// </summary>
    /// <param name="newPosition">The position to move the camera to.</param>
    /// <param name="isFocused">If true, the camera focuses on this position.</param>
    public void MoveTo(Vector3 newPosition, bool isFocused)
    {
        //If the camera is trying to move to the same position, return
        if (currentLocation == newPosition)
            return;

        StartCoroutine(MoveCameraAnimation(virtualCam.transform.position, newPosition, isFocused));
    }

    /// <summary>
    /// Moves the in-world camera's position.
    /// </summary>
    /// <param name="startPos">The starting position of the camera.</param>
    /// <param name="endPos">The ending position of the camera.</param>
    /// <param name="focusCamera">If true, the camera focuses on the character while blurring the background.</param>
    private IEnumerator MoveCameraAnimation(Vector3 startPos, Vector3 endPos, bool focusCamera)
    {
        //Set the camera position with any offset applied
        Vector3 finalPosition = endPos;
        currentLocation = endPos;
        if (protagCamActive && focusCamera)
            finalPosition += protagCamOffset;

        isFocused = focusCamera;

        float startVolumeWeight = characterCamVolume.weight;
        float endVolumeWeight = focusCamera ? 1f : 0f;

        float moveElapsed = 0f;
        while (moveElapsed < moveSpeed)
        {
            moveElapsed += Time.deltaTime * DialogueManager.DialogueSpeedMultiplier;
            virtualCam.transform.position = Vector3.Lerp(startPos, finalPosition, moveElapsed / moveSpeed); //Lerp the camera position
            characterCamVolume.weight = Mathf.Lerp(startVolumeWeight, endVolumeWeight, moveElapsed / moveSpeed); //Lerp the weight of the character cam volume
            yield return null;
        }

        //Set the final values
        virtualCam.transform.position = finalPosition;
        characterCamVolume.weight = endVolumeWeight;
        UpdateCameraOffset(protagCamActive && focusCamera ? protagCamOffset : Vector3.zero);
    }

    /// <summary>
    /// Adds an offset to the camera to incorporate the protag cam view.
    /// </summary>
    public void AddProtagOffset()
    {
        //If the protagonist camera view is already active, return
        if (protagCamActive)
            return;

        protagCamActive = true;
        StartCoroutine(MoveCameraAnimation(currentLocation, currentLocation, isFocused));
    }

    /// <summary>
    /// Removes the offset on the camera that incorporates the protag cam view.
    /// </summary>
    public void RemoveProtagOffset()
    {
        //If the protagonist camera view is not active, return
        if (!protagCamActive)
            return;

        protagCamActive = false;
        StartCoroutine(MoveCameraAnimation(currentLocation, currentLocation, isFocused));
    }

    /// <summary>
    /// Adjusts the offset to the camera position.
    /// </summary>
    /// <param name="offset">The offset for the camera.</param>
    public void UpdateCameraOffset(Vector3 offset)
    {
        cameraOffset = offset;
        virtualCam.transform.position = currentLocation + cameraOffset;
    }

    public bool IsProtagCamActive() => protagCamActive;
    public CinemachineCamera GetVirtualCamera() => virtualCam;
}
