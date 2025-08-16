using System.Collections.Generic;
using Godot;

public partial class Stage : Node
{
    [Export] TrainsSpawner trainsSpawner;
    private readonly List<Path> paths = [];

    public override void _Ready()
    {
        trainsSpawner.SpawnedTrain += RegisterSpawnedTrain;
    }

    public void RegisterSpawnedTrain(Train train, Path path)
    {
        paths.Add(path);
    }
}