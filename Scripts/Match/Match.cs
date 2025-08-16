using Godot;

public partial class Match : Node
{
    [Export] Stage stage;

    public override void _Ready()
    {
        stage.Bump += OnBump;
    }

    void OnBump()
    {
        GD.Print("Match lost");
    }
}
