using System;
using System.Collections.Generic;
using System.Linq;

public static class CommandAutoCompleter
{
    public static IEnumerable<string> GetAutoCompletion(string input)
    {
        if (string.IsNullOrEmpty(input)) 
            yield break;

        string[] tokens = input.Split(' ');
        string firstToken = tokens[0];
        List<string> otherTokens = tokens.Skip(1).ToList();

        // autocomplete the command name
        if (otherTokens.Count == 0)
        {
            foreach (string commandName in CommandManager.GetCommandNames())
            {
                if (commandName.StartsWith(firstToken))
                {
                    yield return commandName;
                }
            }
        }

        // autocomplete the parameters
        else
        {
            if (!CommandManager.TryGetCommandInfo(firstToken, out CommandInfo commandInfo))
                yield break;

            int tokenIndex = otherTokens.Count - 1;
            string lastToken = otherTokens.Last();

            if (tokenIndex >= commandInfo.MethodInfo.GetParameters().Length)
                yield break;

            string parameterName = commandInfo.MethodInfo.GetParameters()[tokenIndex].Name;
            Type parameterType = commandInfo.MethodInfo.GetParameters()[tokenIndex].ParameterType;

            foreach (string possibleParameterValue in GetParameterPossibilities(parameterName, parameterType))
            {
                if (possibleParameterValue.ToLower().StartsWith(lastToken.ToLower()))
                {
                    otherTokens[tokenIndex] = possibleParameterValue;
                    yield return firstToken + " " + string.Join(" ", otherTokens);
                }
            }
        }
    }

    static IEnumerable<string> GetParameterPossibilities(string parameterName, Type parameterType)
    {
        if (parameterType.IsEnum)
        {
            return Enum.GetNames(parameterType);
        }

        if (parameterType == typeof(bool))
        {
            return new string[] { "true", "false"};
        }

        return new string[1] { $"<{parameterType.Name} {parameterName}>" }; // no example values for other types

    }
}
