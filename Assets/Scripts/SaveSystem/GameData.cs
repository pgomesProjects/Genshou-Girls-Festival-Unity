using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


[System.Serializable]
public class GameData 
{
    [Tooltip("The current history of nodes viewed.")] public List<NodeData> nodeHistory;
    [Tooltip("The current history of decision trees visited.")] public List<DecisionData> decisionDataQueue;
    [Tooltip("The current history of decisions made.")] public List<DecisionData.Decision> decisionQueue;
    [Tooltip("The current history of conditional branches reached.")] public List<ConditionalData> conditionalDataQueue;
    [Tooltip("The current history of conditionals met.")] public List<Conditional> currentConditionalQueue;
    [Tooltip("The current history of commands run.")] public List<string> commandHistory;
    [Tooltip("The list of completed nodes.")] public List<string> completedNodes;
    [Tooltip("The saved transcript of the game.")] public List<Transcript> transcript;
    [Tooltip("The saved variable dictionary.")] public SerializedDictionary<string, Variable> variableDictionary;
    [Tooltip("The current line for the most recent node.")] public int currentLine;

    public GameData()
    {
        nodeHistory = new List<NodeData>();
        decisionDataQueue = new List<DecisionData>();
        decisionQueue = new List<DecisionData.Decision>();
        conditionalDataQueue = new List<ConditionalData>();
        currentConditionalQueue = new List<Conditional>();
        commandHistory = new List<string>();
        completedNodes = new List<string>();
        transcript = new List<Transcript>();
        variableDictionary = new SerializedDictionary<string, Variable>();
        currentLine = 0;
    }

    /// <summary>
    /// Gets the latest node from the node history.
    /// </summary>
    /// <returns>The NodeData object at the front of the list.</returns>
    public NodeData GetLatestNode()
    {
        //If there is no history, return null
        if (nodeHistory.Count == 0)
            return null;

        //Return the latest node
        return nodeHistory[nodeHistory.Count - 1];
    }

    /// <summary>
    /// Removes the latest node from the node history.
    /// </summary>
    public void CompleteNode()
    {
        //If there is no history, return
        if (nodeHistory.Count == 0)
            return;

        //Add the name of the current node to the list of completed nodes and remove it from the history
        completedNodes.Add(nodeHistory[nodeHistory.Count - 1].name);
        nodeHistory.RemoveAt(nodeHistory.Count - 1);
    }
}

[System.Serializable]
public class NodeData
{
    public string name;
    public string sceneName;
    public int line;
    public List<string> lines;
    public List<DecisionData> decisionData;
    public List<ConditionalData> conditionalData;

    public NodeData(string name, string sceneName)
    {
        this.name = name;
        this.sceneName = sceneName;
        this.line = 0;
        this.lines = new List<string>();
        this.decisionData = new List<DecisionData>();
        this.conditionalData = new List<ConditionalData>();
    }

    public NodeData(string name, string sceneName, int line, List<string> lines, List<DecisionData> decisionData, List<ConditionalData> conditionalData)
    {
        this.name = name;
        this.sceneName = sceneName;
        this.line = line;
        this.lines = lines;
        this.decisionData = decisionData;
        this.conditionalData = conditionalData;
    }

    public void AddLine(string dialogue)
    {
        lines.Add(dialogue);
        //Debug.Log((lines.Count - 1).ToString() + ": " + dialogue);
    }
}

[System.Serializable]
public class DecisionData
{
    [System.Serializable]
    public class Decision
    {
        public int startLine;
        public int endLine;
        public string decision;

        public Decision(int startLine, string decision)
        {
            this.startLine = startLine;
            this.endLine = startLine;
            this.decision = decision;
        }

        public override string ToString()
        {
            return decision + ": Line " + startLine + " to Line " + endLine;
        }
    }

    public int startLine;
    public int endLine;
    public List<Decision> decisions;

    public DecisionData(int startLine)
    {
        this.startLine = startLine;
        this.endLine = startLine;
        this.decisions = new List<Decision>();
    }

    public override string ToString()
    {
        string message = "Decisions (From Line " + startLine + " to Line " + endLine + "): ";

        for(int i = 0; i < decisions.Count; i++)
        {
            message += decisions[i].ToString();

            //Add a comma for all items except the last
            if (i < decisions.Count - 1)
                message += ", ";
        }

        return message;
    }
}

[System.Serializable]
public class ConditionalData
{
    public Conditional ifStatement;
    public List<Conditional> elseIfStatements;
    public Conditional elseStatement;

    public int startLine;
    public int endLine;

    public ConditionalData(Conditional ifStatement)
    {
        this.ifStatement = ifStatement;
        this.elseIfStatements = new List<Conditional>();

        this.startLine = ifStatement.startLine;
        this.endLine = startLine;
    }
}

