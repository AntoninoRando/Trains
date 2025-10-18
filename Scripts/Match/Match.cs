using Godot;

public partial class Match : Node
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] PackedScene stageScene;
    [Export] Node defeat;
    [Export] Label stageLabel;
    [Export] CompleteAnimation completeAnimation;
    [Export] StageCamera matchCamera;
    #endregion -----------------------------------------------------------------



    Stage stage;
    int stageNumber = 0;
    Train winningTrain = null;



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        defeat.GetNode<Button>("Container/Retry").Pressed += OnRetry;
        defeat.GetNode<Button>("Container/Exit").Pressed += OnExit;
        matchCamera.TransitionComplete += StartStage;
        StartStage();
    }
    #endregion -----------------------------------------------------------------



    void StartStage()
    {
        stageNumber++;

        if (stage != null) RemoveChild(stage);
        stage = stageScene.Instantiate<Stage>();
        stage.Bump += OnBump;
        stage.Completed += OnStageCompleted;
        AddChild(stage);

        // Position the stage to align with the camera's offset
        // This ensures new paths and trains spawn in the camera's view
        stage.Position = matchCamera.Offset;

        // If there's a winning train, pass it to the new stage
        if (winningTrain != null)
        {
            Callable.From(() => stage.Begin(winningTrain)).CallDeferred();
            winningTrain = null;
        }
        else
        {
            Callable.From(() => stage.Begin()).CallDeferred();
        }

        UpdateLabel();
    }

    void OnStageCompleted(Train train)
    {
        winningTrain = train;
        matchCamera.TrackTrain(train);
    }

    void UpdateLabel()
    {
        stageLabel.Text = $"Stage {stageNumber}";
    }

    void OnBump()
    {
        Log.Info("Match lost");
        stage.StopTrains();
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

