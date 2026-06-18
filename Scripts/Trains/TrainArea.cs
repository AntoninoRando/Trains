using System;
using Godot;

public partial class TrainArea : Area2D
{
    /// <summary>The train this area belongs to (loco or one of its wagons).
    /// Used to ignore overlaps between a train and its own cars.</summary>
    public Train OwnerTrain;

    public event Action BumpedTrain;

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    void OnAreaEntered(Area2D area)
    {
        if (area is not TrainArea other) return;

        // A longer train must not bump into itself: ignore overlaps between the
        // loco and its own wagons (and between wagons of the same train).
        if (OwnerTrain != null && other.OwnerTrain == OwnerTrain) return;

        BumpedTrain?.Invoke();
    }
}
