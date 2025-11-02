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



    readonly Match match = new();
    public Match MatchModel => match;



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
        var stageView = match.Stage != null ? ((IMouldable)match.Stage).GetView<StageNode2D>() : null;
        if (stageView != null) RemoveChild(stageView);

        // Instantiate the stage node
        var newStageNode = stageScene.Instantiate<StageNode2D>();
        var newStage = newStageNode.StageModel;

        // If there's a winning train, pass it to the new stage
        if (match.WinningTrain != null)
        {
            Callable.From(() => newStageNode.Begin(match.WinningTrain)).CallDeferred();
        }
        else
        {
            Callable.From(() => newStageNode.Begin()).CallDeferred();
        }

        match.ChangeStage(newStage);
        match.Stage.Bump += OnBump;
        match.Stage.Completed += OnStageCompleted;
        AddChild(newStageNode);

        // Position the stage to align with the camera's offset
        // This ensures new paths and trains spawn in the camera's view
        newStageNode.Position = matchCamera.Offset;
        UpdateLabel();
    }

    void OnStageCompleted(Train train)
    {
        match.WinningTrain = train;
        matchCamera.TrackTrain(train);
    }

    void UpdateLabel()
    {
        stageLabel.Text = $"Stage {match.StageNumber}";
    }

    void OnBump()
    {
        Log.Info("Match lost");
        match.Stage.StopTrains();
        defeat.GetNode<Control>("Container").Visible = true;
    }

    void OnRetry()
    {
        Log.Info("Retrying match");
        GetTree().ReloadCurrentScene();
    }

    void OnExit()
    {
        Log.Info("Exiting to main menu");
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}

