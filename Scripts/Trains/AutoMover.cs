using Godot;

public partial class AutoMover : Node
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] public Node2D trainModel;
    [Export] public PathFollow2D followPath2D;
    [Export] public double Speed = 0.05;
    #endregion -----------------------------------------------------------------


    
    public override void _Process(double delta)
    {
        followPath2D.ProgressRatio += (float)(Speed * delta);
    }
}
