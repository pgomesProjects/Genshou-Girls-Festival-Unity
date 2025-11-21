using System.IO;
using UnityEngine;

public static class DataIO
{
    /// <summary>
    /// Loads data from a Json file at the given path. If not created or invalid, it returns default data.
    /// </summary>
    /// <typeparam name="T">The type of class to read from the file.</typeparam>
    /// <param name="filePath">The path for the file to read.</param>
    /// <returns>The data from the class given from the file.</returns>
    public static T LoadFile<T>(string filePath) where T : new()
    {
        //If the directory does not exist, create it
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        //If the file does not exist, create it
        if (!File.Exists(filePath))
        {
            //Create a file with the default values
            T defaultData = new T();
            File.WriteAllText(filePath, JsonUtility.ToJson(defaultData));
            Debug.Log(typeof(T).Name + " File created at " + filePath);
            return defaultData;
        }

        //Deserialize and read the json file
        T data = JsonUtility.FromJson<T>(File.ReadAllText(filePath));

        //If the data cannot be read, reset to default
        if(data == null)
        {
            data = new T();
            File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
            Debug.LogWarning(typeof(T).Name + " File was invalid. Resetting to default.");
        }

        //Return the data
        Debug.Log(typeof(T).Name + " File loaded successfully.");
        return data;
    }

    /// <summary>
    /// Saves data to a Json file at the given path.
    /// </summary>
    /// <typeparam name="T">The type of class to read from the file.</typeparam>
    /// <param name="filePath">The path for the file to read.</param>
    /// <param name="data">The data to save to the file.</param>
    /// <param name="revertToDefault">If true, the data will be overwritten to default.</param>
    public static void SaveFile<T>(string filePath, T data, bool revertToDefault = false) where T : new()
    {
        //If the directory does not exist, create it
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        //If reverting to default, overwrite with default data
        if (revertToDefault)
            data = new T();

        //Saves the file to the appropriate path
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
        Debug.Log(typeof(T).Name + " File saved successfully at " + filePath);
    }
}
