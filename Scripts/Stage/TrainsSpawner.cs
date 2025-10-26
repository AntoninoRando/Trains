using System;
using System.Collections.Generic;
using Godot;

public partial class TrainsSpawner : Node
{
    [Export] Node2D SpawnedContainer;



    public event Action<Train, PathNode2D> SpawnedTrain;


    private readonly List<TrainData> TrainsData = [];
    private record TrainData(PackedScene Train, PackedScene Path, double Delay);

    PackedScene train = GD.Load<PackedScene>("res:///Assets/OfTrains/TrainScene.tscn");
    PackedScene path0001 = GD.Load<PackedScene>("res:///Assets/TrainsPaths/0001.tscn");
    PackedScene path0002 = GD.Load<PackedScene>("res:///Assets/TrainsPaths/0002.tscn");

    public override void _Ready()
    {
        base._Ready();
    }

    public void StartStage()
    {
        TrainsData.Clear();
        Enqueue(train, path0001, 0);
        Enqueue(train, path0002, 0);
    }

    public void Enqueue(PackedScene train, PackedScene path, double delay)
    {
        TrainsData.Add(new TrainData(train, path, delay));
    }

    public override void _Process(double delta)
    {
        for (int i = TrainsData.Count - 1; i >= 0; i--)
        {
            var data = TrainsData[i];
            var updated = data with { Delay = data.Delay - delta };
            if (updated.Delay <= 0)
            {
                Spawn(updated);
                TrainsData.RemoveAt(i);
            }
            else
            {
                TrainsData[i] = updated;
            }
        }
    }

    private void Spawn(TrainData data)
    {
        var trainNode = data.Train.Instantiate<TrainNode2D>();
        var pathNode = data.Path.Instantiate<PathNode2D>();

        SpawnedContainer.AddChild(trainNode);
        SpawnedContainer.AddChild(pathNode);

        pathNode.PathModel.AddTrain(trainNode.TrainModel);
        SpawnedTrain?.Invoke(trainNode.TrainModel, pathNode);
    }
}