using System;
using System.Collections.Generic;
using Godot;

public partial class TrainsSpawner : Node
{
    [Export] Node2D SpawnedContainer;



    public event Action<Train, PathNode2D> SpawnedTrain;


    private readonly List<TrainData> TrainsData = [];
    private record TrainData(PackedScene Train, Curve2D Curve, double Delay);

    PackedScene train = GD.Load<PackedScene>("res:///Assets/OfTrains/TrainScene.tscn");

    public override void _Ready()
    {
        base._Ready();
    }

    /// <summary>
    /// Queues a fresh train for every generated path. When a train is carried
    /// over from the previous stage it owns the first path (index 0), so that
    /// slot is skipped here to avoid spawning a second train on it.
    /// </summary>
    public void StartStage(IReadOnlyList<Curve2D> curves, bool spawnFirstPath = true)
    {
        TrainsData.Clear();
        for (int i = 0; i < curves.Count; i++)
        {
            if (i == 0 && !spawnFirstPath) continue;
            Enqueue(train, curves[i], 0);
        }
    }

    public void Enqueue(PackedScene train, Curve2D curve, double delay)
    {
        TrainsData.Add(new TrainData(train, curve, delay));
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
        var pathNode = PathFactory.Build(data.Curve);

        SpawnedContainer.AddChild(trainNode);
        SpawnedContainer.AddChild(pathNode);

        pathNode.PathModel.AddTrain(trainNode.TrainModel);
        SpawnedTrain?.Invoke(trainNode.TrainModel, pathNode);
    }
}
