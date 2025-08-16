using System;
using Godot;

public partial class EndPathArea : Area2D
{
    public event Action TrainArrived;

    public override void _Ready()
    {
        AreaEntered += OnTrainEnter;
    }

    void OnTrainEnter(Area2D area)
    {
        if (area is not TrainArea)
            return;

        GD.Print("Path reached the end!");
        TrainArrived?.Invoke();
    }
}
