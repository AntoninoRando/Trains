using Godot;

public partial class Match : Node
{
    [Export] Stage stage;
    [Export] Node defeat;

    public override void _Ready()
    {
        stage.Bump += OnBump;
    }

    void OnBump()
    {
        GD.Print("Match lost");
        stage.StopTrains();
        defeat.GetNode<Control>("Container").Visible = true;
    }
}
