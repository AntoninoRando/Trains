using System;
using Godot;

public partial class TrainArea : Area2D
{
    public event Action BumpedTrain;

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    void OnAreaEntered(Area2D area)
    {
        if (area is TrainArea)
            BumpedTrain?.Invoke();
    }
}
