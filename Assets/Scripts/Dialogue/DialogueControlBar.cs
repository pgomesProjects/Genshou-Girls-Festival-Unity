using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueControlBar : MonoBehaviour
{
    [SerializeField, Tooltip("The color for buttons when they are toggled.")] private Color toggleColor;
    [Space()]
    [SerializeField, Tooltip("The auto button.")] private Button autoButton;
    [SerializeField, Tooltip("The fast forward button.")] private Button fastForwardButton;

    private RectTransform controlBarRectTransform;
    private GameMenuController gameMenuController;

    private Color autoDefaultColor;
    private Color fastForwardDefaultColor;

    private void Awake()
    {
        autoDefaultColor = autoButton.colors.normalColor;
        fastForwardDefaultColor = fastForwardButton.colors.normalColor;
    }

    private void OnEnable()
    {
        //Rebuild the layout for more consistent layout visuals
        controlBarRectTransform = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(controlBarRectTransform);
        gameMenuController = FindFirstObjectByType<GameMenuController>();
    }

    /// <summary>
    /// Pauses the game and goes to the transcript menu.
    /// </summary>
    public void QuickMenuTranscript()
    {
        gameMenuController?.ToggleGameMenu(GameMenuController.SubMenu.Transcript);
    }

    /// <summary>
    /// Pauses the game and goes to the options menu.
    /// </summary>
    public void QuickMenuOptions()
    {
        gameMenuController?.ToggleGameMenu(GameMenuController.SubMenu.Options);
    }

    /// <summary>
    /// Lets the text automatically advance.
    /// </summary>
    public void ToggleAuto()
    {
        //Toggle the auto playback state
        if(DialogueManager.playbackState == DialogueManager.PlaybackState.Auto)
        {
            DialogueManager.playbackState = DialogueManager.PlaybackState.Normal;
        }
        else
        {
            DialogueManager.playbackState = DialogueManager.PlaybackState.Auto;
            DialogueManager.Instance.BeginAutoRead();
        }

        DialogueManager.Instance.RefreshAutoIndicator();

        //Change the normal color of the auto button
        ColorBlock colors = autoButton.colors;
        colors.normalColor = DialogueManager.playbackState == DialogueManager.PlaybackState.Auto ? toggleColor : autoDefaultColor;
        autoButton.colors = colors;
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ToggleSkip()
    {
        //Toggle the fast forward state
        if (DialogueManager.playbackState == DialogueManager.PlaybackState.FastForward)
            DialogueManager.playbackState = DialogueManager.PlaybackState.Normal;
        else
            DialogueManager.playbackState = DialogueManager.PlaybackState.FastForward;

        //Change the normal color of the fast forward button
        ColorBlock colors = fastForwardButton.colors;
        colors.normalColor = DialogueManager.playbackState == DialogueManager.PlaybackState.FastForward ? toggleColor : fastForwardDefaultColor;
        fastForwardButton.colors = colors;
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Returns true / false depending on whether any of the buttons in the control bar are being used.
    /// </summary>
    /// <returns>True if any button is highlighted. False if otherwise.</returns>
    public bool InUse()
    {
        //Return true if any of the buttons in the control bar are highlighted
        foreach(var button in GetComponentsInChildren<MenuButton>())
            if (button.IsHighlighted)
                return true;

        return false;
    }
}
