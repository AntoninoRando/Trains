using System;
using System.Collections.Generic;

public class Train : IMouldable
{
    public Path Path;
    public float BaseSpeed = 0.01f;

    #region WAGONS -------------------------------------------------------------
    readonly List<Wagon> wagons = [];

    /// <summary>Wagons currently coupled to this train's tail (front to back).</summary>
    public IReadOnlyList<Wagon> Wagons => wagons;

    /// <summary>Raised when a new wagon is coupled. The view layer listens to
    /// this to spawn the trailing car that rides the path behind the loco.</summary>
    public event Action<Wagon> WagonAttached;

    /// <summary>Couples a wagon to the tail, lengthening the train.</summary>
    public void AttachWagon(Wagon wagon)
    {
        if (wagon == null) return;
        wagons.Add(wagon);
        WagonAttached?.Invoke(wagon);
    }

    /// <summary>Drops every wagon (e.g. when the train starts a fresh stage).</summary>
    public void ClearWagons() => wagons.Clear();
    #endregion -----------------------------------------------------------------
}
