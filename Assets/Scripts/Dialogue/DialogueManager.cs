using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public static readonly string DialogueFolder = "/Dialogue/";
    public static readonly string CGFolder = "CGs/";
    public static readonly string MusicFolder = "Audio/BGM/";
    public static readonly string SFXFolder = "Audio/SFX/";

    [SerializeField, Tooltip("The node in the file to start with.")] private string startingNode;
    [Space()]
    [SerializeField, Tooltip("The color of dialogue text.")] private Color dialogueColor;
    [SerializeField, Tooltip("The color of internalized dialogue text.")] private Color internalizedDialogueColor;
    [SerializeField, Tooltip("The color of emphasized text.")] private Color emphasizedColor;
    [SerializeField, Tooltip("The name box.")] private RectTransform nameBox;
    [SerializeField, Tooltip("The text box.")] private RectTransform textBox;
    [SerializeField, Tooltip("The control bar with the user controls.")] private DialogueControlBar controlBar;
    [SerializeField, Tooltip("The text for the text writer.")] private TextMeshProUGUI dialogueText;
    [SerializeField, Tooltip("The image for the CGs.")] private Image CGImage;
    [SerializeField, Tooltip("The auto text indicator.")] private RectTransform autoIndicator;
    [SerializeField, Tooltip("The continue icon.")] private RectTransform continueIcon;
    [SerializeField, Tooltip("The amount of time it takes to read one character (in seconds).")] private float timePerCharacter = 0.1f;
    [SerializeField, Tooltip("The amount of time spent on text during fast-forward (in seconds).")] private float fastForwardTime = 0.25f;
    [Space()]
    [SerializeField, Tooltip("The RectTransform of the protag camera view.")] private RectTransform protagCamRectTransform;
    [SerializeField, Tooltip("The position of the protag camera when active.")] private Vector2 protagCamActivePos;
    [SerializeField, Tooltip("The position of the protag camera when hidden.")] private Vector2 protagCamInactivePos;
    [SerializeField, Tooltip("The speed in which the protag camera view moves.")] private float protagCamSpeed;
    [Space()]
    [SerializeField, Tooltip("The decision manager.")] private DecisionManager decisionManager;

    private CanvasGroup CGCanvasGroup;
    
    private NodeData currentNode;
    
    private List<DecisionData> decisionDataQueue;
    private List<DecisionData.Decision> decisionQueue;

    private List<ConditionalData> conditionalDataQueue;
    private List<Conditional> currentConditionalQueue;

    private List<Dialogue> currentDialogue;
    private int currentLine;
    private string currentSpeaker;
    private Coroutine textSequence;

    private bool autoTimerActive;
    private float autoTimerElapsed;
    private float timeToReadText;

    private float fastForwardTimerElapsed;

    private bool decisionActive;

    private TextWriter.TextWriterInstance textWriter;
    private PlayerControls playerControls;

    internal static PlaybackState playbackState = PlaybackState.Normal;
    internal static float DialogueSpeedMultiplier = 1f;
    public enum PlaybackState { Normal, Auto, FastForward };

    private void Awake()
    {
        //Singleton-ize the script
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        playerControls = new PlayerControls();
        playerControls.Player.AdvanceText.started += _ => ReadPlayerInput();

        decisionDataQueue = new List<DecisionData>();
        decisionQueue = new List<DecisionData.Decision>();

        conditionalDataQueue = new List<ConditionalData>();
        currentConditionalQueue = new List<Conditional>();

        CGCanvasGroup = CGImage.GetComponent<CanvasGroup>();
        CGCanvasGroup.alpha = 0f;
        RefreshAutoIndicator();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        playerControls?.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        //Run the node saved in the current data
        if (GameManager.Instance.loadingSaveSlot)
        {
            LoadNode(GameManager.currentGameData.GetLatestNode().name, GameManager.currentGameData.currentLine);
            GameManager.Instance.loadingSaveSlot = false;
        }
        //Run the starting node in the scene
        else
            RunStartingNode();
    }

    private void RunStartingNode()
    {
        //Clear the command history
        GameManager.currentGameData.commandHistory.Clear();

        FindNode(startingNode);

        //Create an auto save
        StartCoroutine(CreateAutoSave());
    }

    public void FindNode(string nodeName)
    {
        //If the node is marked as complete, ignore it
        if (GameManager.currentGameData.completedNodes.Contains(nodeName))
            return;

        NodeData newNode = GetNodeData(nodeName);
        currentDialogue = GetDialogueLines(newNode);

        //If there is no dialogue found, return
        if (currentDialogue.Count == 0)
        {
            Debug.LogWarning("No Dialogue Found.");
            return;
        }

        GoToNode(nodeName, newNode);

        //Show the next line
        ShowCurrentLine();
    }

    public void LoadNode(string nodeName, int line)
    {
        //Get the node data from the game data
        NodeData newNode = GetNodeData(nodeName);
        currentDialogue = GetDialogueLines(newNode);

        //Set the data for the current playthrough
        decisionDataQueue = GameManager.currentGameData.decisionDataQueue;
        decisionQueue = GameManager.currentGameData.decisionQueue;
        conditionalDataQueue = GameManager.currentGameData.conditionalDataQueue;
        currentConditionalQueue = GameManager.currentGameData.currentConditionalQueue;
        Variable.SetDictionary(GameManager.currentGameData.variableDictionary.ToDictionary());

        //Run through every command in the queue
        for (int i = 0; i < GameManager.currentGameData.commandHistory.Count; i++)
            RunCommand(GameManager.currentGameData.commandHistory[i]);

        //Go to the node and skip to the line indicated
        GoToNode(nodeName, newNode);
        currentLine = line;
        ShowCurrentLine();
    }

    private void GoToNode(string nodeName, NodeData nodeData)
    {
        //Store the next line if currently in a node
        if (currentNode != null)
            currentNode.line = currentLine;

        //Get the node information if the node already has information
        NodeData currentData = GameManager.currentGameData.GetLatestNode();
        if (currentData != null && currentData.name == nodeName)
            currentNode = currentData;
        else
        {
            //Add the node to the game history if it's new
            currentNode = nodeData;
            GameManager.currentGameData.nodeHistory.Add(currentNode);
        }

        //Set the line
        currentLine = currentNode.line;
    }

    private NodeData GetNodeData(string nodeName)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Application.streamingAssetsPath + DialogueFolder);
        FileInfo[] allDialogue = directoryInfo.GetFiles("*.dlg", SearchOption.AllDirectories);

        //Store the pattern for variable names (var name = value)
        Regex varPattern = new(@"^var\s+(\w+)\s*=\s*(.+)$");

        //Look through all files
        foreach (var file in allDialogue)
        {
            string textInfo = File.ReadAllText(file.FullName);
            var lines = textInfo.Split("\n");

            bool nodeFound = false;

            NodeData nodeData = new NodeData(nodeName, GameManager.Instance.GetSceneName());

            //Create a queue for gathering decision data
            List<DecisionData> tempDecisionQueue = new List<DecisionData>();

            //Create a queue for gathering conditional data
            List<ConditionalData> tempConditionalQueue = new List<ConditionalData>();

            foreach (var rawLine in lines)
            {
                //Get the line data without any whitespace or comments
                var line = RemoveCommentsFromLine(rawLine.Trim());

                //If there is nothing on this line, continue
                if (string.IsNullOrEmpty(line))
                    continue;

                //If the current line has the correct variable syntax, store a variable in the appropriate dictionary
                Match varPatternMatch = varPattern.Match(line);
                if (varPatternMatch.Success)
                {
                    Variable.SetVariable(varPatternMatch.Groups[1].Value, Variable.ParseValue(varPatternMatch.Groups[2].Value));
                    continue;
                }

                //If this line starts a node
                if (line.Contains("#Node:"))
                {
                    //If the name of the node is found, mark the node is round
                    if (line.Substring(6).Trim() == nodeName)
                    {
                        nodeFound = true;
                        continue;
                    }
                }

                //If this line starts a decision, start looking for decision paths
                else if (nodeFound && line.Contains("#StartDecision"))
                {
                    tempDecisionQueue.Add(new DecisionData(nodeData.lines.Count));
                    continue;
                }

                //If a line starts with "-", it notes that this is a decision
                else if (tempDecisionQueue.Count > 0 && line.StartsWith("-"))
                {
                    DecisionData currentData = tempDecisionQueue[tempDecisionQueue.Count - 1];

                    //If there is a previous decision, save the end line
                    if (currentData.decisions.Count > 0)
                        currentData.decisions[currentData.decisions.Count - 1].endLine = nodeData.lines.Count;

                    //Create a new decision
                    currentData.decisions.Add(new DecisionData.Decision(nodeData.lines.Count, line[1..]));
                    continue;
                }

                //If this line ends a decision, stop looking for decisions
                else if (nodeFound && line.Contains("#EndDecision"))
                {
                    DecisionData currentData = tempDecisionQueue[tempDecisionQueue.Count - 1];
                    //If there is a previous decision, save the end line
                    if (currentData.decisions.Count > 0)
                        currentData.decisions[currentData.decisions.Count - 1].endLine = nodeData.lines.Count;

                    currentData.endLine = nodeData.lines.Count;
                    nodeData.decisionData.Add(currentData);

                    //Clear current decision data
                    tempDecisionQueue.RemoveAt(tempDecisionQueue.Count - 1);

                    continue;
                }

                //If this line starts an if statement, start checking for conditionals
                else if(nodeFound && line.StartsWith("if"))
                {
                    //Store this line as an if statement
                    tempConditionalQueue.Add(new ConditionalData(new Conditional(ExtractConditionalStatement(line), nodeData.lines.Count)));
                    continue;
                }

                //Else if, check if attached to an if statement
                else if (nodeFound && line.StartsWith("else if"))
                {
                    if(tempConditionalQueue.Count > 0)
                    {
                        //Store the previous line as the end line for the if statement
                        tempConditionalQueue[tempConditionalQueue.Count - 1].ifStatement.endLine = nodeData.lines.Count - 1;
                        //Store this line as an else if statement for the most recent if statement in the queue
                        tempConditionalQueue[tempConditionalQueue.Count - 1].elseIfStatements.Add(new Conditional(ExtractConditionalStatement(line), nodeData.lines.Count));
                        continue;
                    }
                }

                //Else, check if attached to an if statement
                else if (nodeFound && line.StartsWith("else"))
                {
                    if(tempConditionalQueue.Count > 0)
                    {
                        ConditionalData tempConditionalData = tempConditionalQueue[tempConditionalQueue.Count - 1];

                        //If the if statement hasn't been closed, close it with the previous line
                        if (tempConditionalData.ifStatement.startLine == tempConditionalData.ifStatement.endLine)
                            tempConditionalData.ifStatement.endLine = nodeData.lines.Count - 1;

                        //If there is an unclosed else if statement, close it with the previous line
                        if (tempConditionalData.elseIfStatements.Count > 0)
                            if (tempConditionalData.elseIfStatements[tempConditionalData.elseIfStatements.Count - 1].startLine == tempConditionalData.elseIfStatements[tempConditionalData.elseIfStatements.Count - 1].endLine)
                                tempConditionalData.elseStatement.endLine = nodeData.lines.Count - 1;

                        //Add the else statement data
                        tempConditionalQueue[tempConditionalQueue.Count - 1].elseStatement = new Conditional(nodeData.lines.Count);
                        continue;
                    }
                }

                //End this line ends an if statement, stop checking for the top most if statement
                else if (nodeFound && line.Contains("endif"))
                {
                    ConditionalData tempConditionalData = tempConditionalQueue[tempConditionalQueue.Count - 1];

                    //If the if statement hasn't been closed, close it with the previous line
                    if (tempConditionalData.ifStatement.startLine == tempConditionalData.ifStatement.endLine)
                        tempConditionalData.ifStatement.endLine = nodeData.lines.Count - 1;

                    //If there is an unclosed else if statement, close it with the previous line
                    if (tempConditionalData.elseIfStatements.Count > 0)
                        if (tempConditionalData.elseIfStatements[tempConditionalData.elseIfStatements.Count - 1].startLine == tempConditionalData.elseIfStatements[tempConditionalData.elseIfStatements.Count - 1].endLine)
                            tempConditionalData.elseStatement.endLine = nodeData.lines.Count - 1;

                    //If there is an else statement that hasn't been closed, close it with the previous line
                    if (tempConditionalData.elseStatement != null)
                        if (tempConditionalData.elseStatement.startLine == tempConditionalData.elseStatement.endLine)
                            tempConditionalData.elseStatement.endLine = nodeData.lines.Count - 1;

                    //Store it in the node and close the conditional data
                    tempConditionalData.endLine = nodeData.lines.Count;
                    nodeData.conditionalData.Add(tempConditionalData);
                    tempConditionalQueue.RemoveAt(tempConditionalQueue.Count - 1);
                    continue;
                }

                //If this line ends a node, end the read and return the node data
                else if (nodeFound && line.Contains("#EndNode"))
                {
                    nodeFound = false;
                    return nodeData;
                }

                //Add each line to the list if the node has been found
                if (nodeFound)
                    nodeData.AddLine(line);
            }
        }

        //Return if the node being search for wasn't found
        Debug.LogError("Error - Node Not Found: " + nodeName);
        return null;
    }

    /// <summary>
    /// Removes any comments from the line of text.
    /// </summary>
    /// <param name="line">The text within the line.</param>
    /// <returns>Returns the text with all comments stripped from it.</returns>
    private string RemoveCommentsFromLine(string line)
    {
        bool inString = false;

        for(int i = 0; i < line.Length - 1; i++)
        {
            //Ignore any slashes that are inside of a string
            if (line[i] == '"')
                inString = !inString;

            //If there are two subsequent slashes that aren't in dialogue (//), this is a comment. Remove it
            if (!inString && line[i] == '/' && line[i + 1] == '/')
                return line.Substring(0, i).TrimEnd();
        }

        //If there are no comments, return the string as is
        return line;
    }

    /// <summary>
    /// Extracts the conditional statement from a line of text.
    /// </summary>
    /// <param name="line">The line of text to check.</param>
    /// <returns></returns>
    private string ExtractConditionalStatement(string line)
    {
        //Start at the opening parenthesis, if none is found, return empty
        int startIndex = line.IndexOf('(');
        if (startIndex == -1)
            return string.Empty;

        //Keep track of the parentheses depth
        int depth = 0;
        for(int i = startIndex; i < line.Length; i++)
        {
            //If a set of parentheses is started, add to the depth
            if (line[i] == '(')
                depth++;

            //If a set of parentheses is ended, remove from the depth
            else if (line[i] == ')')
            {
                depth--;
                //If all parentheses have been closed, return the resulting substring
                if(depth == 0)
                {
                    int endIndex = i;
                    return line.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                }
            }
        }

        //If there is no closing parenthesis, return empty since that is a syntax error
        return string.Empty;
    }

    private List<Dialogue> GetDialogueLines(NodeData nodeData)
    {
        //If there is no data to read from the node, return
        if (nodeData == null || nodeData.lines.Count == 0)
            return null;

        List<Dialogue> allDialogue = new List<Dialogue>();
        //Get the information from each node
        for (int i = 0; i < nodeData.lines.Count; i++)
        {
            string data = nodeData.lines[i];

            //If there is nothing on this line, continue
            if (string.IsNullOrEmpty(data))
                continue;

            //Add a new line of dialogue
            allDialogue.Add(new Dialogue(data));

            //If the line contains a "|", split the text between speaker and text
            if (data.Contains("|"))
            {
                string[] parts = data.Split("|");
                allDialogue[allDialogue.Count - 1] = new Dialogue(parts[0], parts[1]);
            }
        }

        return allDialogue;
    }

    /// <summary>
    /// Begins the sequence of running a dialogue line.
    /// </summary>
    private void BeginDialogueSequence()
    {
        //Debug.Log(currentNode.name + " Line: " + currentLine);

        string speaker = currentDialogue[currentLine].line.speaker;
        string dialogue = currentDialogue[currentLine].line.text;

        //If there is no text, immediately skip to the next line
        if (string.IsNullOrEmpty(dialogue))
            TryAdvanceText();

        //Check for any tags in the text

        //Variable displays (Pattern = {variableName})
        Regex variablePattern = new(@"\{(\w+)\}");
        dialogue = variablePattern.Replace(dialogue, match =>
        {
            string variableName = match.Groups[1].Value;
            if (Variable.HasVariable(variableName))
            {
                //Get the variable value from the variable dictionary
                object value = Variable.GetVariable<object>(variableName);

                //If the value is null or it cannot be converted to a string, show nothing
                return value?.ToString() ?? string.Empty;
            }

            //If nothing was found, change nothing
            return match.Value;
        });

        //Emphasized text (Pattern = [emp][/emp])
        string hexColor = ColorUtility.ToHtmlStringRGBA(emphasizedColor);
        dialogue = dialogue.Replace("[emp]", $"<color=#{hexColor}>").Replace("[/emp]", "</color>");

        //Italics (Pattern = [it][/it]
        dialogue = dialogue.Replace("[it]", $"<i>").Replace("[/it]", "</i>");

        //If there is no speaker, hide the name box
        if (string.IsNullOrEmpty(speaker))
        {
            nameBox.gameObject.SetActive(false);
            dialogueText.color = internalizedDialogueColor; //Internalized dialogue color
        }

        //If there is a speaker, show the name and the name box
        else
        {
            dialogueText.color = dialogueColor; //Spoken dialogue color
            nameBox.GetComponentInChildren<TextMeshProUGUI>().text = speaker;
            nameBox.gameObject.SetActive(true);

            //If a new speaker is being introduced, move the camera to the speaker
            if (speaker != currentSpeaker)
            {
                currentSpeaker = speaker;
                MoveCamera(currentSpeaker);
            }
        }

        //Start the text writer
        textWriter = TextWriter.Instance.AddTextWriter(null, dialogueText, dialogue, 1f / GameSettings.configData.textSpeed, () => OnTextCompleted());

        //Add the current line to the game data
        GameManager.currentGameData.currentLine = currentLine;

        //Add the text to the transcript
        GameManager.Instance.AddToTranscript(new Transcript(speaker, dialogue));

        //If the text is being fast forward, write all of the text automatically
        if (playbackState == PlaybackState.FastForward)
            textWriter.WriteAllText();
    }

    /// <summary>
    /// Checks for commands to see which ones should be run.
    /// </summary>
    private IEnumerator VerifyDialoguePlacement()
    {
        //Run a sequence until the current line does not change
        int tempCurrentLine;
        do
        {
            //If the line count has reached the end of the dialogue, exit the coroutine
            if (currentLine >= currentDialogue.Count)
                yield break;

            tempCurrentLine = currentLine;  //Save the current line (if this changes, a condition has moved the line)
            CheckForConditionals();         //Check for node conditionals
            CheckForDecisions();            //Check for player decisions

            //If there is a decision active, exit the coroutine
            if (decisionActive)
                yield break;

            string cmd = currentDialogue[currentLine].line.text;

            //If there are no commands, re-loop
            if (string.IsNullOrEmpty(cmd) || cmd[0] != '[' && cmd[cmd.Length - 1] != ']')
                continue;

            //Save the command history
            GameManager.currentGameData.commandHistory.Add(cmd);

            //Run the command
            RunCommand(cmd);

            yield return null;

        } while (tempCurrentLine != currentLine);
    }

    /// <summary>
    /// Runs a command from the dialogue.
    /// </summary>
    /// <param name="cmd">The command to run.</param>
    private void RunCommand(string cmd)
    {
        //Remove the brackets to determine the commmand
        cmd = cmd.Substring(1, cmd.Length - 2);
        currentLine++;

        //Show Player command
        if (cmd == "ShowPlayer")
        {
            WorldCamera.main.AddProtagOffset();
            ShowProtagView();
        }
        //Hide Player command
        else if (cmd == "HidePlayer")
        {
            WorldCamera.main.RemoveProtagOffset();
            HideProtagView();
        }
        //Show CG command
        else if (cmd.StartsWith("CG="))
        {
            ShowCG(cmd.Split("=")[1]);
        }
        //Hide CG command
        else if (cmd == "HideCG")
        {
            HideCG();
        }
        //Go to command
        else if (cmd.StartsWith("GoTo="))
        {
            FindNode(cmd.Split("=")[1]);
        }
        //Play music command
        else if (cmd.StartsWith("Music="))
        {
            GameManager.Instance.AudioManager.PlayMusic(cmd.Split("=")[1]);
        }
        //Stop music command
        else if (cmd == "StopMusic")
        {
            GameManager.Instance.AudioManager.StopMusic(); ;
        }
    }

    /// <summary>
    /// Shows a CG.
    /// </summary>
    /// <param name="fileName">The name of the image file (without an extension).</param>
    private void ShowCG(string fileName)
    {
        Sprite currentCGSprite = Resources.Load<Sprite>(CGFolder + fileName);

        //If an image is found, show the image
        if(currentCGSprite != null)
        {
            CGImage.sprite = currentCGSprite;
            CGCanvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Hides a CG.
    /// </summary>
    private void HideCG()
    {
        CGImage.sprite = null;
        CGCanvasGroup.alpha = 0f;
    }

    private void MoveCamera(string speakerName)
    {
        GameObject speakerCharacter = GameObject.FindGameObjectWithTag(speakerName);

        //If a speaker character is found, move the camera to the speaker
        if(speakerCharacter != null && speakerCharacter.TryGetComponent(out Character newCharacter))
        {
            //If the protagonist is not speaking, move the camera to the in-world character
            if (speakerName != "Player")
                WorldCamera.main.MoveTo(newCharacter.GetCameraTransform().position, true);
        }
    }

    /// <summary>
    /// Shows the protagonist camera view.
    /// </summary>
    private void ShowProtagView()
    {
        StartCoroutine(MoveProtagCameraAnimation(protagCamRectTransform.anchoredPosition, protagCamActivePos));
    }

    /// <summary>
    /// Hides the protagonist camera view.
    /// </summary>
    private void HideProtagView()
    {
        StartCoroutine(MoveProtagCameraAnimation(protagCamRectTransform.anchoredPosition, protagCamInactivePos));
    }

    /// <summary>
    /// Moves the protagonist camera's position.
    /// </summary>
    /// <param name="startPos">The starting position of the camera.</param>
    /// <param name="endPos">The ending position of the camera.</param>
    private IEnumerator MoveProtagCameraAnimation(Vector2 startPos, Vector2 endPos)
    {
        float moveElapsed = 0f;
        while (moveElapsed < protagCamSpeed)
        {
            moveElapsed += Time.deltaTime * DialogueSpeedMultiplier;
            protagCamRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, moveElapsed / protagCamSpeed); //Lerp the camera position
            yield return null;
        }

        //Set the final values
        protagCamRectTransform.anchoredPosition = endPos;
    }

    private void ReadPlayerInput()
    {
        //If the game is in the game menu, return
        if (GameManager.Instance.InGameMenu)
            return;

        //If a decision is active, return
        if (decisionActive)
            return;

        //If the player is selecting a button, return
        if (controlBar.InUse())
            return;

        switch (playbackState)
        {
            //If the player attempts to click while on auto, revert to normal 
            case PlaybackState.Auto:
                playbackState = PlaybackState.Normal;
                RefreshAutoIndicator();
                break;
            //If the player attempts to click while fast forwarding, revert to normal
            case PlaybackState.FastForward:
                ToggleFastForward();
                break;
            default:
                //Try to advance text
                CheckForAdvanceText();
                break;
        }
    }

    public void BeginAutoRead()
    {
        //If the text is already complete, go to the next line
        if (textWriter.TextComplete)
            TryAdvanceText();
    }

    public void ToggleFastForward()
    {
        //If the play state is anything but fast forward, set it to fast forward
        if (playbackState != PlaybackState.FastForward)
            playbackState = PlaybackState.FastForward;
        //If currently fast forwarding, return to normal
        else
            playbackState = PlaybackState.Normal;

        //Toggle the speed multiplier based on the playback state
        DialogueSpeedMultiplier = playbackState == PlaybackState.FastForward ? 1f / fastForwardTime : 1f;
    }

    /// <summary>
    /// Calculate the time to read the text.
    /// </summary>
    public void StartAutoReaderTimer()
    {
        //If the current line is out of bounds from the current dialogue, return
        if (currentLine >= currentDialogue.Count)
            return;

        //Set the time for the text to auto read (minimum read time + time to read one character * the characters in the string of text)
        timeToReadText = timePerCharacter * currentDialogue[currentLine].line.GetRawText().Length * GameSettings.configData.autoForwardTime;
        autoTimerActive = true;
    }

    private void Update()
    {
        //If a decision is active, return
        if (decisionActive)
            return;

        switch (playbackState)
        {
            case PlaybackState.Auto:
                if (autoTimerActive)
                {
                    //Advance the text when the timer is reached
                    if (autoTimerElapsed >= timeToReadText)
                    {
                        TryAdvanceText();
                    }
                    else
                        autoTimerElapsed += Time.deltaTime;
                }
                break;
            case PlaybackState.FastForward:
                //Advance the text when the timer is reached
                if (fastForwardTimerElapsed >= fastForwardTime)
                {
                    TryAdvanceText();
                    fastForwardTimerElapsed = 0f;
                }
                else
                    fastForwardTimerElapsed += Time.deltaTime;
                break;
        }
    }

    /// <summary>
    /// Checks to see if the text can be advanced or if it needs to still be displayed fully.
    /// </summary>
    private void CheckForAdvanceText()
    {
        //If there is no text writer, return
        if (textWriter == null)
            return;

        //If the text is complete, advance the dialogue
        if (textWriter.TextComplete)
            TryAdvanceText();
        //If the text is not complete, write all of the text first
        else
            textWriter.WriteAllText();
    }

    /// <summary>
    /// Tries to advance the text in the dialogue.
    /// </summary>
    private void TryAdvanceText()
    {
        autoTimerActive = false;
        autoTimerElapsed = 0f;
        currentLine++;

        ShowCurrentLine();
    }

    /// <summary>
    /// Shows the current line in the dialogue.
    /// </summary>
    private void ShowCurrentLine()
    {
        //Hide the continue icon
        continueIcon.gameObject.SetActive(false);

        //Check for conditions and commands
        StartCoroutine(VerifyDialoguePlacement());

        //If there is a decision active, don't show the next line
        if (decisionActive)
            return;

        //If all of the dialogue has been seen, end the node
        if (currentLine >= currentDialogue.Count)
            EndNode();
        //Begin showing the current line of the current node
        else
            BeginDialogueSequence();
    }

    /// <summary>
    /// Checks to see if a decision needs to be made for the current line.
    /// </summary>
    private void CheckForDecisions()
    {
        //If there is an active decision
        if(decisionDataQueue.Count > 0)
        {
            //Debug.Log("Decision: Line " + decisionDataQueue[decisionDataQueue.Count - 1].startLine + " - " + decisionDataQueue[decisionDataQueue.Count - 1].endLine);
            //If the current branch dialogue has reached the end of the line, skip to the end of the decision data
            if (currentLine >= decisionQueue[decisionQueue.Count - 1].endLine)
            {
                //Skip to the end of the decision data
                currentLine = decisionDataQueue[decisionDataQueue.Count - 1].endLine;

                //Clear current decision data
                decisionDataQueue.RemoveAt(decisionDataQueue.Count - 1);
                decisionQueue.RemoveAt(decisionQueue.Count - 1);
                GameManager.currentGameData.decisionDataQueue = decisionDataQueue;
                GameManager.currentGameData.decisionQueue = decisionQueue;

                //Check for other decisions
                if (decisionDataQueue.Count > 0)
                    CheckForDecisions();
                else
                    return;
            }
        }

        //Check to see if the player made a decision in the queue at this line
        foreach (var decisionData in decisionDataQueue)
            foreach (var d in decisionData.decisions)
                if (d.startLine == currentLine)
                    return;

        foreach (DecisionData decision in currentNode.decisionData)
        {
            //If a decision needs to be made on this line, show the decision
            if (decision.startLine == currentLine && !decisionDataQueue.Contains(decision))
            {
                decisionDataQueue.Add(decision);
                GameManager.currentGameData.decisionDataQueue = decisionDataQueue;

                //Get all of the decisions from the data
                List<string> decisionLines = new List<string>();
                foreach(DecisionData.Decision d in decision.decisions)
                {
                    //Debug.Log(d.decision + ": Lines " + d.startLine + " - " + d.endLine);
                    decisionLines.Add(d.decision);
                }

                //Show the decision UI with the corresponding decisions
                decisionManager.CreateDecisions(decisionLines.ToArray());
                break;
            }
        }
    }

    public void MakeDecision(int decisionIndex)
    {
        //If there is no decision data, return null
        if (decisionDataQueue.Count == 0)
            return;

        //Set the line to the line indicated by the decision
        decisionQueue.Add(decisionDataQueue[decisionDataQueue.Count - 1].decisions[decisionIndex]);
        currentLine = decisionQueue[decisionQueue.Count - 1].startLine;
        GameManager.currentGameData.decisionQueue = decisionQueue;

        //Hide the decision UI
        decisionManager.HideUI();

        //If the config doesn't continue after skipping, turn off the fast forward
        if (!GameSettings.configData.skipAfterChoices && playbackState == PlaybackState.FastForward)
            ToggleFastForward();
    }

    private void CheckForConditionals()
    {
        if (conditionalDataQueue.Count > 0)
        {
            //If the current conditional branch has finished, skip to the end of the entire conditional data segment and remove the data
            Conditional currentConditional = currentConditionalQueue[currentConditionalQueue.Count - 1];
            if (currentConditional.endLine >= currentLine)
            {
                currentLine = conditionalDataQueue[conditionalDataQueue.Count - 1].endLine;

                //Clear the conditional data
                currentConditionalQueue.RemoveAt(currentConditionalQueue.Count - 1);
                conditionalDataQueue.RemoveAt(conditionalDataQueue.Count - 1);
                GameManager.currentGameData.conditionalDataQueue = conditionalDataQueue;
                GameManager.currentGameData.currentConditionalQueue = currentConditionalQueue;
            }
        }

        foreach(ConditionalData conditionalData in currentNode.conditionalData)
        {
            //Debug.Log("Conditional: Line " + conditionalData.startLine + " - " + conditionalData.endLine +  " | Current Line: " + currentLine);
            //If a conditional starts here, check for decisions
            if(conditionalData.startLine == currentLine)
            {
                conditionalDataQueue.Add(conditionalData);
                GameManager.currentGameData.conditionalDataQueue = conditionalDataQueue;

                //Check for the starting if statement
                if (conditionalData.ifStatement.Evaluate())
                {
                    currentConditionalQueue.Add(conditionalData.ifStatement);
                    GameManager.currentGameData.currentConditionalQueue = currentConditionalQueue;
                    currentLine = conditionalData.ifStatement.startLine;
                    break;
                }

                //Check for any sequential else if statements
                Conditional elseIfConditional = null;
                foreach(Conditional elseIf in conditionalData.elseIfStatements)
                {
                    if (elseIf.Evaluate())
                    {
                        elseIfConditional = elseIf;
                        break;
                    }
                }

                //If any else if statement returned true, store it
                if(elseIfConditional != null)
                {
                    currentConditionalQueue.Add(elseIfConditional);
                    GameManager.currentGameData.currentConditionalQueue = currentConditionalQueue;
                    currentLine = elseIfConditional.startLine;
                    break;
                }

                //If nothing returned true, use the else statement
                currentConditionalQueue.Add(conditionalData.elseStatement);
                GameManager.currentGameData.currentConditionalQueue = currentConditionalQueue;
                currentLine = conditionalData.elseStatement.startLine;
            }
        }
    }

    /// <summary>
    /// The function called when the text is completed.
    /// </summary>
    private void OnTextCompleted()
    {
        //If the auto reader is on, start the timer
        if (playbackState == PlaybackState.Auto)
            StartAutoReaderTimer();

        //Show the continue graphic
        ShowContinue();
    }

    /// <summary>
    /// Shows the text continue graphic.
    /// </summary>
    private void ShowContinue()
    {
        continueIcon.gameObject.SetActive(playbackState == PlaybackState.Normal);
    }

    /// <summary>
    /// Refreshes the visibility of the auto text graphic.
    /// </summary>
    public void RefreshAutoIndicator()
    {
        autoIndicator.gameObject.SetActive(playbackState == PlaybackState.Auto);
        ShowContinue();
    }

    private void EndNode()
    {
        //Add the node to the list of completed nodes
        GameManager.currentGameData.CompleteNode();

        //If there is a node to go back to, return to it
        NodeData previousNode = GameManager.currentGameData.GetLatestNode();
        if (previousNode != null)
        {
            FindNode(previousNode.name);
        }
        else
        {
            GameManager.Instance.LoadScene("Title");
        }
    }

    /// <summary>
    /// Sets whether a decision is being made by the player or not.
    /// </summary>
    /// <param name="decisionActive">If true, the player is making a decision. False if otherwise.</param>
    public void SetDecisionActive(bool decisionActive)
    {
        this.decisionActive = decisionActive;

        //Clear the decision data and show the line indicated from the decision
        if (!decisionActive)
            ShowCurrentLine();
    }

    public IEnumerator CreateAutoSave()
    {
        //Get the least recent auto-save index and use that
        int autoSlotIndex = PlayerPrefs.GetInt("LastAutosave", 0);
        autoSlotIndex = (autoSlotIndex % 6) + 1;

        //Screenshot the game
        yield return GameManager.Instance.ScreenshotGame();

        //Save the auto-save to a file
        SaveData.SaveToFile(SaveSlot.SaveType.Auto, autoSlotIndex);
    }
}
