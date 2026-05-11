using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string CommandName;
    public string Description;
    public bool DisplaySuccessMessage;

    public CommandAttribute(string name, string description = "", bool displaySuccessMessage = true)
    {
        CommandName = name;
        Description = description;
        DisplaySuccessMessage = displaySuccessMessage;
    }
}
