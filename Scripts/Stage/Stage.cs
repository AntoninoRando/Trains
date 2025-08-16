using System.Collections.Generic;
using Godot;

public partial class Stage : Node
{
    [Export] TrainsSpawner trainsSpawner;
    private readonly List<(Path, string)> paths = [];
    const int PATHS_LIMIT = 5;

    public override void _Ready()
    {
        trainsSpawner.SpawnedTrain += RegisterTrain;
    }

    public override void _Input(InputEvent @event)
    {
        if (paths.Count == 0) return;

        foreach (var item in paths)
        {
            (var path, var actionKey) = item;
            if (Input.IsActionPressed(actionKey)) path.Sprint();
            else path.StopSprint();
        }
    }

    void RegisterTrain(Train train, Path path)
    {
        if (paths.Count >= PATHS_LIMIT) return;

        var action_key = $"train_{paths.Count+1}";
        paths.Add((path, action_key));
    }
}