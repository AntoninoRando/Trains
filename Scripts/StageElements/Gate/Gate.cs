using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A rhythmic barrier — the on/off "beat block" of this game. A gate is either
/// OPEN (trains roll through) or CLOSED (a train that reaches it stops there),
/// and it flips between the two on the shared <see cref="Metronome"/>'s beat:
/// it holds the <see cref="StartsOpen"/> phase for <see cref="OpenBeats"/> beats,
/// then the other phase for <see cref="ClosedBeats"/> beats, and repeats.
///
/// The state is computed as a pure function of the global beat index
/// (<see cref="StateAt"/>), so any number of gates — no matter when they
/// spawn — stay perfectly in step, and two gates that differ only in
/// <see cref="StartsOpen"/> form a clean alternating pattern.
///
/// A train caught at a closed gate is halted with a path speed-layer (exactly
/// how <see cref="Mine"/> stops a train) and released the instant the gate opens.
/// </summary>
public class Gate
{
    #region FIELDS -------------------------------------------------------------
    /// <summary>Beats the gate spends in its <see cref="StartsOpen"/> phase.</summary>
    public int OpenBeats = 2;

    /// <summary>Beats the gate spends in the opposite phase before flipping back.</summary>
    public int ClosedBeats = 2;

    /// <summary>The phase the gate holds first (and during every "open" window).
    /// Two gates that differ here run in opposite phase.</summary>
    public bool StartsOpen = true;

    // A unique speed-layer id per gate, so several gates (and the Mine) never
    // clobber one another's layer on a shared path.
    static int nextId;
    readonly string layerId = $"gate_{++nextId}";

    bool isOpen = true;
    readonly HashSet<Train> trainsAtGate = [];
    readonly HashSet<Train> stopped = [];
    #endregion -----------------------------------------------------------------



    #region PROPERTIES ---------------------------------------------------------
    public bool IsOpen => isOpen;
    #endregion -----------------------------------------------------------------



    #region EVENTS -------------------------------------------------------------
    /// <summary>Raised whenever the gate actually changes state (true = open).</summary>
    public event Action<bool> StateChanged;
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    public Gate(bool startsOpen = true)
    {
        StartsOpen = startsOpen;
        isOpen = startsOpen;
    }

    /// <summary>Snaps the gate to the state implied by the given beat without
    /// firing <see cref="StateChanged"/> — used at spawn to land in phase.</summary>
    public void SyncToTick(long tickIndex) => SetOpen(StateAt(tickIndex), notify: false);

    /// <summary>Drives the gate from the shared metronome's beat.</summary>
    public void OnTick(long tickIndex) => SetOpen(StateAt(tickIndex), notify: true);

    /// <summary>A train's body reached the gate. If the gate is shut, the train
    /// stops here until the next opening.</summary>
    public void TrainEntered(Train train)
    {
        if (train == null) return;
        trainsAtGate.Add(train);
        if (!isOpen) Stop(train);
    }

    /// <summary>A train has cleared the gate area.</summary>
    public void TrainExited(Train train)
    {
        if (train == null) return;
        trainsAtGate.Remove(train);
        Release(train);
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    /// <summary>Open/closed purely as a function of the beat index, so every gate
    /// is consistent regardless of when it started listening.</summary>
    bool StateAt(long tickIndex)
    {
        int open = Math.Max(0, OpenBeats);
        int closed = Math.Max(0, ClosedBeats);
        int period = open + closed;
        if (period <= 0) return StartsOpen; // degenerate: never toggles

        long pos = ((tickIndex % period) + period) % period; // 0..period-1, never negative
        bool inOpenPhase = pos < open;
        return StartsOpen ? inOpenPhase : !inOpenPhase;
    }

    void SetOpen(bool open, bool notify)
    {
        if (open == isOpen) return;
        isOpen = open;

        if (isOpen) ReleaseAll();
        else StopAll();

        if (notify) StateChanged?.Invoke(isOpen);
    }

    void StopAll()
    {
        foreach (var train in trainsAtGate) Stop(train);
    }

    void ReleaseAll()
    {
        foreach (var train in stopped.ToList()) Release(train);
    }

    void Stop(Train train)
    {
        if (train?.Path == null) return;
        if (stopped.Add(train))
            train.Path.AddSpeedLayer(layerId, 200, _ => 0);
    }

    void Release(Train train)
    {
        if (stopped.Remove(train))
            train.Path?.RemoveSpeedLayer(layerId);
    }
    #endregion -----------------------------------------------------------------
}
