using Godot;

public partial class TrainNode2D : Node2D
{
    [Export] Sprite2D Model;

    readonly Train train = new();
    public Train TrainModel => train;

    public override void _Ready()
    {
        train.SetView(this);
    }
}