using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class CommandManager
{
    static Dictionary<string, CommandInfo> _commands;

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        _commands = new Dictionary<string, CommandInfo>();

        Type[] types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (Type type in types)
        {
            bool isSubClassOfObject = type.IsSubclassOf(typeof(UnityEngine.Object));

            foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                bool isMethodStatic = methodInfo.IsStatic;

                if (!isMethodStatic && !isSubClassOfObject)
                    continue;

                CommandAttribute commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();

                if (commandAttribute != null)
                {
                    object[] instances = null;

                    if (!methodInfo.IsStatic && isSubClassOfObject)
                        instances = Resources.FindObjectsOfTypeAll(type);

                    CommandInfo commandInfo = new CommandInfo(methodInfo, instances, commandAttribute.Description, commandAttribute.DisplaySuccessMessage);
                    if (!_commands.TryAdd(commandAttribute.CommandName, commandInfo))
                    {
                        CommandConsole.LogWarning($"It is not possible to declare several commands with the same name ({commandAttribute.CommandName}). Only the first one has been retained.");
                    }
                }
            }
        }
    }

    public static void ExecuteCommand(string commandLine)
    {
        // Récupération de la commande et de ses arguments
        string[] tokens = commandLine.Split(' ');
        string commandName = tokens[0];
        string[] args = tokens.Skip(1).ToArray();

        // Recherche de la commande correspondante
        if (TryGetCommandInfo(commandName, out CommandInfo commandInfo))
        {
            // Vérification du nombre d'arguments
            int expectedArgsCount = commandInfo.MethodInfo.GetParameters().Length;
            int actualArgsCount = args.Length;

            if (actualArgsCount != expectedArgsCount)
            {
                CommandConsole.LogError($"Wrong number of arguments for command <i>{commandName}</i>. (Expected {expectedArgsCount} but got {actualArgsCount}.)");
                return;
            }

            // Conversion des arguments
            object[] convertedArgs = new object[expectedArgsCount];
            for (int i = 0; i < expectedArgsCount; i++)
            {
                Type paramType = commandInfo.MethodInfo.GetParameters()[i].ParameterType;
                string arg = args[i];
                object convertedArg = null;
                try
                {
                    if (paramType.IsEnum)
                        convertedArg = Enum.Parse(paramType, arg);
                    else
                        convertedArg = Convert.ChangeType(arg, paramType);
                }
                catch (Exception e)
                {
                    CommandConsole.LogError($"Failed to convert argument <i>{arg}</i> to type <i>{paramType.Name}</i>. (Error message: <i>{e.Message}</i>)");
                    return;
                }
                convertedArgs[i] = convertedArg;
            }

            // Exécution de la commande

            if (commandInfo.MethodInfo.IsStatic)
            {
                try
                {
                    var result = commandInfo.MethodInfo.Invoke(null, convertedArgs);

                    if (commandInfo.DisplaySuccessMessage)
                        CommandConsole.LogSuccess($"Successfully executed static command <i>{commandName}</i>.");
                    if (result != null)
                        CommandConsole.Log(result.ToString());
                }
                catch (Exception e)
                {
                    CommandConsole.LogError($"Failed to execute static command <i>{commandName}</i>. <color=#AAAAAA>(Error message: <i>{e}</i>)");
                    return;
                }
            }
            else
            {
                StringBuilder results = new StringBuilder();
                bool isThereAResult = false;
                foreach (object instance in commandInfo.Instances)
                {
                    try
                    {
                        var result = commandInfo.MethodInfo.Invoke(instance, convertedArgs);
                        if (result != null)
                        {
                            isThereAResult = true;
                            results.AppendLine(result.ToString());
                        }
                        else
                        {
                            results.AppendLine("null");
                        }
                    }
                    catch (Exception e)
                    {
                        CommandConsole.LogError($"Failed to execute non-static command <i>{commandName}</i> in one of its {commandInfo.Instances.Length} instance(s). <color=#AAAAAA>(Error message: <i>{e}</i>)");
                        return;
                    }
                }

                if (commandInfo.DisplaySuccessMessage)
                    CommandConsole.LogSuccess($"Successfully executed non-static command <i>{commandName}</i>.");
                if (isThereAResult)
                    CommandConsole.Log(results.ToString());
            }
        }
        else
        {
            CommandConsole.LogError($"Unknown command: <i>{commandName}</i>");
        }
    }

    public static bool TryGetCommandInfo(string commandName, out CommandInfo commandInfo)
    {
        return _commands.TryGetValue(commandName, out commandInfo);
    }

    public static string[] GetCommandNames()
    {
        return _commands.Keys.ToArray();
    }

    [Command("help", "Display this help page.", false)]
    static string HelpCommand()
    {
        CommandConsole.LogSuccess("\n--- Help ---");

        StringBuilder sb = new StringBuilder();

        foreach (KeyValuePair<string, CommandInfo> command in _commands)
        {
            if (command.Key == "help")
                continue;

            sb.Append(command.Key + ' ');
            foreach (var parameter in command.Value.MethodInfo.GetParameters())
            {
                sb.Append($"<{parameter.ParameterType.Name} {parameter.Name}> ");
            }
            if (!string.IsNullOrWhiteSpace(command.Value.Description))
                sb.Append($"<i>\u2192 {command.Value.Description}</i>");

            sb.Append('\n');
        }

        return sb.ToString();
    }
}

public class CommandInfo
{
    public MethodInfo MethodInfo { get; private set; }
    public object[] Instances { get; private set; } // Instances of the class that has the method if it's non-static
    public string Description { get; private set; }
    public bool DisplaySuccessMessage { get; private set; }

    public CommandInfo(MethodInfo methodInfo, object[] instances, string description, bool displaySuccessMessage)
    {
        this.MethodInfo = methodInfo;
        this.Instances = instances;
        Description = description;
        DisplaySuccessMessage = displaySuccessMessage;
    }
}
