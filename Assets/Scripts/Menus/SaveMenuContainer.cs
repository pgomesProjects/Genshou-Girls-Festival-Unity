using UnityEngine;
using TMPro;
using System.IO;

public class SaveMenuContainer : MonoBehaviour
{
    public enum SaveAction { Load, Save };

    [SerializeField, Tooltip("The type of action for the save menu.")] private SaveAction currentAction;
    [SerializeField, Tooltip("The save slots in the container.")] private SaveSlot[] saveSlots;
    [SerializeField, Tooltip("The title for the page.")] private TextMeshProUGUI savePageTitle;

    private int currentPage;

    private void OnEnable()
    {
        //Start on the auto saves page when loading
        if(currentAction == SaveAction.Load)
        {
            currentPage = -1;
            GoToPage(currentPage);
        }
        //Start on the first manual saves page when saving
        else if(currentAction == SaveAction.Save)
        {
            currentPage = 1;
            GoToPage(currentPage);
        }
    }

    public void GoToPage(int page)
    {
        currentPage = Mathf.Max(-1, page);

        SaveSlot.SaveType currentSaveType;
        //If the index is -1, this is the auto saves page
        if(page == -1)
        {
            savePageTitle.text = "Automatic Saves";
            currentSaveType = SaveSlot.SaveType.Auto;
        }
        //If the index is 0, this is the quick saves page
        else if (page == 0)
        {
            savePageTitle.text = "Quick Saves";
            currentSaveType = SaveSlot.SaveType.Quick;
        }
        //Manual saves
        else
        {
            savePageTitle.text = "Page " + currentPage.ToString();
            currentSaveType = SaveSlot.SaveType.Manual;
        }

        for(int i = 0; i < saveSlots.Length; i++)
        {
            //Get the slot index for the save slot
            int slotIndex = i + 1;
            if (page > 0)
                slotIndex *= page;

            //Get the save data for the current slot
            SaveData currentSaveData = null;
            string filePath = SaveData.GetFileName(currentSaveType, slotIndex);
            if (File.Exists(filePath))
                currentSaveData = DataIO.LoadFile<SaveData>(filePath);

            //Assign the save slot data
            saveSlots[i].AssignSaveSlot(currentSaveType, currentAction, currentSaveData, slotIndex);
        }
    }

    public void IncrementPage(int increment)
    {
        //Increment the page number, ensuring not to go lower than -1
        currentPage += increment;
        currentPage = Mathf.Max(-1, currentPage);

        //Go to the save page
        GoToPage(currentPage);
    }
}
