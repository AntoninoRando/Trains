using Godot;

public partial class AutoMover : Node
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] public double Speed = 0.05;
    #endregion -----------------------------------------------------------------



    public PathFollow2D FollowPath2D;


    public AutoMover()
    {
        
    }

    
    public override void _Process(double delta)
    {
        FollowPath2D.ProgressRatio += (float)(Speed * delta);
    }
}
