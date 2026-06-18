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

    // Per-train WagonAttached handlers, removed when this stage leaves the tree
    // so a carried-over train doesn't keep firing into the old (freed) stage.
    readonly List<(Train Train, Action<Wagon> Handler)> wagonSubs = [];



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        ((IMouldable)stage).SetView(this);

        // Render the paths as a tiled railroad track. Added as the first child of
        // the paths container so its tiles draw beneath the trains (which live in
        // the path nodes added afterwards) yet above the stage background.
        var trackTiler = new TrackTiler { Name = "TrackTiler" };
        pathsContainer.AddChild(trackTiler);
        pathsContainer.MoveChild(trackTiler, 0);

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
        // The carryover train re-uses path 0001: don't spawn a new train on it,
        // otherwise the two trains would overlap and bump immediately.
        trainsSpawner.StartStage(spawnFirstPath: carryoverTrain == null);

        // If there's a carryover train from the previous stage, assign it to a new path
        if (carryoverTrain != null)
        {
            AssignCarryoverTrain(carryoverTrain);
        }
    }

    void AssignCarryoverTrain(Train train)
    {
        // Each stage starts fresh: drop any wagons (and their orbs) the train
        // earned last stage. Their gold was already paid out on arrival.
        train.ClearWagons();

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

        // Give this path and its train a shared identity colour (1-based slot),
        // so the track and the train that runs on it are easy to tell apart.
        var color = TrackPalette.For(n - 1);
        pathNode.TrackColor = color;

        // Register in logic layer
        stage.RegisterTrain(train, pathNode.PathModel, action_key);

        // Store UI reference
        pathNodes.Add((pathNode, action_key));

        var trainNode = ((IMouldable)train).GetView<TrainNode2D>();
        // Identity colour times the equipped livery tint (white = no change), so
        // trains stay tellable apart while wearing the purchased cosmetic.
        if (trainNode != null) trainNode.Modulate = color * PlayerProfile.TrainTint;
        var area = trainNode.GetNode<TrainArea>("Area");
        area.OwnerTrain = train;
        area.BumpedTrain += () => stage.TriggerBump();
        pathNode.End.TrainArrived += stage.OnTrainArrived;

        // Grow a tail car whenever this train picks up a Mystical Orb.
        Action<Wagon> onWagon = wagon => OnWagonAttached(train, pathNode, wagon);
        train.WagonAttached += onWagon;
        wagonSubs.Add((train, onWagon));

        labelQueue.Enqueue(action_key);
        TrySpawnLabel(pathNode.PathModel);

        // "Lucky Charm" shop unlock: drop a bonus orb partway along this path.
        if (PlayerProfile.ExtraOrb) SpawnBonusOrb(pathNode);
    }

    static readonly PackedScene orbScene =
        GD.Load<PackedScene>("res://Scripts/StageElements/MysticalOrb/MysticalOrbScene.tscn");

    /// <summary>
    /// Adds one extra Mystical Orb to a path (the Lucky Charm unlock), sampled
    /// ~60% along the curve so it sits clear of the orb baked into the path scene.
    /// Curve coordinates share the path root's space, so the orb is parented to
    /// the path node at the sampled point.
    /// </summary>
    void SpawnBonusOrb(PathNode2D pathNode)
    {
        var curve = pathNode.Curve;
        if (orbScene == null || curve == null || curve.PointCount < 2) return;

        var orb = orbScene.Instantiate<MysticalOrbNode2D>();
        float length = curve.GetBakedLength();
        Vector2 point = curve.SampleBaked(length * 0.6f);
        Vector2 path2DOffset = pathNode.Path2DNode?.Position ?? Vector2.Zero;

        pathNode.AddChild(orb);
        orb.Position = path2DOffset + point;
    }

    /// <summary>
    /// Reacts to a Mystical Orb pickup. The orb fires this from inside a physics
    /// area callback, so the new collision car is spawned deferred (next idle)
    /// to avoid mutating the physics world mid-flush.
    /// </summary>
    void OnWagonAttached(Train train, PathNode2D pathNode, Wagon wagon)
    {
        // Capture the wagon's 1-based place in the line now (it was just appended).
        int slot = train.Wagons.Count;
        Callable.From(() => SpawnWagonView(train, pathNode, slot)).CallDeferred();
    }

    /// <summary>
    /// Builds the visual car for a coupled wagon, places it on the train's path
    /// behind the loco, and wires its collision so the now-longer train can be
    /// bumped along its whole length.
    /// </summary>
    void SpawnWagonView(Train train, PathNode2D pathNode, int slot)
    {
        if (!IsInstanceValid(pathNode)) return;

        var wagonNode = new WagonNode2D();

        // Match the car to its train's identity colour.
        var trainNode = ((IMouldable)train).GetView<TrainNode2D>();
        if (trainNode != null) wagonNode.SetBodyColor(trainNode.Modulate);

        pathNode.AttachWagon(wagonNode, slot);

        // The car's area is created once it enters the tree (in AttachWagon).
        if (wagonNode.Area != null)
        {
            wagonNode.Area.OwnerTrain = train;
            wagonNode.Area.BumpedTrain += () => stage.TriggerBump();
        }
    }

    public override void _ExitTree()
    {
        foreach (var (train, handler) in wagonSubs)
            train.WagonAttached -= handler;
        wagonSubs.Clear();
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
