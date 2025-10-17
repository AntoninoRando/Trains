using Godot;
using System.Collections.Generic;

public partial class Mine : Area2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] public double MiningDuration = 3.0; // seconds
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    // Track which trains are currently mining
    private Dictionary<Train, double> miningTrains = [];
    // Store original speeds to restore them later
    private Dictionary<Train, double> originalSpeeds = [];
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        // Update mining timers for all trains currently mining
        var trainsToRelease = new List<Train>();

        foreach (var kvp in miningTrains)
        {
            Train train = kvp.Key;
            double remainingTime = kvp.Value - delta;

            if (remainingTime <= 0)
            {
                // Mining complete, release the train
                trainsToRelease.Add(train);
            }
            else
            {
                // Update remaining time
                miningTrains[train] = remainingTime;
            }
        }

        // Release trains that finished mining
        foreach (var train in trainsToRelease)
        {
            ReleaseTrain(train);
        }
    }
    #endregion -----------------------------------------------------------------

    

    private void OnAreaEntered(Area2D area)
    {
        // Check if a train entered the mine area
        if (area is TrainArea trainArea)
        {
            Train train = trainArea.GetParent<Train>();

            // Only start mining if this train isn't already mining
            if (train != null && !miningTrains.ContainsKey(train))
            {
                StartMining(train);
            }
        }
    }

    private void StartMining(Train train)
    {
        if (train.Path == null) return;

        // Store the original speed
        originalSpeeds[train] = train.Path.Speed;

        // Stop the train
        train.Path.Speed = 0;

        // Add to mining queue with full duration
        miningTrains[train] = MiningDuration;

        GD.Print($"Train started mining at mine. Duration: {MiningDuration}s");
    }

    private void ReleaseTrain(Train train)
    {
        if (train.Path == null || !originalSpeeds.ContainsKey(train))
        {
            miningTrains.Remove(train);
            return;
        }

        // Restore the original speed
        train.Path.Speed = originalSpeeds[train];

        // Clean up tracking dictionaries
        miningTrains.Remove(train);
        originalSpeeds.Remove(train);

        GD.Print("Train finished mining and resumed movement");
    }
}
