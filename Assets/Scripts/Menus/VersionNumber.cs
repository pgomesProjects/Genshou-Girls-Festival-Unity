using UnityEngine;
using TMPro;


[RequireComponent(typeof(TextMeshProUGUI))]
public class VersionNumber : MonoBehaviour
{
    private TextMeshProUGUI text;
    private string versionCommand = "[Version]";
    private string unityVersionCommand = "[Unity]";

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        string message = text.text;

        //Replace any commands in the text with the up-to-date info
        message = message.Replace(versionCommand, Application.version);
        message = message.Replace(unityVersionCommand, Application.unityVersion);
        text.text = message;
    }
}
