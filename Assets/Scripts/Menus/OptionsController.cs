using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [SerializeField, Tooltip("The window display toggle.")] private Toggle windowToggle;
    [SerializeField, Tooltip("The fullscreen display toggle.")] private Toggle fullscreenToggle;

    [SerializeField, Tooltip("The skip unseen text toggle.")] private Toggle unseenTextToggle;
    [SerializeField, Tooltip("The skip after choices toggle.")] private Toggle afterChoicesToggle;
    [SerializeField, Tooltip("The skip transitions toggle.")] private Toggle transitionsToggle;

    [SerializeField, Tooltip("The slider for the text speed.")] private Slider textSpeedSlider;
    [SerializeField, Tooltip("The slider for the auto forward time.")] private Slider autoForwardTime;

    [SerializeField, Tooltip("The slider for the music.")] private Slider musicSlider;
    [SerializeField, Tooltip("The slider for the sound effects.")] private Slider sfxSlider;
    [SerializeField, Tooltip("The mute all toggle.")] private Toggle muteAllToggle;

    private void OnEnable()
    {
        SetInitialValues();
    }

    /// <summary>
    /// Sets the initial values for the options components.
    /// </summary>
    private void SetInitialValues()
    {
        //Fullscreen
        windowToggle.SetIsOnWithoutNotify(!GameSettings.configData.isFullScreen);
        windowToggle.GetComponent<ToggleButton>().SetToggle(!GameSettings.configData.isFullScreen);
        fullscreenToggle.SetIsOnWithoutNotify(GameSettings.configData.isFullScreen);
        fullscreenToggle.GetComponent<ToggleButton>().SetToggle(GameSettings.configData.isFullScreen);

        //Skip Settings
        unseenTextToggle.SetIsOnWithoutNotify(GameSettings.configData.skipUnseenText);
        afterChoicesToggle.SetIsOnWithoutNotify(GameSettings.configData.skipAfterChoices);
        transitionsToggle.SetIsOnWithoutNotify(GameSettings.configData.skipTransitions);

        //Text Settings
        textSpeedSlider.SetValueWithoutNotify(GameSettings.configData.textSpeed);
        autoForwardTime.SetValueWithoutNotify(GameSettings.configData.autoForwardTime);

        //Audio Settings
        musicSlider.SetValueWithoutNotify(GameSettings.configData.muteAll ? 0 : Mathf.CeilToInt(GameSettings.configData.musicVolume * 10f));
        sfxSlider.SetValueWithoutNotify(GameSettings.configData.muteAll ? 0 : Mathf.CeilToInt(GameSettings.configData.soundVolume * 10f));
        muteAllToggle.SetIsOnWithoutNotify(GameSettings.configData.muteAll);
    }

    public void SetFullscreen(bool isOn)
    {
        GameSettings.configData.isFullScreen = isOn;
        GameSettings.SaveConfig();

        //Make sure only one toggle is on at all times
        windowToggle.SetIsOnWithoutNotify(!GameSettings.configData.isFullScreen);
        windowToggle.GetComponent<ToggleButton>().SetToggle(!isOn);
        fullscreenToggle.SetIsOnWithoutNotify(GameSettings.configData.isFullScreen);
        fullscreenToggle.GetComponent<ToggleButton>().SetToggle(isOn);

        //Apply the settings immediately
        Screen.fullScreen = GameSettings.configData.isFullScreen;
    }

    public void SetUnseenText(bool isOn)
    {
        GameSettings.configData.skipUnseenText = isOn;
        GameSettings.SaveConfig();
    }

    public void SetAfterChoices(bool isOn)
    {
        GameSettings.configData.skipAfterChoices = isOn;
        GameSettings.SaveConfig();
    }

    public void SetTransitions(bool isOn)
    {
        GameSettings.configData.skipTransitions = isOn;
        GameSettings.SaveConfig();
    }

    public void SetTextSpeed(float textSpeed)
    {
        GameSettings.configData.textSpeed = Mathf.CeilToInt(textSpeed);
        GameSettings.SaveConfig();
    }

    public void SetAutoForward(float autoForward)
    {
        GameSettings.configData.autoForwardTime = autoForward / autoForwardTime.maxValue;
        GameSettings.SaveConfig();
    }

    public void SetMusic(float musicVol)
    {
        GameSettings.configData.musicVolume = musicVol / 10f;
        //If the audio is currently all muted, set the values and turn of the mute all toggle
        if (GameSettings.configData.muteAll)
        {
            GameSettings.configData.soundVolume = sfxSlider.value / 10f;
            muteAllToggle.isOn = false;
        }
        else
        {
            //Save the config file and refresh the volumes
            GameSettings.SaveConfig();
            GameManager.Instance.AudioManager.RefreshMixerVolumes();
        }
    }

    public void SetSFX(float sfxVol)
    {
        GameSettings.configData.soundVolume = sfxVol / 10f;

        //If the audio is currently all muted, set the values and turn of the mute all toggle
        if (GameSettings.configData.muteAll)
        {
            GameSettings.configData.musicVolume = musicSlider.value / 10f;
            muteAllToggle.isOn = false;
        }
        else
        {
            //Save the config file and refresh the volumes
            GameSettings.SaveConfig();
            GameManager.Instance.AudioManager.RefreshMixerVolumes();
        }
    }

    public void SetMuteAll(bool isOn)
    {
        GameSettings.configData.muteAll = isOn;

        //If all audio is muted, set the sliders to 0
        if (isOn)
        {
            musicSlider.SetValueWithoutNotify(0f);
            sfxSlider.SetValueWithoutNotify(0f);
        }
        //If the audio is not muted, reset their positions
        else
        {
            musicSlider.SetValueWithoutNotify(Mathf.CeilToInt(GameSettings.configData.musicVolume * 10f));
            sfxSlider.SetValueWithoutNotify(Mathf.CeilToInt(GameSettings.configData.soundVolume * 10f));
        }

        //Save the config file and refresh the volumes
        GameSettings.SaveConfig();
        GameManager.Instance.AudioManager.RefreshMixerVolumes();
    }
}
