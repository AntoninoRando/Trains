using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
    The inheritance from Control is required to acquire the mouse position.
*/
public partial class StageNode2D : Node2D
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



    readonly Stage stage = new();
    public Stage StageModel => stage;

    private readonly List<(PathNode2D, string)> pathNodes = [];
    PackedScene keyLabel = GD.Load<PackedScene>("res://Scenes/OfUI/Pedal.tscn");

    readonly List<Pedal> keyLabels = [];
    readonly Queue<string> labelQueue = new();
    readonly ProximityDetection proximityDetection = new();



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        ((IMouldable)stage).SetView(this);
        trainsSpawner.SpawnedTrain += RegisterTrain;
        proximityDetection.HoverLimit = 100;
        stage.KeyRegistered += OnKeyRegistered;
        stage.Bump += OnBump;
        stage.Completed += OnCompleted;
    }

    public override void _Process(double delta)
    {
        var clickPosition = GetGlobalMousePosition();
        var trainNodes = stage.Trains.Select(t => ((IMouldable)t).GetView<TrainNode2D>()).Where(tn => tn != null);
        proximityDetection.Update(delta, clickPosition, trainNodes);

        if (stage.Paths.Count == 0) return;

        /*
            Sprint with keys.
        */
        foreach (var (path, actionKey) in stage.Paths)
        {
            if (Input.IsActionPressed(actionKey)) path.Sprint();
            else if (path.IsSprinting) path.StopSprint();
        }

        /*
            Sprint with actions.
        */
        /// Detect when action is first pressed (clicked)
        if (Input.IsActionJustPressed("speed_focused_train"))
        {
            if (proximityDetection.Hovered is TrainNode2D trainNode)
            {
                stage.TrainOnFocus = trainNode.TrainModel;
                stage.TrainOnFocus.Path.Sprint();
            }
        }
        // If action is released, stop sprinting and clear focus
        else if (Input.IsActionJustReleased("speed_focused_train"))
        {
            stage.TrainOnFocus?.Path.StopSprint();
            stage.TrainOnFocus = null;
        }
        // While action is held down, keep the focused train sprinting
        else if (Input.IsActionPressed("speed_focused_train") && stage.TrainOnFocus != null)
        {
            stage.TrainOnFocus.Path.Sprint();
        }
    }
    #endregion -----------------------------------------------------------------



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

    void RegisterTrain(Train train, PathNode2D pathNode)
    {
        if (!stage.CanRegisterPath) return;

        var n = stage.Paths.Count + 1;
        var action_key = $"train_{n}";

        // Register in logic layer
        stage.RegisterTrain(train, pathNode.PathModel, action_key);

        // Store UI reference
        pathNodes.Add((pathNode, action_key));

        var trainNode = ((IMouldable)train).GetView<TrainNode2D>();
        var area = trainNode.GetNode<TrainArea>("Area");
        area.BumpedTrain += () => stage.TriggerBump();
        pathNode.End.TrainArrived += stage.OnTrainArrived;

        labelQueue.Enqueue(action_key);
        TrySpawnLabel(pathNode.PathModel);
    }

    void OnKeyRegistered(string actionKey)
    {
        // Handle any UI updates when a key is registered
    }

    void OnBump()
    {
        // UI-specific bump handling can go here if needed
    }

    void OnCompleted(Train train)
    {
        // UI-specific completion handling can go here if needed
    }

    void ClearPaths(Train keepTrain = null)
    {
        foreach (var (pathNode, _) in pathNodes)
        {
            // Skip the path that contains the winning train
            if (keepTrain != null && pathNode.PathModel.Trains.Contains(keepTrain))
            {
                continue;
            }
            pathNode.QueueFree();
        }
        pathNodes.Clear();
        stage.ClearPaths(keepTrain);

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
