using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure logic class representing a stage in the game.
/// Contains game state and logic without any UI dependencies.
/// </summary>
public class Stage : IMouldable
{
    readonly List<(Path, string)> paths = [];
    readonly List<Train> trains = [];
    Train trainOnFocus;
    Train winningTrain;

    public IReadOnlyList<(Path, string)> Paths => paths;
    public IReadOnlyList<Train> Trains => trains;
    public Train TrainOnFocus { get => trainOnFocus; set => trainOnFocus = value; }
    public Train WinningTrain => winningTrain;

    const int PATHS_LIMIT = 5;

    public event Action<string> KeyRegistered;
    public event Action Bump;
    public event Action<Train> Completed;

    public bool CanRegisterPath => paths.Count < PATHS_LIMIT;

    public void RegisterTrain(Train train, Path path, string actionKey)
    {
        if (paths.Count >= PATHS_LIMIT) return;

        KeyRegistered?.Invoke(actionKey);
        paths.Add((path, actionKey));
        trains.Add(train);
    }

    public void StopTrains()
    {
        paths.ForEach(x => x.Item1.BaseSpeed = 0);
    }

    public void OnTrainArrived(Train train)
    {
        // Only trigger completion once for the first train
        if (winningTrain != null) return;

        winningTrain = train;
        StopTrains();
        Completed?.Invoke(train);
    }

    public void TriggerBump()
    {
        Bump?.Invoke();
    }

    public void ClearPaths(Train keepTrain = null)
    {
        paths.Clear();
        trains.Clear();

        // If we're keeping a train, re-add it to the trains list
        if (keepTrain != null)
        {
            trains.Add(keepTrain);
        }
    }

    public void Sprint(string actionKey)
    {
        var pathTuple = paths.FirstOrDefault(p => p.Item2 == actionKey);
        if (pathTuple.Item1 != null)
        {
            pathTuple.Item1.Sprint();
        }
    }

    public void StopSprint(string actionKey)
    {
        var pathTuple = paths.FirstOrDefault(p => p.Item2 == actionKey);
        if (pathTuple.Item1 != null && pathTuple.Item1.IsSprinting)
        {
            pathTuple.Item1.StopSprint();
        }
    }
}
