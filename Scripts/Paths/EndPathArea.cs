using Godot;

public partial class EndPathArea : Area2D
{
    public override void _Ready()
    {
        AreaEntered += OnTrainEnter;
    }


    void OnTrainEnter(Area2D area)
    {
        GD.Print("Path reached the end!");
    }
}
