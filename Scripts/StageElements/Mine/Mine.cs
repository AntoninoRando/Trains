using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents a mine area where trains stop to mine resources.
/// </summary>
public class Mine
{
    #region FIELDS -------------------------------------------------------------
    public double MiningDuration = 3.0;
    // Track which trains are currently mining
    private Dictionary<Train, double> miningTrains = [];
    // Store original speeds to restore them later
    private Dictionary<Train, double> originalSpeeds = [];
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    public void Update(double delta)
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



    #region PRIVATE METHODS ----------------------------------------------------
    public void StartMining(Train train)
    {
        if (train == null || train.Path == null) return;
        if (miningTrains.ContainsKey(train)) return;

        // Store the original speed
        originalSpeeds[train] = train.Path.Speed;

        // Stop the train
        train.Path.AddSpeedLayer("mine", 200, speed => 0);

        // Add to mining queue with full duration
        miningTrains[train] = MiningDuration;

        GD.Print($"Train started mining at mine. Duration: {MiningDuration}s");
    }

    void ReleaseTrain(Train train)
    {
        if (train.Path == null || !originalSpeeds.ContainsKey(train))
        {
            miningTrains.Remove(train);
            return;
        }

        // Restore the original speed
        train.Path.RemoveSpeedLayer("mine");

        // Clean up tracking dictionaries
        miningTrains.Remove(train);
        originalSpeeds.Remove(train);

        GD.Print("Train finished mining and resumed movement");
    }
    #endregion -----------------------------------------------------------------
}
