using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip audioClip;
}

public enum AudioType { Music, SFX, UI, Voice };

public class AudioManager : MonoBehaviour
{
    [SerializeField, Tooltip("The master audio mixer.")] private AudioMixerGroup masterMixer;
    [SerializeField, Tooltip("The music audio mixer.")] private AudioMixerGroup musicMixer;
    [SerializeField, Tooltip("The sound effects audio mixer.")] private AudioMixerGroup sfxMixer;
    [Space()]
    [SerializeField, Tooltip("The master list of music.")] private Sound[] music;
    [SerializeField, Tooltip("The master list of sound effects.")] private Sound[] sounds;
    [SerializeField, Tooltip("The master list of UI sounds.")] private Sound[] ui;
    [Space()]
    [SerializeField, Tooltip("The template GameObject for the AudioSources.")] private GameObject template;
    [SerializeField, Tooltip("The initial size of the audio pool (used to spawn AudioSources for reusability).")] private int poolSize = 10;

    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeSources = new List<AudioSource>();

    private AudioSource musicAudioSource;

    private void Awake()
    {
        CreateMusicAudioSource();
        CreateAudioPool();
    }

    private void Start()
    {
        RefreshMixerVolumes();
    }

    #region MUSIC
    /// <summary>
    /// Creates a music audio source.
    /// </summary>
    private void CreateMusicAudioSource()
    {
        GameObject musicObject = Instantiate(template, transform);
        musicObject.name = "MusicSource";
        musicAudioSource = musicObject.AddComponent<AudioSource>();
        musicAudioSource.loop = true;
        musicAudioSource.outputAudioMixerGroup = musicMixer;
    }

    /// <summary>
    /// Plays audio on the music audio source.
    /// </summary>
    /// <param name="musicName">The name of the music sound.</param>
    /// <param name="volume">The volume of the music.</param>
    public void PlayMusic(string musicName)
    {
        Sound currentMusic = FindMusic(musicName);
        if (currentMusic == null)
        {
            Debug.LogWarning("Music '" + musicName + "' could not be found.");
            return;
        }

        if (currentMusic.audioClip == null)
        {
            Debug.LogWarning("AudioClip for '" + musicName + "' could not be found.");
            return;
        }

        musicAudioSource.clip = currentMusic.audioClip;
        musicAudioSource.Play();
    }

    /// <summary>
    /// Stops the current music from playing.
    /// </summary>
    public void StopMusic()
    {
        musicAudioSource.Stop();
        musicAudioSource.clip = null;
    }

    #endregion

