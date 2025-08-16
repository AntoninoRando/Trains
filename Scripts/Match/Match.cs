using Godot;

public partial class Match : Node
{
    [Export] PackedScene stageScene;
    [Export] Node defeat;
    [Export] Label stageLabel;

    Stage stage;
    int stageNumber = 1;

    public override void _Ready()
    {
        defeat.GetNode<Button>("Container/Retry").Pressed += OnRetry;
        defeat.GetNode<Button>("Container/Exit").Pressed += OnExit;
        StartStage();
    }

    void StartStage()
    {
        stage = stageScene.Instantiate<Stage>();
        stage.Bump += OnBump;
        stage.Completed += OnStageCompleted;
        AddChild(stage);
        stage.CallDeferred(nameof(Stage.Begin));
        UpdateLabel();
    }

    void OnStageCompleted()
    {
        stage.QueueFree();
        stageNumber++;
        StartStage();
    }

    void UpdateLabel()
    {
        stageLabel.Text = $"Stage {stageNumber}";
    }

    void OnBump()
    {
        GD.Print("Match lost");
        stage.StopTrains();
        defeat.GetNode<Control>("Container").Visible = true;
    }

    void OnRetry()
    {
        GetTree().ReloadCurrentScene();
    }

    void OnExit()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}

