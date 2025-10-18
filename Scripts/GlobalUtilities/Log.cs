using Godot;

public static class Log
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Info(string message)
    {
        GD.Print("[INFO] " + message);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Warning(string message)
    {
        GD.Print("[WARNING] " + message);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Error(string message)
    {
        GD.PrintErr("[ERROR] " + message);
    }
}