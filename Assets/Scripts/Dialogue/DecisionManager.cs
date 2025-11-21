using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class DecisionManager : MonoBehaviour
{
    [SerializeField, Tooltip("The container for the decisions.")] private RectTransform decisionContainer;
    [SerializeField, Tooltip("The prefab for the decision items.")] private DecisionItem decisionItemPrefab;
    [Space()]
    [SerializeField, Tooltip("The image for the background dim.")] private Image dimImage;
    [SerializeField, Tooltip("The RectTransform for the menu UI.")] private RectTransform menuUI;
    [SerializeField, Tooltip("The alpha value for the dim image.")] private float dimAlpha;
    [SerializeField, Tooltip("The duration of the slide animation.")] private float slideAniDuration;
    [Space()]
    [SerializeField, Tooltip("The offset for the camera when active.")] private float cameraXOffset;
    [Space()]
    [SerializeField] private bool debugSlideIn = false;
    [SerializeField] private bool debugSlideOut = false;

    private bool slideInActive;
    private bool slideOutActive;
    
    private float slideAniElapsed;
    private Vector2 menuHidePos;

    private CinemachineCamera cinemachineCamera;
    private CinemachineBasicMultiChannelPerlin cinemachineNoiseProfile;

    public static System.Action OnDecisionEnd;

    private void Start()
    {
        if(WorldCamera.main != null)
        {
            cinemachineCamera = WorldCamera.main.GetVirtualCamera();

            //Get the noise profile from the cinemachine camera and turn it off
            cinemachineNoiseProfile = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineNoiseProfile.enabled = false;
        }

        menuHidePos = menuUI.anchoredPosition;
    }

    /// <summary>
    /// Creates a list of decisions for the player to choose.
    /// </summary>
    /// <param name="decisions">The list of decisions to create.</param>
    public void CreateDecisions(string[] decisions)
    {
        //Clear any previous decisions
        foreach (Transform trans in decisionContainer)
            Destroy(trans.gameObject);

        //Create a decision item for each decision given
        for(int i = 0; i < decisions.Length; i++)
        {
            DecisionItem newDecision = Instantiate(decisionItemPrefab, decisionContainer);
            newDecision.SetDecision(i, decisions[i]);
        }

        //Show the UI
        ShowUI();
    }

    /// <summary>
    /// Shows the decision UI.
    /// </summary>
    public void ShowUI()
    {
        //If an animation is already active, return
        if (slideInActive || slideOutActive)
            return;

        //Set decision active to true
        DialogueManager.Instance.SetDecisionActive(true);

        slideInActive = true;
        slideAniElapsed = 0f;
    }

    /// <summary>
    /// Hides the decision UI.
    /// </summary>
    public void HideUI()
    {
        //If an animation is already active, return
        if (slideInActive || slideOutActive)
            return;

        slideOutActive = true;
        slideAniElapsed = 0f;
    }

    private void Update()
    {
        //Slide in animation
        if (slideInActive)
        {
            if(slideAniElapsed >= slideAniDuration)
            {
                //Set the final alpha value
                Color dimColor = dimImage.color;
                dimColor.a = dimAlpha;
                dimImage.color = dimColor;

                //Set the menu position
                menuUI.anchoredPosition = Vector2.zero;

                //Show the menu wobble
                if(cinemachineCamera != null)
                    cinemachineNoiseProfile.enabled = true;

                slideInActive = false;
            }
            else
            {
                slideAniElapsed += Time.deltaTime;
                float t = slideAniElapsed / slideAniDuration;

                //Transition the dim color in
                Color dimColor = dimImage.color;
                dimColor.a = Mathf.Lerp(0f, dimAlpha, t);
                dimImage.color = dimColor;

                //Slide the decision menu in frame
                menuUI.anchoredPosition = Vector2.Lerp(menuHidePos, Vector2.zero, t);

                //Update the camera offset
                if (WorldCamera.main != null && !WorldCamera.main.IsProtagCamActive())
                    WorldCamera.main.UpdateCameraOffset(Vector3.Lerp(Vector3.zero, new Vector3(cameraXOffset, 0f, 0f), t));
            }
        }

        //Slide out animation
        if (slideOutActive)
        {
            if (slideAniElapsed >= slideAniDuration)
            {
                //Set the final alpha value
                Color dimColor = dimImage.color;
                dimColor.a = 0f;
                dimImage.color = dimColor;

                //Set the menu position
                menuUI.anchoredPosition = menuHidePos;

                //Stop the menu wobble
                if (cinemachineCamera != null)
                    cinemachineNoiseProfile.enabled = false;

                //Set decision active to false
                DialogueManager.Instance.SetDecisionActive(false);

                slideOutActive = false;
            }
            else
            {
                slideAniElapsed += Time.deltaTime;
                float t = slideAniElapsed / slideAniDuration;

                //Transition the dim color out
                Color dimColor = dimImage.color;
                dimColor.a = Mathf.Lerp(dimAlpha, 0f, t);
                dimImage.color = dimColor;

                //Slide the decision menu out of frame
                menuUI.anchoredPosition = Vector2.Lerp(Vector2.zero, menuHidePos, t);

                //Update the camera offset
                if (!WorldCamera.main.IsProtagCamActive())
                    WorldCamera.main.UpdateCameraOffset(Vector3.Lerp(new Vector3(cameraXOffset, 0f, 0f), Vector3.zero, t));
            }
        }

        //Debug
        if (debugSlideIn)
        {
            CreateDecisions(new string[]{ "Example 1", "Example 2", "Example 3" });
            debugSlideIn = false;
        }

        if (debugSlideOut)
        {
            HideUI();
            debugSlideOut = false;
        }
    }
}
