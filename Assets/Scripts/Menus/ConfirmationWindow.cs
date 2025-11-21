using UnityEngine;
using TMPro;

public class ConfirmationWindow : MonoBehaviour
{
    [SerializeField, Tooltip("The text for the confirmation window.")] private TextMeshProUGUI questionText;

    private System.Action onConfirmed;


    /// <summary>
    /// Initializes the confirmation window.
    /// </summary>
    /// <param name="message">The message for the window.</param>
    /// <param name="onConfirmed">The action to take when yes is pressed.</param>
    public void Initialize(string message, System.Action onConfirmed)
    {
        questionText.text = message;
        this.onConfirmed = onConfirmed;
    }

    /// <summary>
    /// Closes the window after invoking the yes option.
    /// </summary>
    public void YesOption()
    {
        onConfirmed?.Invoke();
        CloseWindow();
    }

    /// <summary>
    // Closes the confirmation window without any action.
    /// </summary>
    public void NoOption() => CloseWindow();

    /// <summary>
    /// Closes the window.
    /// </summary>
    private void CloseWindow()
    {
        Destroy(gameObject);
    }
}
