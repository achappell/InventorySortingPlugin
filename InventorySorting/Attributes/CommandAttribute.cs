using System;

namespace InventorySorting.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }
        public string? SecondaryCommand { get; }

        public CommandAttribute(string command, string? secondaryCommand = null)
        {
            Command = command;
            SecondaryCommand = secondaryCommand;
        }
    }
}
