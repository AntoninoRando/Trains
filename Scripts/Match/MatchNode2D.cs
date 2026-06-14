using Godot;

public partial class MatchNode2D : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] PackedScene stageScene;
    [Export] Node defeat;
    [Export] Label stageLabel;
    [Export] CompleteAnimation completeAnimation;
    [Export] StageCamera matchCamera;
    #endregion -----------------------------------------------------------------



    readonly Match match = new(); public Match MatchModel => match;



    #region STAGE TRANSITION STATE ---------------------------------------------
    /// The stage currently being played.
    StageNode2D currentStageNode;

    /// The next stage, spawned as soon as a train wins so that the camera
    /// slide reveals its background.
    StageNode2D pendingStageNode;

    /// The winning train of the stage that just completed. Kept here because
    /// Match.Start() clears Match.WinningTrain before Started fires.
    Train winnerTrain;

    /// The winning train view, kept in overlay during the transition.
    TrainNode2D winnerNode;
    Vector2 winnerBaseScale;
    float winnerBaseRotation;
    int winnerBaseZIndex;

    const float WINNER_ROTATION_DEGREES = 6f;
    const float WINNER_ZOOM = 1.35f;
    const double WINNER_TWEEN_DURATION = 0.35;
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        ((IMouldable)match).SetView(this);
        defeat.GetNode<Button>("Container/Retry").Pressed += OnRetry;
        defeat.GetNode<Button>("Container/Exit").Pressed += OnExit;
        matchCamera.TransitionComplete += match.Start;
        match.Started += OnMatchStarted;
        match.Start();
    }
    #endregion -----------------------------------------------------------------



    void OnMatchStarted()
    {
        // Discard the old stage, reclaiming the winning train first so it
        // survives and can continue on a new path.
        if (currentStageNode != null)
        {
            if (winnerNode != null) winnerNode.Reparent(this);
            currentStageNode.QueueFree();
        }

        // The next stage was already placed during the transition slide;
        // only the very first stage needs to be created here.
        var newStageNode = pendingStageNode ?? CreateStageNode(matchCamera.Offset);
        pendingStageNode = null;
        currentStageNode = newStageNode;

        match.ChangeStage(newStageNode.StageModel);
        match.Stage.Bump += OnBump;
        match.Stage.Completed += OnStageCompleted;

        // If there's a winning train, revert its overlay and let it start a
        // new path from where it is now, while the new trains enter the scene.
        // Captured locally: the deferred call must not read a cleared field.
        var carryover = winnerTrain;
        winnerTrain = null;
        if (carryover != null) RevertWinnerOverlay();
        Callable.From(() => newStageNode.Begin(carryover)).CallDeferred();

        UpdateLabel();
    }

    StageNode2D CreateStageNode(Vector2 position)
    {
        var stageNode = stageScene.Instantiate<StageNode2D>();
        AddChild(stageNode);
        stageNode.Position = position;
        return stageNode;
    }

    void OnStageCompleted(Train train)
    {
        match.WinningTrain = train;
        winnerTrain = train;

        // 1. Overlay the winning train: slight clockwise rotation + zoom.
        PlayWinnerOverlay(((IMouldable)train).GetView<TrainNode2D>());

        // 2. Slide everything away from the border the train is touching.
        matchCamera.TrackTrain(train);

        // 3. Place the next stage where the slide will end, so its background
        //    is revealed by the slide itself.
        pendingStageNode = CreateStageNode(matchCamera.TargetOffset);
    }

    void PlayWinnerOverlay(TrainNode2D node)
    {
        winnerNode = node;
        winnerBaseScale = node.Scale;
        winnerBaseRotation = node.RotationDegrees;
        winnerBaseZIndex = node.ZIndex;

        // Sovra-impressione: draw above both the old and the new stage.
        node.ZIndex = 100;

        var tween = CreateTween().SetParallel();
        tween
            .TweenProperty(node, "rotation_degrees", winnerBaseRotation + WINNER_ROTATION_DEGREES, WINNER_TWEEN_DURATION)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(node, "scale", winnerBaseScale * WINNER_ZOOM, WINNER_TWEEN_DURATION)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
    }

    void RevertWinnerOverlay()
    {
        if (winnerNode == null) return;
        var node = winnerNode;
        winnerNode = null;

        var tween = CreateTween().SetParallel();
        tween
            .TweenProperty(node, "rotation_degrees", winnerBaseRotation, WINNER_TWEEN_DURATION)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween
            .TweenProperty(node, "scale", winnerBaseScale, WINNER_TWEEN_DURATION)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(node)) node.ZIndex = winnerBaseZIndex;
        }));
    }

    void UpdateLabel()
    {
        stageLabel.Text = $"Stage {match.StageNumber}";
    }

    void OnBump()
    {
        match.Lose();
        defeat.GetNode<Control>("Container").Visible = true;
    }

    void OnRetry()
    {
        Log.Info("Retrying match");
        match.Interrupt();
        GetTree().ReloadCurrentScene();
    }

    void OnExit()
    {
        Log.Info("Exiting to main menu");
        match.Interrupt();
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
