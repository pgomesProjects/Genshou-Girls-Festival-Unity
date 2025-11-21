using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class TextWriter : MonoBehaviour
{
    public class TextWriterInstance
    {
        private Action onBegin, onComplete;
        private string dialogue;
        private int charIndex;
        private float timePerChar, timer;

        public TextMeshProUGUI UIText { get; private set; }
        public bool TextComplete { get; private set; }

        public TextWriterInstance(Action onBegin, TextMeshProUGUI uiText, string dialogue, float timePerChar, Action onComplete)
        {
            //Set variables
            this.onBegin = onBegin;
            UIText = uiText;
            this.dialogue = dialogue;
            this.timePerChar = timePerChar;
            this.onComplete = onComplete;
            charIndex = 0;
            TextComplete = false;

            //Invoke the onBegin command
            this.onBegin?.Invoke();
        }

        /// <summary>
        /// Writes the dialogue text.
        /// </summary>
        /// <returns>Returns true if the text has finished writing. Returns false otherwise.</returns>
        public bool WriteText()
        {
            //Decrement timer
            timer -= Time.deltaTime;
            while (timer <= 0f)
            {
                //If the text is complete, return
                if (charIndex >= dialogue.Length)
                    return true;

                //If a rich-text command is found, write it out entirely
                if(dialogue[charIndex] == '<')
                {
                    //If the ending tag is found, jump to the end of the tag
                    int closingIndex = dialogue.IndexOf('>', charIndex);
                    if (closingIndex != -1)
                        charIndex = closingIndex + 1;

                    //If no ending tag is found, simply advance to the next character
                    else
                        charIndex++;
                }
                //Display the next character normally otherwise
                else
                {
                    timer += timePerChar;
                    charIndex++;
                }

                //Show the current substring, plus all of the other text as invisible characters to keep the text aligned
                UIText.text = BuildVisibleText(dialogue, charIndex);

                //If the character index has reached the length of the dialogue line, invoke the complete command and return true
                if (charIndex >= dialogue.Length)
                {
                    onComplete?.Invoke();
                    TextComplete = true;
                    return true;
                }
            }

            TextComplete = false;
            return false;
        }

        /// <summary>
        /// Builds the visible text to be shown.
        /// </summary>
        /// <param name="dialogue">The dialogue to write.</param>
        /// <param name="charIndex">The current index of the dialogue to show.</param>
        /// <returns>The substring of text to show on screen.</returns>
        private string BuildVisibleText(string dialogue, int charIndex)
        {
            //Get the characters that are already displayed
            StringBuilder sb = new StringBuilder();
            sb.Append(dialogue.Substring(0, charIndex));

            //Go through the rest of the text that hasn't been display
            int i = charIndex;
            while (i < dialogue.Length)
            {
                //If a rich-text command is found, write the command out entirely
                if (dialogue[i] == '<')
                {
                    int closingIndex = dialogue.IndexOf('>', i);
                    if (closingIndex != -1)
                    {
                        sb.Append(dialogue.Substring(i, closingIndex - i + 1));
                        i = closingIndex + 1;
                        continue;
                    }
                }

                //Wrap every single character that will be displayed as invisible to align the text properly
                sb.Append("<color=#00000000>");
                sb.Append(dialogue[i]);
                sb.Append("</color>");
                i++;
            }

            //Return the resulting string
            return sb.ToString();
        }

        /// <summary>
        /// Writes all of the text immediately.
        /// </summary>
        public void WriteAllText()
        {
            UIText.text = dialogue;
            charIndex = dialogue.Length;
            onComplete?.Invoke();
            TextComplete = true;
        }
    }

    public static TextWriter Instance;
    private List<TextWriterInstance> textWriters = new List<TextWriterInstance>();

    private void Awake()
    {
        //Singleton-ize script
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    /// <summary>
    /// Adds a text writer to the list.
    /// </summary>
    /// <param name="onBegin">Any action called when the text begins writing.</param>
    /// <param name="uiText">The text component to write onto.</param>
    /// <param name="dialogue">The text to writer.</param>
    /// <param name="timePerChar">The time it takes to write each character (in seconds).</param>
    /// <param name="onComplete">Any action called when the text ends writing.</param>
    /// <returns>Returns the text writer instance created.</returns>
    public TextWriterInstance AddTextWriter(Action onBegin, TextMeshProUGUI uiText, string dialogue, float timePerChar, Action onComplete)
    {
        TextWriterInstance textWriterInstance = new TextWriterInstance(onBegin, uiText, dialogue, timePerChar, onComplete);
        textWriters.Add(textWriterInstance);
        return textWriterInstance;
    }

    /// <summary>
    /// Removes a writer from the list.
    /// </summary>
    /// <param name="uiText">The text component that the writer is writing to.</param>
    public void RemoveWriter(TextMeshProUGUI uiText)
    {
        for (int i = 0; i < textWriters.Count; i++)
        {
            //If the text component has been found, remove it from the list
            if (textWriters[i].UIText == uiText)
            {
                textWriters.RemoveAt(i);
                i--;
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < textWriters.Count; i++)
        {
            //Write the text
            bool textComplete = textWriters[i].WriteText();

            //If the text is complete, remove the text writer
            if (textComplete)
            {
                textWriters.RemoveAt(i);
                i--;
            }
        }
    }
}
