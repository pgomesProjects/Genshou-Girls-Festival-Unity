using UnityEngine;
using TMPro;

public class DecisionItem : MonoBehaviour
{
    private TextMeshProUGUI decisionText;
    private int decisionIndex;

    private void Awake()
    {
        decisionText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Sets the index and the text for the decision item.
    /// </summary>
    /// <param name="decisionIndex">The index of the decision.</param>
    /// <param name="decision">The text for the decision.</param>
    public void SetDecision(int decisionIndex, string decision)
    {
        this.decisionIndex = decisionIndex;
        decisionText.text = decision;
    }

    /// <summary>
    /// Sends the decision index to the dialogue manager.
    /// </summary>
    public void MakeDecision() => DialogueManager.Instance.MakeDecision(decisionIndex);
}
