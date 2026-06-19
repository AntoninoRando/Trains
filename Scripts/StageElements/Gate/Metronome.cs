using Godot;
using System;

/// <summary>
/// The game-wide beat source for rhythmic stage elements (the <see cref="Gate"/>s).
/// A single instance lives under the scene-tree root for the whole session;
/// gates fetch it via <see cref="Ensure"/> and subscribe to <see cref="Ticked"/>,
/// so every gate on screen flips on the very same beat.
///
/// While at least one gate is active the metronome advances and plays an audible
/// click; with no gates it stays silent and frozen, then rewinds to a clean
/// downbeat when the next gate appears. It is created lazily (no autoload entry
/// in project.godot required) the first time a gate asks for it.
/// </summary>
public partial class Metronome : Node
{
    #region SINGLETON ----------------------------------------------------------
    public static Metronome Instance { get; private set; }

    /// <summary>Returns the shared metronome, creating it under the scene root on
    /// first use. Safe to call from any node's <c>_Ready</c>.</summary>
    public static Metronome Ensure(Node context)
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance)) return Instance;

        var m = new Metronome { Name = "Metronome" };
        Instance = m;
        // Park it on the root so it outlives individual stages; deferred so we
        // never mutate the tree while another node is still setting up.
        context.GetTree().Root.CallDeferred(Node.MethodName.AddChild, m);
        return m;
    }
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    readonly BeatClock clock = new();
    AudioStreamPlayer player;
    int activeGates;
    #endregion -----------------------------------------------------------------



    #region PROPERTIES ---------------------------------------------------------
    /// <summary>The current global beat index (shared by every gate).</summary>
    public long Tick => clock.Tick;

    /// <summary>Tempo, in seconds per beat.</summary>
    public double SecondsPerTick
    {
        get => clock.SecondsPerTick;
        set => clock.SecondsPerTick = value;
    }
    #endregion -----------------------------------------------------------------



    #region EVENTS -------------------------------------------------------------
    /// <summary>Forwarded from the inner clock: fires once per beat with its index.</summary>
    public event Action<long> Ticked;
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        clock.Ticked += OnClockTicked;

        player = new AudioStreamPlayer { Name = "Tick" };
        AddChild(player);

        // The click is optional: if the asset hasn't been imported yet the gate
        // still works, just silently. (GameAssets warns once and returns null.)
        var stream = GameAssets.GateTick();
        if (stream != null) player.Stream = stream;
    }

    public override void _Process(double delta)
    {
        if (activeGates <= 0) return; // silent + frozen when no gate needs a beat
        clock.Update(delta);
    }
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    /// <summary>A gate announces it needs the beat. The first active gate rewinds
    /// the clock so the stage's gates start on a clean downbeat.</summary>
    public void AddGate()
    {
        if (activeGates == 0) clock.Reset();
        activeGates++;
    }

    /// <summary>A gate stops needing the beat (left the tree / the stage ended).</summary>
    public void RemoveGate()
    {
        activeGates = Math.Max(0, activeGates - 1);
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    void OnClockTicked(long tickIndex)
    {
        if (player?.Stream != null) player.Play();
        Ticked?.Invoke(tickIndex);
    }
    #endregion -----------------------------------------------------------------
}
