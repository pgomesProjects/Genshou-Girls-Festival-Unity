using UnityEngine;

[System.Serializable]
public class SaveData
{
    public static string filePath = "/Saves/";

    public int slotIndex;
    public string timeStamp;
    public string screenshotData;
    public GameData gameData;

    /// <summary>
    /// Gets the file name for the save slot.
    /// </summary>
    /// <param name="saveType">The type of save slot.</param>
    /// <param name="slotIndex">The index for the save slot.</param>
    /// <returns>The full file path of the save slot.</returns>
    public static string GetFileName(SaveSlot.SaveType saveType, int slotIndex)
    {
        string fileName = string.Empty;
        if (saveType == SaveSlot.SaveType.Auto)
            fileName += "auto_";
        else if (saveType == SaveSlot.SaveType.Quick)
            fileName += "quick_";
        fileName += "save_" + slotIndex.ToString("D2");

        return Application.persistentDataPath + filePath + fileName + ".save";
    }

    /// <summary>
    /// Gets the current formatted timestamp.
    /// </summary>
    /// <returns>The timestamp as a saveable string.</returns>
    public static string GetTimeStamp() => System.DateTime.Now.ToString("dddd, MMMM dd yyyy, HH:mm");

    public static SaveData SaveToFile(SaveSlot.SaveType currentSaveType, int slotIndex)
    {
        //Add the most recent screenshot and current timestamp to the save
        SaveData currentSaveData = new SaveData();
        currentSaveData.screenshotData = GameManager.Instance.CurrentScreenshot;
        currentSaveData.timeStamp = GetTimeStamp();
        currentSaveData.gameData = GameManager.currentGameData;

        //Save the data to a file
        DataIO.SaveFile(GetFileName(currentSaveType, slotIndex), currentSaveData);

        //Return the data
        return currentSaveData;
    }
}
