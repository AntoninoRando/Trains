using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
    The inheritance from Control is required to acquire the mouse position.
*/
public partial class Stage : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] Sprite2D background;
    [Export] Node2D pathsContainer;
    [Export] TrainsSpawner trainsSpawner;
    #endregion -----------------------------------------------------------------



    #region PUBLIC PROPERTIES --------------------------------------------------
    public Sprite2D Background => background;
    public Node2D PathsContainer => pathsContainer;
    public TrainsSpawner TrainsSpawner => trainsSpawner;
    #endregion -----------------------------------------------------------------



    private readonly List<(PathNode2D, string)> paths = [];
    const int PATHS_LIMIT = 5;
    PackedScene keyLabel = GD.Load<PackedScene>("res://Scenes/OfUI/Pedal.tscn");

    readonly List<Pedal> keyLabels = [];
    readonly Queue<string> labelQueue = new();
    bool spawningLabel = false;
    readonly ProximityDetection proximityDetection = new();
    readonly List<Train> trains = [];
    Train trainOnFocus;
    Train winningTrain = null;



    #region EVENTS -------------------------------------------------------------
    public event Action<string> KeyRegistered;
    public event Action Bump;
    public event Action<Train> Completed;
    #endregion -----------------------------------------------------------------



    public override void _Ready()
    {
        trainsSpawner.SpawnedTrain += RegisterTrain;
        proximityDetection.HoverLimit = 100;
    }

    public void Begin(Train carryoverTrain = null)
    {
        trainsSpawner.StartStage();

        // If there's a carryover train from the previous stage, assign it to a new path
        if (carryoverTrain != null)
        {
            AssignCarryoverTrain(carryoverTrain);
        }
    }

    void AssignCarryoverTrain(Train train)
    {
        // Load a new path for the carryover train
        PackedScene newPathScene = GD.Load<PackedScene>("res:///Assets/TrainsPaths/0001.tscn");
        PathNode2D newPath = newPathScene.Instantiate<PathNode2D>();

        // Add the path to the stage
        PathsContainer.AddChild(newPath);

        // Move the train to the new path
        newPath.PathModel.AddTrain(train);

        // Register the train with the new path
        RegisterTrain(train, newPath);
    }

    public override void _Process(double delta)
    {
        var clickPosition = GetGlobalMousePosition();
        var trainNodes = trains.Select(t => t.GetView<TrainNode2D>()).Where(tn => tn != null);
        proximityDetection.Update(delta, clickPosition, trainNodes);

        if (paths.Count == 0) return;

        /*
            Sprint with keys.
        */
        foreach (var item in paths)
        {
            (var pathNode, var actionKey) = item;
            if (Input.IsActionPressed(actionKey)) pathNode.PathModel.Sprint();
            else if (pathNode.PathModel.IsSprinting) pathNode.PathModel.StopSprint();
        }

        /*
            Sprint with actions.
        */
        /// Detect when action is first pressed (clicked)
        if (Input.IsActionJustPressed("speed_focused_train"))
        {
            if (proximityDetection.Hovered is TrainNode2D trainNode)
            {
                trainOnFocus = trainNode.TrainModel;
                trainOnFocus.Path.Sprint();
            }
        }
        // If action is released, stop sprinting and clear focus
        else if (Input.IsActionJustReleased("speed_focused_train"))
        {
            trainOnFocus?.Path.StopSprint();
            trainOnFocus = null;
        }
        // While action is held down, keep the focused train sprinting
        else if (Input.IsActionPressed("speed_focused_train") && trainOnFocus != null)
        {
            trainOnFocus.Path.Sprint();
        }
    }

    void RegisterTrain(Train train, PathNode2D pathNode)
    {
        if (paths.Count >= PATHS_LIMIT) return;

        var n = paths.Count + 1;
        var action_key = $"train_{n}";
        KeyRegistered?.Invoke(action_key);
        paths.Add((pathNode, action_key));
        trains.Add(train);

        var trainNode = train.GetView<TrainNode2D>();
        var area = trainNode.GetNode<TrainArea>("Area");
        area.BumpedTrain += () => Bump?.Invoke();
        pathNode.End.TrainArrived += OnTrainArrived;

        labelQueue.Enqueue(action_key);
        TrySpawnLabel(pathNode.PathModel);
    }

    public void StopTrains()
    {
        paths.ForEach(x => x.Item1.PathModel.BaseSpeed = 0);
    }

    void OnTrainArrived(Train train)
    {
        // Only trigger completion once for the first train
        if (winningTrain != null) return;

        winningTrain = train;
        StopTrains();
        Completed?.Invoke(train);
    }

    void ClearPaths(Train keepTrain = null)
    {
        foreach (var (pathNode, _) in paths)
        {
            // Skip the path that contains the winning train
            if (keepTrain != null && pathNode.PathModel.Trains.Contains(keepTrain))
            {
                continue;
            }
            pathNode.QueueFree();
        }
        paths.Clear();
        trains.Clear();

        // If we're keeping a train, re-add it to the trains list
        if (keepTrain != null)
        {
            trains.Add(keepTrain);
        }

        foreach (var label in keyLabels)
        {
            label.QueueFree();
        }
        keyLabels.Clear();
        labelQueue.Clear();
    }

    async void TrySpawnLabel(Path path)
    {
        var action = labelQueue.Dequeue();
        var label = keyLabel.Instantiate<Pedal>();
        label.AssignToPath(path);
        label.ShowKey(action);
        AddChild(label);
        keyLabels.Add(label);

        var viewport = GetViewport().GetVisibleRect();
        const float size = 40f;
        const float spacing = 50f;
        var startY = viewport.Size.Y;
        var finalY = viewport.Size.Y - size - 50;

        // start at bottom center
        label.Position = new Vector2(viewport.Size.X / 2 - size / 2, startY);

        // final horizontal positions for all labels
        var totalWidth = keyLabels.Count * (size + spacing) - spacing;
        var left = viewport.Size.X / 2 - totalWidth / 2;

        for (int i = 0; i < keyLabels.Count; i++)
        {
            var targetX = left + i * (size + spacing);
            var tween = CreateTween();
            if (keyLabels[i] == label)
            {
                tween.TweenProperty(keyLabels[i], "position", new Vector2(targetX, finalY), 0.3);
                await ToSignal(tween, Tween.SignalName.Finished);
            }
            else
            {
                tween.TweenProperty(keyLabels[i], "position:x", targetX, 0.3);
            }
        }
    }
}