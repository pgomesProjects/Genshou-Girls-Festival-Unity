using UnityEngine;

[System.Serializable]
public class ConfigData
{
    //Display Settings
    [Tooltip("If true, the game is fullscreened. Otherwise, the game is windowed.")] public bool isFullScreen;

    //Skip Settings
    [Tooltip("If true, text can be skipped before its been seen.")] public bool skipUnseenText;
    [Tooltip("If true, text will continue to skip after choices have been made.")] public bool skipAfterChoices;
    [Tooltip("If true, scene transitions will be skipped entirely.")] public bool skipTransitions;

    //Text Settings
    [Tooltip("The speed in which the text appears on the screen.")] public int textSpeed;
    [Tooltip("The speed in which text advances in auto mode after completion.")] public float autoForwardTime;

    //Audio Settings
    [Tooltip("The volume for music.")] public float musicVolume;
    [Tooltip("The volume for sound effects.")] public float soundVolume;
    [Tooltip("If true, all audio is muted.")] public bool muteAll;

    public ConfigData()
    {
        //Display Settings
        isFullScreen = true;

        //Skip Settings
        skipUnseenText = false;
        skipAfterChoices = false;
        skipTransitions = false;

        //Text Settings
        textSpeed = 40;
        autoForwardTime = 0.5f;

        //Audio Settings
        musicVolume = 0.5f;
        soundVolume = 0.5f;
        muteAll = false;
    }
}
