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

        // Get the train node and then its model
        var trainNode = trainArea.GetParent<TrainNode2D>();
        var train = trainNode.TrainModel;

        Log.Info($"Train {trainNode.Name} has arrived at the end path area.");
        TrainArrived?.Invoke(train);
    }
}
