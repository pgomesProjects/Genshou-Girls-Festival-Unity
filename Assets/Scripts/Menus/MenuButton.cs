using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [SerializeField, Tooltip("The name of the sound when the user hovers the button.")] private string onHoverSFX;
    [SerializeField, Tooltip("The name of the sound when the user selects the button.")] private string onSelectSFX;

    public bool IsHighlighted { get; private set; }
    private Button buttonComponent;

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
    }

    private void OnEnable()
    {
        buttonComponent.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        buttonComponent.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Called when the user hovers over the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnButtonHovered();
        IsHighlighted = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHighlighted = false;
    }

    /// <summary>
    /// Called when the user selects the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSelect(BaseEventData eventData)
    {
        OnButtonHovered();
    }

    private void OnButtonHovered()
    {
        if(!string.IsNullOrEmpty(onHoverSFX))
            GameManager.Instance.AudioManager.PlaySFX(onHoverSFX, audioType: AudioType.UI);
    }

    private void OnButtonClicked()
    {
        if (!string.IsNullOrEmpty(onSelectSFX))
            GameManager.Instance.AudioManager.PlaySFX(onSelectSFX, audioType: AudioType.UI);
    }
}
