using System;
using System.Collections.Generic;
using Godot;

public partial class TrainsSpawner : Node
{
    [Export] public Node2D SpawnedContainer;



    public event Action<Train, Path> SpawnedTrain;


    public record TrainData(Train Train, Path Path, double Delay);

    private readonly List<TrainData> trainsData = [];
    public IReadOnlyList<TrainData> TrainsData => trainsData;

    PackedScene train = GD.Load<PackedScene>("res:///Assets/OfTrains/TrainScene.tscn");
    PackedScene path0001 = GD.Load<PackedScene>("res:///Scenes/TrainsPaths/0001.tscn");
    PackedScene path0002 = GD.Load<PackedScene>("res:///Scenes/TrainsPaths/0002.tscn");


    public void StartStage()
    {
        trainsData.Clear();
        Enqueue(train, path0001, 0);
        Enqueue(train, path0002, 0);
    }

    public void Enqueue(PackedScene train, PackedScene path, double delay)
    {
        var trainInstance = train.Instantiate<Train>();
        var pathInstance = path.Instantiate<Path>();
        trainsData.Add(new TrainData(trainInstance, pathInstance, delay));
    }

    public override void _Process(double delta)
    {
        for (int i = trainsData.Count - 1; i >= 0; i--)
        {
            var data = trainsData[i];
            var updated = data with { Delay = data.Delay - delta };
            if (updated.Delay <= 0)
            {
                Spawn(updated);
                trainsData.RemoveAt(i);
            }
            else
            {
                trainsData[i] = updated;
            }
        }
    }

    private void Spawn(TrainData data)
    {
        var trainInstance = data.Train;
        var pathInstance = data.Path;

        SpawnedContainer.AddChild(trainInstance);
        SpawnedContainer.AddChild(pathInstance);

        pathInstance.AddTrain(trainInstance);
        SpawnedTrain?.Invoke(trainInstance, pathInstance);
    }
}