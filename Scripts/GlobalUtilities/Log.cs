using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public static class Log
{
    [Conditional("DEBUG")]
    public static void Info(string message, Dictionary<string, object> meta = null)
    {
        if (meta != null)
        {
            string metaString = "";
            foreach (var kvp in meta)
            {
                metaString += $"{kvp.Key}: {kvp.Value}, ";
            }
            GD.Print("[INFO] " + message + " [Meta: " + metaString.TrimEnd(',', ' ') + "]");
        }
        else
        {
            GD.Print("[INFO] " + message);
        }
    }

    [Conditional("DEBUG")]
    public static void Warning(string message)
    {
        GD.Print("[WARNING] " + message);
    }

    [Conditional("DEBUG")]
    public static void Error(string message)
    {
        GD.PrintErr("[ERROR] " + message);
    }
}