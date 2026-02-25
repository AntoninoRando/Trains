using System;

public static class Game
{
    public static readonly Random RNG = new();
    public static object Context { get; private set; }
    public static MatchOrchestrator MatchOrchestrator { get; } = new();



    #region EVENTS ─────────────────────────────────────────────────────────────
    public static event Action<object> NewContext;
    #endregion ─────────────────────────────────────────────────────────────────



    public static void ChangeContext(object newContext)
    {
        Context = newContext;
        NewContext?.Invoke(newContext);
    }
}