[System.Serializable]
public class Conditional
{
    public string conditional;
    public int startLine;
    public int endLine;

    public Conditional(string conditional, int startLine)
    {
        this.conditional = conditional;
        this.startLine = startLine;
        this.endLine = startLine;
    }

    public Conditional(int startLine)
    {
        this.conditional = string.Empty;
        this.startLine = startLine;
        this.endLine = startLine;
    }

    /// <summary>
    /// Evaluates whether the conditional is true or false.
    /// </summary>
    /// <returns>Returns a true / false value.</returns>
    public bool Evaluate()
    {
        //If there is no conditional, return false
        if (string.IsNullOrWhiteSpace(conditional))
            return false;

        //Replace any variable names with their respective values (regex gets any groups of singular words)
        string valuedConditional = Regex.Replace(conditional, @"\b(\w+)\b", match =>
        {
            string variableName = match.Groups[1].Value;

            //If the variable has been found, change the variable name in the text to the value for more convenience
            if (Variable.TryGetVariable(variableName, out object variableValue))
            {
                //Return the string with quotes around it
                if(variableValue is string s)
                    return "\"" + s + "\"";

                //Return the boolean as fully lowercase
                if (variableValue is bool b)
                    return b.ToString().ToLower();

                //Return the string value of the variable
                return variableValue.ToString();
            }

            //If not found, do not do anything
            return match.Value;
        });

        //Evaluate the conditional
        try
        {
            return EvaluateExpressions(valuedConditional);
        }
        //If any error is thrown during the evaluation, catch the error and return false
        catch(Exception e)
        {
            Debug.LogError("Error evaluating conditional '" + conditional + "': " + e.Message);
            return false;
        }
    }

    private bool EvaluateExpressions(string valuedConditional)
    {
        //Trim any whitespace before evaluating
        valuedConditional = valuedConditional.Trim();

        //Evaluate parentheses first
        int parenthesesDepth = valuedConditional.LastIndexOf('(');
        while(parenthesesDepth != -1)
        {
            int closedParentheses = valuedConditional.IndexOf(')', parenthesesDepth);
            //If there is no matching parenthesis, throw an error
            if (closedParentheses == -1)
                throw new Exception("Unmatched parentheses");

            //Recursively evaluate the inner expression
            string innerExpression = valuedConditional.Substring(parenthesesDepth + 1, closedParentheses - parenthesesDepth - 1);
            bool expressionValue = EvaluateExpressions(innerExpression);

            //Replace the inner expressions with a true or false value and check for the next set of parentheses, if any
            valuedConditional = valuedConditional.Substring(0, parenthesesDepth) + (expressionValue ? "true" : "false") + valuedConditional.Substring(closedParentheses + 1);
            parenthesesDepth = valuedConditional.LastIndexOf('(');
        }

        //Check for a logical AND by spliting the conditional into parts
        if (valuedConditional.Contains("&&"))
        {
            //If any of the parts of this expression returns false, the whole conditional is false
            string[] parts = valuedConditional.Split("&&");
            foreach (string part in parts)
                if (!EvaluateExpressions(part))
                    return false;

            return true;
        }

        //Check for a logical AND by spliting the conditional into parts
        if (valuedConditional.Contains("||"))
        {
            //If any of the parts of this expression returns true, the whole conditional is true
            string[] parts = valuedConditional.Split("||");
            foreach (string part in parts)
                if (EvaluateExpressions(part))
                    return true;

            return false;
        }

        //If the expression starts with an exclamation mark, check for the opposite value
        if (valuedConditional.StartsWith("!"))
            return !EvaluateExpressions(valuedConditional.Substring(1));

        //Check for comparisons between both sides of the current statement
        if (valuedConditional.Contains(">=")) return Compare(valuedConditional, ">=");
        if (valuedConditional.Contains("<=")) return Compare(valuedConditional, "<=");
        if (valuedConditional.Contains("==")) return Compare(valuedConditional, "==");
        if (valuedConditional.Contains("!=")) return Compare(valuedConditional, "!=");
        if (valuedConditional.Contains(">")) return Compare(valuedConditional, ">");
        if (valuedConditional.Contains("<")) return Compare(valuedConditional, "<");

        //Check for literal boolean values
        if(bool.TryParse(valuedConditional, out bool boolValue))
            return boolValue;

        //Check for truthiness (if a singular value is not 0)
        if (float.TryParse(valuedConditional, out float numVal)) 
            return numVal != 0;

        //Check for string literals
        return !string.IsNullOrEmpty(valuedConditional.Trim('"'));
    }

