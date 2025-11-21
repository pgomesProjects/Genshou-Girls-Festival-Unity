using UnityEngine;
using TMPro;

public class TranscriptController : MonoBehaviour
{
    [SerializeField, Tooltip("The maximum amount of lines to show in the transcript.")] private int maxLines = 100;
    [SerializeField, Tooltip("The text to write the transcript on.")] private TextMeshProUGUI transcriptText;

    private void OnEnable()
    {
        transcriptText.text = GameManager.Instance.GetTranscript(maxLines);
    }
}
