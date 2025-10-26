using System;
using System.Collections.Generic;
using System.Linq;

public class Path
{
    #region FIELDS -------------------------------------------------------------
    public double BaseSpeed = 0.05;
    public double SprintMultiplier = 2;
    readonly List<SpeedLayer> speedLayers = [];
    bool onSprint;
    public bool IsSprinting => onSprint;
    string assignedAction;
    readonly List<Train> trains = [];
    public IReadOnlyList<Train> Trains => trains;
    #endregion -----------------------------------------------------------------
    


    #region PROPERTIES ---------------------------------------------------------
    public double Speed
    {
        get
        {
            double speed = BaseSpeed;
            foreach (var layer in speedLayers.OrderBy(l => l.Priority))
            {
                speed = layer.Transform(speed);
            }
            return speed;
        }
    }
    #endregion -----------------------------------------------------------------



    #region EVENTS -------------------------------------------------------------
    public event Action<Train> TrainAdded;
    public event Action<Train> TrainRemoved;
    public event Action SprintStarted;
    public event Action SprintStopped;
    #endregion -----------------------------------------------------------------



    public void AddSpeedLayer(string id, int priority, Func<double, double> transform)
    {
        RemoveSpeedLayer(id); // Remove existing layer with same id if present
        speedLayers.Add(new SpeedLayer(id, priority, transform));
    }

    public void RemoveSpeedLayer(string id)
    {
        speedLayers.RemoveAll(layer => layer.Id == id);
    }

    public void Sprint()
    {
        if (onSprint) return;
        AddSpeedLayer("sprint", 100, speed => speed * SprintMultiplier);
        onSprint = true;
        SprintStarted?.Invoke();
    }

    public void StopSprint()
    {
        if (!onSprint) return;
        RemoveSpeedLayer("sprint");
        onSprint = false;
        SprintStopped?.Invoke();
    }


    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain(Train train)
    {
        trains.Add(train);
        train.Path = this;
        TrainAdded?.Invoke(train);
    }

    public void RemoveTrain(Train train)
    {
        if (trains.Remove(train))
        {
            train.Path = null;
            TrainRemoved?.Invoke(train);
        }
    }
}
