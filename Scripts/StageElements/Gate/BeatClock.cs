using System;

/// <summary>
/// A pure, engine-agnostic metronome. Fed elapsed time through <see cref="Update"/>
/// it counts whole "ticks" (beats) of <see cref="SecondsPerTick"/> and raises
/// <see cref="Ticked"/> once per beat boundary crossed, carrying the running
/// tick index. The audible click and the rhythmic <see cref="Gate"/>s are both
/// driven from this single source, so they stay perfectly in sync.
/// </summary>
public class BeatClock
{
    #region FIELDS -------------------------------------------------------------
    /// <summary>Seconds between beats (tempo). 0.6s ≈ 100 BPM.</summary>
    public double SecondsPerTick = 0.6;

    double accumulator;
    long tick;
    #endregion -----------------------------------------------------------------



    #region PROPERTIES ---------------------------------------------------------
    /// <summary>How many beats have elapsed since the clock last (re)started.</summary>
    public long Tick => tick;
    #endregion -----------------------------------------------------------------



    #region EVENTS -------------------------------------------------------------
    /// <summary>Raised once for every beat boundary crossed, with the new tick index.</summary>
    public event Action<long> Ticked;
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    /// <summary>Advances the clock by <paramref name="delta"/> seconds, emitting a
    /// <see cref="Ticked"/> for each beat crossed (safe across large deltas).</summary>
    public void Update(double delta)
    {
        if (SecondsPerTick <= 0 || delta <= 0) return;

        accumulator += delta;
        while (accumulator >= SecondsPerTick)
        {
            accumulator -= SecondsPerTick;
            tick++;
            Ticked?.Invoke(tick);
        }
    }

    /// <summary>Rewinds to beat zero, so a fresh rhythmic stage starts on a clean
    /// downbeat.</summary>
    public void Reset()
    {
        accumulator = 0;
        tick = 0;
    }
    #endregion -----------------------------------------------------------------
}