    private bool Compare(string expression, string op)
    {
        string[] parts = expression.Split(op);
        //If there are not two parts to compare, return false immediately
        if (parts.Length != 2)
            return false;

        //Get the left and right values separated and trimmed
        string left = parts[0].Trim().Trim('"');
        string right = parts[1].Trim().Trim('"');

        //Check for numerical values and use numerical comparisons
        bool leftIsNumerical = float.TryParse(left, out float leftNum);
        bool rightIsNumerical = float.TryParse(right, out float rightNum);
        if(leftIsNumerical && rightIsNumerical)
        {
            switch (op)
            {
                case "==": return leftNum == rightNum;
                case "!=": return leftNum != rightNum;
                case ">": return leftNum > rightNum;
                case "<": return leftNum < rightNum;
                case ">=": return leftNum >= rightNum;
                case "<=": return leftNum <= rightNum;
            }
        }

        //Check for boolean values and use boolean comparisons
        bool leftIsBool = bool.TryParse(left, out bool leftBool);
        bool rightIsBool = bool.TryParse(right, out bool rightBool);
        if (leftIsBool && rightIsBool)
        {
            switch (op)
            {
                case "==": return leftBool == rightBool;
                case "!=": return leftBool != rightBool;
            }
        }

        //String comparisons via btye comparison for exact values
        switch (op)
        {
            case "==": return string.Equals(left, right, StringComparison.Ordinal);
            case "!=": return !string.Equals(left, right, StringComparison.Ordinal);
            case ">": return string.Compare(left, right, StringComparison.Ordinal) > 0;
            case "<": return string.Compare(left, right, StringComparison.Ordinal) < 0;
            case ">=": return string.Compare(left, right, StringComparison.Ordinal) >= 0;
            case "<=": return string.Compare(left, right, StringComparison.Ordinal) <= 0;
        }

        //If nothing else applies, an unsupported operator is being used
        throw new Exception("Unsupported operator: " + op);
    }
}

[System.Serializable]
public class Variable
{
    public string name;
    public object value;

    public Variable(string name, object value)
    {
        this.name = name;
        this.value = value;
    }

    private static Dictionary<string, Variable> variables = new Dictionary<string, Variable>();

    /// <summary>
    /// Sets the variable information.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    public static void SetVariable(string name, object value)
    {
        variables[name] = new Variable(name, value);
        GameManager.currentGameData.variableDictionary = new SerializedDictionary<string, Variable>(variables);
    }

    /// <summary>
    /// Gets the value of the variable.
    /// </summary>
    /// <typeparam name="V">The variable datatype to use.</typeparam>
    /// <param name="name">The name of the variable.</param>
    /// <returns>The value of the variable converted to the specified datatype.</returns>
    public static V GetVariable<V>(string name)
    {
        //If the name exists, get the variable and convert it to the type specified
        if (variables.TryGetValue(name, out var variable))
            return (V)Convert.ChangeType(variable.value, typeof(V));

        //Otherwise, throw an exception since the variable does not exist
        throw new Exception("Variable " + name + " not found.");
    }

    /// <summary>
    /// Tries to get the value of the variable.
    /// </summary>
    /// <typeparam name="V">The variable datatype to use.</typeparam>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">Outputs the value of the variable.</param>
    /// <returns>Returns true if the value is not null. Returns false if otherwise.</returns>
    public static bool TryGetVariable<V>(string name, out V value)
    {
        //If the name exists, get the variable and convert it to the type specified
        if(variables.TryGetValue(name, out var variable))
        {
            //Try to convert the value to the type of the generic, send warnings if unsuccessful
            try
            {
                value = (V)Convert.ChangeType(variable.value, typeof(V));
                return true;
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning("Variable '" + name + "' exists but cannot be converted to " + typeof(V).Name + ".");
            }
            catch (FormatException)
            {
                Debug.LogWarning("Variable '" + name + "' exists but its value format is invalid for " + typeof(V).Name + ".");
            }
        }

        //Fallback to default values
        value = default;
        return false;
    }

    /// <summary>
    /// Checks to see if a variable exists.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <returns>If true, the variable exists in the variable dictionary.</returns>
    public static bool HasVariable(string name) => variables.ContainsKey(name);

    /// <summary>
    /// Parses the value of the object based on the text provided.
    /// </summary>
    /// <param name="rawValue">The text of the value found in the file.</param>
    /// <returns>The object as the appropriate datatype.</returns>
    public static object ParseValue(string rawValue)
    {
        //Check for a string (surrounded by quotes)
        if (rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
            return rawValue.Substring(1, rawValue.Length - 2);

        //Check for a boolean value
        if (bool.TryParse(rawValue, out bool boolValue))
            return boolValue;

        //Check for an integer value
        if (int.TryParse(rawValue, out int intValue))
            return intValue;

        //Return as generic object if none apply
        return rawValue;
    }

    /// <summary>
    /// Sets the variable dictionary keys / values.
    /// </summary>
    /// <param name="dictionary">The dictionary data to set.</param>
    public static void SetDictionary(Dictionary<string, Variable> dictionary) => variables = dictionary;
}
