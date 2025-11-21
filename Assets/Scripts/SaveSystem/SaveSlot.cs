using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlot : MonoBehaviour
{
    public enum SaveType { Auto, Quick, Manual };

    [SerializeField, Tooltip("The raw image for the screenshot.")] private RawImage screenShot;
    [SerializeField, Tooltip("The text for the save information.")] private TextMeshProUGUI saveInfo;
    [SerializeField, Tooltip("The color of the text when disabled.")] private Color disabledColor;
    [SerializeField, Tooltip("The text to show when a slot is empty.")] private string emptySlotMessage;

    private Button saveButton;
    private Color defaultTextColor;

    private bool saveEnabled;

    private int slotIndex;
    private SaveType currentSaveType;
    private SaveMenuContainer.SaveAction currentSaveAction;
    private SaveData currentSaveData;

    private void Awake()
    {
        saveButton = GetComponent<Button>();
        defaultTextColor = saveInfo.color;
    }

    /// <summary>
    /// Assigns data to a save slot.
    /// </summary>
    /// <param name="saveType">The type of save to show.</param>
    /// <param name="saveAction">The action to perform when clicked.</param>
    /// <param name="saveData">The save data to display.</param>
    /// <param name="slotIndex">The index of the slot in the menu.</param>
    public void AssignSaveSlot(SaveType saveType, SaveMenuContainer.SaveAction saveAction, SaveData saveData, int slotIndex)
    {
        this.slotIndex = slotIndex;
        currentSaveType = saveType;
        currentSaveAction = saveAction;

        switch (saveType)
        {
            case SaveType.Auto:
                EnableSave(saveAction == SaveMenuContainer.SaveAction.Load);
                break;
            case SaveType.Quick:
                EnableSave(true);
                break;
            case SaveType.Manual:
                EnableSave(true);
                break;
        }

        currentSaveData = saveData;
        DisplaySaveInfo();
    }

    /// <summary>
    /// Enables the save button.
    /// </summary>
    /// <param name="isEnabled">True if the save is enabled. False if otherwise.</param>
    private void EnableSave(bool isEnabled)
    {
        saveEnabled = isEnabled;

        //If there is no save button assigned, return
        if (saveButton == null)
            return;

        //Update the button visuals
        saveInfo.color = saveEnabled ? defaultTextColor : disabledColor;
        saveButton.interactable = saveEnabled;
    }

    /// <summary>
    /// Lets the user use a save slot.
    /// </summary>
    public void UseSaveSlot()
    {
        //Load save
        if (currentSaveAction == SaveMenuContainer.SaveAction.Load)
            LoadSave();

        //Create save
        else if (currentSaveAction == SaveMenuContainer.SaveAction.Save)
            TrySave();
    }

    /// <summary>
    /// Loads the save in the save slot.
    /// </summary>
    private void LoadSave()
    {
        //If there is no save data, return
        if (currentSaveData == null)
            return;

        //Set the game data and load it in
        GameManager.currentGameData = currentSaveData.gameData;
        GameManager.Instance.LoadGameData();
    }

    /// <summary>
    /// Checks for a save overwrite before saving.
    /// </summary>
    private void TrySave()
    {
        if (currentSaveData != null)
        {
            string message = "Are you sure you want to overwrite your save?";
            GameManager.Instance.AddConfirmationWindow(FindFirstObjectByType<Canvas>().GetComponent<RectTransform>(), message, () => CreateSave());
        }
        else
            CreateSave();
    }

    /// <summary>
    /// Creates a save on the save slot.
    /// </summary>
    private void CreateSave()
    {
        currentSaveData = SaveData.SaveToFile(currentSaveType, slotIndex);

        //Show the new save
        DisplaySaveInfo();
    }

    /// <summary>
    /// Displays the save info stored in the current slot.
    /// </summary>
    public void DisplaySaveInfo()
    {
        if (currentSaveData == null)
        {
            //Remove the screenshot and the save info
            screenShot.color = Color.clear;
            saveInfo.text = emptySlotMessage;

            //If the save is enabled, check to see if the current action is saving or not
            if(saveEnabled)
                EnableSave(currentSaveAction == SaveMenuContainer.SaveAction.Save);
        }
        else
        {
            //Apply the screenshot to the raw image
            screenShot.color = Color.white;
            byte[] bytes = System.Convert.FromBase64String(currentSaveData.screenshotData);
            Texture2D screenshotTex = new Texture2D(2, 2);
            screenshotTex.LoadImage(bytes);
            screenshotTex.Apply();
            screenShot.texture = screenshotTex;

            //Show the timestamp
            saveInfo.text = currentSaveData.timeStamp;
        }
    }
}
