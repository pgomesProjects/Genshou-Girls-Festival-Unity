using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField, Tooltip("The prefab for confirmation windows.")] private ConfirmationWindow confirmationWindowPrefab;
    [SerializeField, Tooltip("The list of compendium entries.")] private CompendiumEntry[] compendiumEntries;

    internal bool InGameMenu;
    internal string CurrentScreenshot;
    internal bool loadingSaveSlot;

    public AudioManager AudioManager { get; private set; }

    public CompendiumEntry[] CompendiumEntryList => compendiumEntries;
    public static GameData currentGameData = new GameData();


    private void Awake()
    {
        //Singleton-ize script
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        AudioManager = GetComponentInChildren<AudioManager>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        switch (scene.name)
        {
            case "Title":
                //Reset the dialogue values
                DialogueManager.playbackState = DialogueManager.PlaybackState.Normal;
                DialogueManager.DialogueSpeedMultiplier = 1f;
                currentGameData = new GameData();
                break;
        }
    }

    public void AddToTranscript(Transcript currentLine)
    {
        currentGameData.transcript.Add(currentLine);
    }

    /// <summary>
    /// Gets the transcript.
    /// </summary>
    /// <param name="maxLines">The maximum amount of lines to show in the transcript.</param>
    /// <returns>A string containing the most recent section of the transcript.</returns>
    public string GetTranscript(int maxLines)
    {
        string transcriptText = string.Empty;

        int startingIndex = Mathf.Max(0, currentGameData.transcript.Count - maxLines - 1);

        //Display all lines except for the most recent
        for(int i = startingIndex; i < currentGameData.transcript.Count - 1; i++)
        {
            //Display the text differently based on whether there is a speaker or not
            if (string.IsNullOrEmpty(currentGameData.transcript[i].speaker))
                transcriptText += currentGameData.transcript[i].text + "\n";
            else
                transcriptText += currentGameData.transcript[i].speaker + ": " + currentGameData.transcript[i].text + "\n";

            //For all lines except the last one displayed, add an extra line break
            if (i < currentGameData.transcript.Count - 2)
                transcriptText += "\n";
        }

        //If the transcript is empty, display that information
        if (string.IsNullOrEmpty(transcriptText))
            transcriptText = "No Transcript Found.";

        return transcriptText;
    }

    /// <summary>
    /// Clears the transcript.
    /// </summary>
    public void ClearTranscript()
    {
        currentGameData.transcript.Clear();
    }

    /// <summary>
    /// Takes a screenshot and converts it into Base64 data to save.
    /// </summary>
    public IEnumerator ScreenshotGame()
    {
        yield return new WaitForEndOfFrame();

        //Get a screenshot and save it as Base64 data
        Texture2D screenshotTexture = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] imageBytes = screenshotTexture.EncodeToPNG();
        CurrentScreenshot = System.Convert.ToBase64String(imageBytes);
    }

    /// <summary>
    /// Creates a confirmation window.
    /// </summary>
    /// <param name="parent">The parent transform for the message.</param>
    /// <param name="message">The message for the window.</param>
    /// <param name="onConfirmed">The action to invoke when confirmed.</param>
    public ConfirmationWindow AddConfirmationWindow(RectTransform parent, string message, System.Action onConfirmed)
    {
        //Creates a window and initializes the contents
        ConfirmationWindow currentWindow = Instantiate(confirmationWindowPrefab, parent);
        currentWindow.Initialize(message, onConfirmed);

        //Move the window to the front of the sibling index
        currentWindow.transform.SetAsLastSibling();
        return currentWindow;
    }

    /// <summary>
    /// Loads the current game data's most recent node.
    /// </summary>
    public void LoadGameData()
    {
        loadingSaveSlot = true;
        Time.timeScale = 1f;
        LoadScene(currentGameData.GetLatestNode().sceneName);
    }

    /// <summary>
    /// Loads a scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene in the build settings.</param>
    public void LoadScene(string sceneName)
    {
        AudioManager.StopMusic();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    /// <returns>A string with the scene name.</returns>
    public string GetSceneName() => SceneManager.GetActiveScene().name;

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
