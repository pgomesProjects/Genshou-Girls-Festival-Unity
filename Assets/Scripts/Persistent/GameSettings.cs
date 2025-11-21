using UnityEngine;

public class GameSettings : MonoBehaviour
{
    private static string persistentDataFileName = "/persistent.save";
    private static string configDataFileName = "/config.save";

    internal static PersistentData persistentData;
    internal static ConfigData configData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSettings()
    {
        //Load the persistent data
        persistentData = DataIO.LoadFile<PersistentData>(Application.persistentDataPath + persistentDataFileName);
        configData = DataIO.LoadFile<ConfigData>(Application.persistentDataPath + configDataFileName);

        //Apply fullscreen
        Screen.fullScreen = configData.isFullScreen;
    }

    /// <summary>
    /// Saves the persistent data.
    /// </summary>
    public static void SavePersistentData() => DataIO.SaveFile(Application.persistentDataPath + persistentDataFileName, persistentData);

    /// <summary>
    /// Saves the config settings.
    /// </summary>
    public static void SaveConfig() => DataIO.SaveFile(Application.persistentDataPath + configDataFileName, configData);
}
