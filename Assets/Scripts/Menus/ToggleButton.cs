using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [SerializeField, Tooltip("The color of the text when toggled.")] private Color toggleColor;

    private Color defaultTextColor;

    /// <summary>
    /// Sets the toggle of the button.
    /// </summary>
    /// <param name="isOn">The toggle value for the button.</param>
    public void SetToggle(bool isOn)
    {
        //If the default text color hasn't been set, set it
        if(defaultTextColor == Color.clear)
            defaultTextColor = GetComponent<Toggle>().colors.normalColor;

        //Set the visuals for the button
        ColorBlock colors = GetComponent<Toggle>().colors;
        colors.normalColor = isOn ? toggleColor : defaultTextColor;
        GetComponent<Toggle>().colors = colors;
    }
}
