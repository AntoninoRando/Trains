using System;
using Godot;

public partial class EndPathArea : Area2D
{
    public event Action<Train> TrainArrived;

    public override void _Ready()
    {
        AreaEntered += OnTrainEnter;
    }

    void OnTrainEnter(Area2D area)
    {
        if (area is not TrainArea trainArea) return;

        // Only the locomotive completes the path; a trailing wagon's area (whose
        // parent is a WagonNode2D, not a TrainNode2D) crossing the line is ignored.
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode == null) return;

        var train = trainNode.TrainModel;

        Log.Info($"Train {trainNode.Name} has arrived at the end path area.");
        TrainArrived?.Invoke(train);
    }
}