    #region SFX
    /// <summary>
    /// Creates an audio pool for the AudioManager to utilize.
    /// </summary>
    private void CreateAudioPool()
    {
        //Preload audio sources
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = CreateAudioSource();
            audioSourcePool.Enqueue(source);
        }
    }

    /// <summary>
    /// Creates an AudioSource component.
    /// </summary>
    /// <returns>A new AudioSource component on a GameObject.</returns>
    private AudioSource CreateAudioSource()
    {
        //Create an object and child it to the AudioManager
        GameObject audioObject = Instantiate(template, transform);
        audioObject.name = "AudioSource";
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixer;

        //Hide the object and return the AudioSource component
        audioObject.SetActive(false);
        return source;
    }

    /// <summary>
    /// Gets the audio source from the pool.
    /// </summary>
    /// <param name="soundName">The name of the sound, for organizational purposes.</param>
    /// <returns>An AudioSource component from the queue, or a newly created one.</returns>
    private AudioSource GetAudioSource(string soundName = "AudioSource")
    {
        AudioSource source;

        //If there are audio sources in the pool, dequeue one from the front of the queue. Otherwise, create a new one
        if (audioSourcePool.Count > 0)
        {
            source = audioSourcePool.Dequeue();

            //If the source is null, create a new one
            if (source == null)
                source = CreateAudioSource();
        }
        else
            source = CreateAudioSource();

        //Set the audio source active
        source.gameObject.SetActive(true);
        source.gameObject.name = soundName;
        activeSources.Add(source);
        return source;
    }

    /// <summary>
    /// Returns an audio source to the pool.
    /// </summary>
    /// <param name="audioSource">The audio source to return.</param>
    private void ReturnAudioSource(AudioSource audioSource)
    {
        //Stop the sound and return its values to default
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.transform.position = Vector3.zero;
        audioSource.spatialBlend = 0f;
        audioSource.gameObject.name = "AudioSource";

        //Hide the audio source, remove it from the list, and add it to the queue to play later
        audioSource.gameObject.SetActive(false);
        activeSources.Remove(audioSource);
        audioSourcePool.Enqueue(audioSource);
    }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    /// <param name="soundName">The name of the sound.</param>
    /// <param name="volume">The volume of the sound.</param>
    /// <param name="pitch">The pitch of the sound.</param>
    /// <param name="onComplete">Any function subscribed when the sound effect is complete.</param>
    public void PlaySFX(string soundName, float volume = 1f, float pitch = 1f, System.Action onComplete = null, AudioType audioType = AudioType.SFX)
    {
        //Look for the sound based on the type of audio it is
        Sound currentSound;
        switch (audioType)
        {
            case AudioType.UI:
                currentSound = FindUISound(soundName);
                break;
            default:
                currentSound = FindSound(soundName);
                break;
        }

        if (currentSound == null)
        {
            Debug.LogWarning("Sound '" + soundName + "' could not be found.");
            return;
        }

        if (currentSound.audioClip == null)
        {
            Debug.LogWarning("AudioClip for '" + soundName + "' could not be found.");
            return;
        }

        //Create an AudioSource and play the sound
        AudioSource source = GetAudioSource(soundName);
        if (source == null)
        {
            Debug.LogWarning("AudioSource for '" + soundName + "' is null.");
            return;
        }

        source.clip = currentSound.audioClip;
        source.volume = volume;
        source.outputAudioMixerGroup = sfxMixer;

        //If there is an audio mixer, allow for pitch shift
        if (sfxMixer != null)
            source.outputAudioMixerGroup.audioMixer.SetFloat("SFX_Pitch", pitch);

        source.Play();

        //Return the audio source after it has finished playing
        StartCoroutine(ReturnAudioOnFinish(source, currentSound.audioClip.length, onComplete));
    }

    /// <summary>
    /// Plays a sound effect at a specific location in world space.
    /// </summary>
    /// <param name="soundName">The name of the sound.</param>
    /// <param name="position">The position of the audio in world space.</param>
    /// <param name="volume">The volume of the sound.</param>
    /// <param name="pitch">The pitch of the sound.</param>
    /// <param name="attenuationMin">The minimum distance from the player where the sound's volume will be at its highest.</param>
    /// <param name="attenuationMax">The maximum distance from the player where the sound's volume will become inaudible.</param>
    /// <param name="spatialBlend">The spatial blend of the sound (0 = fully 2D, 1 = fully 3D).</param>
    /// <param name="onComplete">Any function subscribed when the sound effect is complete.</param>
    public void PlaySFXAtLocation(string soundName, Vector3 position, float volume = 1f, float pitch = 1f, float attenuationMin = 1f, float attenuationMax = 25f, float spatialBlend = 1f, System.Action onComplete = null)
    {
        Sound currentSound = FindSound(soundName);
        if (currentSound == null)
        {
            Debug.LogWarning("Sound '" + soundName + "' could not be found.");
            return;
        }

        if (currentSound.audioClip == null)
        {
            Debug.LogWarning("AudioClip for '" + soundName + "' could not be found.");
            return;
        }

        //Create an AudioSource and play the sound
        AudioSource source = GetAudioSource(soundName);
        if (source == null)
        {
            Debug.LogWarning("AudioSource for '" + soundName + "' is null.");
            return;
        }

        //Create a GameObject at the position that it will be played
        GameObject positionedSFX = Instantiate(template);
        positionedSFX.name = soundName;

        positionedSFX.transform.position = transform.InverseTransformPoint(position);
        source.transform.position = positionedSFX.transform.position;

        source.clip = currentSound.audioClip;
        source.volume = volume;
        source.outputAudioMixerGroup = sfxMixer;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.minDistance = attenuationMin;
        source.maxDistance = attenuationMax;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        //If there is an audio mixer, allow for pitch shift
        if (sfxMixer != null)
            source.outputAudioMixerGroup.audioMixer.SetFloat("SFX_Pitch", pitch);

        //Return the audio source after it has finished playing
        StartCoroutine(ReturnAudioOnFinish(source, currentSound.audioClip.length, onComplete));
        Destroy(positionedSFX, currentSound.audioClip.length);
    }

    /// <summary>
    /// Returns the audio source to the pool.
    /// </summary>
    /// <param name="audioSource">The audio source to return.</param>
    /// <param name="delay">The delay in seconds beore the audio source is returned.</param>
    /// <param name="onComplete">Any function subscribed when the sound effect is complete.</param>
    private IEnumerator ReturnAudioOnFinish(AudioSource audioSource, float delay, System.Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        ReturnAudioSource(audioSource);
        onComplete?.Invoke();
    }
    #endregion

    /// <summary>
    /// Refreshes the mixer volumes.
    /// </summary>
    public void RefreshMixerVolumes()
    {
        float musicVolume = GameSettings.configData.muteAll ? 0f : GameSettings.configData.musicVolume;
        float sfxVolume = GameSettings.configData.muteAll ? 0f : GameSettings.configData.soundVolume;

        //If the volume is 0, set the value to nothing
        float musicVolumeValue = -80f;
        if (musicVolume > 0f)
            musicVolumeValue = Mathf.Log10(musicVolume) * 20; //Set the volume logarithmically because decibels are exponential

        //If the volume is 0, set the value to nothing
        float sfxVolumeValue = -80f;
        if (sfxVolume > 0f)
            sfxVolumeValue = Mathf.Log10(sfxVolume) * 20; //Set the volume logarithmically because decibels are exponential

        //Set the parameters in the audio mixers
        musicMixer.audioMixer.SetFloat("MusicVolume", musicVolumeValue);
        sfxMixer.audioMixer.SetFloat("SFXVolume", sfxVolumeValue);
    }

    /// <summary>
    /// Pauses all active sounds.
    /// </summary>
    public void PauseAllSounds()
    {
        //Pause all playing sounds in the active sources list
        foreach (AudioSource source in activeSources)
            if (source.isPlaying)
                source.Pause();

        //If the music is playing, pause it
        if (musicAudioSource.isPlaying)
            musicAudioSource.Pause();
    }

    /// <summary>
    /// Resumes all paused sounds.
    /// </summary>
    public void ResumeAllSounds()
    {
        //Resume all paused sounds in the active sources list
        foreach (AudioSource source in activeSources)
            if (!source.isPlaying)
                source.UnPause();

        //If the music is not playing and there is music in the audio source, resume it
        if (!musicAudioSource.isPlaying && musicAudioSource.clip != null)
            musicAudioSource.UnPause();
    }

    /// <summary>
    /// Stops all sounds.
    /// </summary>
    public void StopAllSounds()
    {
        //Stop all sounds in the active sources list
        foreach (AudioSource source in activeSources)
        {
            if (source != null)
                source.Stop();
        }

        StopMusic();
    }

    /// <summary>
    /// Finds music in the master list.
    /// </summary>
    /// <param name="name">The name of the music.</param>
    /// <returns>Returns the sound object found in the master list. Returns null if not found.</returns>
    private Sound FindMusic(string name)
    {
        //Search the master list based on the name
        foreach (Sound sound in music)
            if (sound.name == name)
                return sound;

        return null;
    }

    /// <summary>
    /// Finds a sound in the master list.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <returns>Returns the sound object found in the master list. Returns null if not found.</returns>
    private Sound FindSound(string name)
    {
        //Search the master list based on the name
        foreach (Sound sound in sounds)
            if (sound.name == name)
                return sound;

        return null;
    }


    /// <summary>
    /// Finds a UI sound in the master list.
    /// </summary>
    /// <param name="name">The name of the UI sound.</param>
    /// <returns>Returns the sound object found in the master list. Returns null if not found.</returns>
    private Sound FindUISound(string name)
    {
        //Search the master list based on the name
        foreach (Sound sound in ui)
            if (sound.name == name)
                return sound;

        return null;
    }
}
