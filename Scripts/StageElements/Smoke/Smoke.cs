using System;
using System.Collections.Generic;

/// <summary>
/// A drifting cloud of smoke sitting on the track. Purely an obscuring hazard:
/// the cloud is drawn on top of the trains, so any train that rolls into it
/// vanishes from view and the player has to keep timing a train they can no
/// longer see. The model just records which trains are currently inside and
/// announces the crossings; the drawing and concealment live in
/// <see cref="SmokeNode2D"/>.
/// </summary>
public class Smoke
{
    #region FIELDS -------------------------------------------------------------
    readonly HashSet<Train> inside = [];
    #endregion -----------------------------------------------------------------



    #region PROPERTIES ---------------------------------------------------------
    /// <summary>Trains currently hidden within the cloud.</summary>
    public IReadOnlyCollection<Train> TrainsInside => inside;

    /// <summary>True while at least one train is concealed by the cloud.</summary>
    public bool HasTrainsInside => inside.Count > 0;
    #endregion -----------------------------------------------------------------



    #region EVENTS -------------------------------------------------------------
    /// <summary>Raised when a train first becomes hidden by this cloud.</summary>
    public event Action<Train> TrainEntered;

    /// <summary>Raised when a train clears the cloud and is visible again.</summary>
    public event Action<Train> TrainExited;
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    public void Enter(Train train)
    {
        if (train == null) return;
        if (inside.Add(train)) TrainEntered?.Invoke(train);
    }

    public void Exit(Train train)
    {
        if (train == null) return;
        if (inside.Remove(train)) TrainExited?.Invoke(train);
    }
    #endregion -----------------------------------------------------------------
}
