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
        if (match.Stage != null) RemoveChild(match.Stage);

        // If there's a winning train, pass it to the new stage
        if (match.WinningTrain != null)
        {
            Callable.From(() => match.Stage.Begin(match.WinningTrain)).CallDeferred();
        }
        else
        {
            Callable.From(() => match.Stage.Begin()).CallDeferred();
        }

        match.ChangeStage(stageScene.Instantiate<Stage>());
        match.Stage.Bump += OnBump;
        match.Stage.Completed += OnStageCompleted;
        AddChild(match.Stage);

        // Position the stage to align with the camera's offset
        // This ensures new paths and trains spawn in the camera's view
        match.Stage.Position = matchCamera.Offset;
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

