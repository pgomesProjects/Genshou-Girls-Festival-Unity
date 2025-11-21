using System.Text.RegularExpressions;

[System.Serializable]
public class Dialogue
{
    public Transcript line;

    public Dialogue(string speaker, string text)
    {
        line = new Transcript(speaker, text);
    }

    public Dialogue(string text)
    {
        line = new Transcript(text);
    }
}

[System.Serializable]
public class Transcript
{
    public string speaker;
    public string text;

    public Transcript(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }

    public Transcript(string text)
    {
        this.speaker = string.Empty;
        this.text = text;
    }

    /// <summary>
    /// Gets the text as displayed, without any commands in them.
    /// </summary>
    /// <returns>A string of text with no commands.</returns>
    public string GetRawText() => Regex.Replace(text, @"\[.*?\]", "");
}