using UnityEngine;
using UnityEngine.Events;
using System.Text;

public static class CommandConsole
{
    public static UnityEvent<string> OnConsoleTextChanged { get; private set; } = new UnityEvent<string>();
    public static CommandsOutput OutputType = CommandsOutput.CommandConsoleOnly; // TODO : faire une editor window pour pouvoir modifier l'output type et les messages de bienvenu dans Initialize()

    [Command("console_output")]
    static void ChangeOutput(CommandsOutput outputType)
    {
        OutputType = outputType;
    }

    static StringBuilder _stringBuilder;

    public enum CommandsOutput
    {
        CommandConsoleOnly,
        UnityConsoleOnly,
        Both
    }


    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        _stringBuilder = new StringBuilder();
        CommandConsole.Log("- Welcome to the CommandConsole -");
        CommandConsole.Log("Type 'help' to see available commands!");
    }

    public static void TryExecuteCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        CommandManager.ExecuteCommand(text);
    }

    // Auto complétion
    /*
    private void DisplayAutoCompletion(string[] autoCompleteCommands)
    {
        Console.WriteLine("Auto completion:");
        Console.WriteLine(string.Join(", ", autoCompleteCommands));
    }

    private void HandleAutoCompletion(string[] autoCompleteCommands)
    {
        if (autoCompleteCommands.Length == 1)
        {
            Console.Write(autoCompleteCommands[0] + " ");
        }
        else if (autoCompleteCommands.Length > 1)
        {
            DisplayAutoCompletion(autoCompleteCommands);

            var input = Console.ReadLine();
            var commonCharacters = input
                .Zip(autoCompleteCommands[0], (c1, c2) => c1 == c2 ? c1 : (char?)null)
                .TakeWhile(c => c.HasValue)
                .Select(c => c.Value);

            Console.Write(commonCharacters.ToArray());
        }
    }
    */

    public static void Log(string text)
    {
        if (OutputType == CommandsOutput.UnityConsoleOnly)
        {
            Debug.Log(text);
            return;
        }

        if (OutputType == CommandsOutput.Both)
            Debug.Log(text);

        CommandConsoleLog(text, Color.white);
    }
    public static void LogSuccess(string text)
    {
        if (OutputType == CommandsOutput.UnityConsoleOnly)
        {
            Debug.Log(text);
            return;
        }

        if (OutputType == CommandsOutput.Both)
            Debug.Log(text);

        CommandConsoleLog(text, Color.green);
    }

    public static void LogWarning(string text)
    {
        if (OutputType == CommandsOutput.UnityConsoleOnly)
        {
            Debug.LogWarning(text);
            return;
        }

        if (OutputType == CommandsOutput.Both)
            Debug.LogWarning(text);

        CommandConsoleLog(text, Color.yellow);
    }

    public static void LogError(string text)
    {
        if (OutputType == CommandsOutput.UnityConsoleOnly)
        {
            Debug.LogError(text);
            return;
        }

        if (OutputType == CommandsOutput.Both)
            Debug.LogError(text);

        CommandConsoleLog(text, Color.red);
    }

    static void CommandConsoleLog(string text, Color color)
    {
        if (_stringBuilder == null)
            _stringBuilder = new StringBuilder();

        _stringBuilder.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}");
        OnConsoleTextChanged.Invoke(_stringBuilder.ToString());
    }

    [Command("clear", "Clears this console.", false)]
    public static void Clear()
    {
        _stringBuilder.Clear();
        OnConsoleTextChanged.Invoke(_stringBuilder.ToString());
    }
}