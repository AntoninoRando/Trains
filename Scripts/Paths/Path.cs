using Godot;

public partial class Path : Node
{
    #region FIELDS -------------------------------------------------------------
    [Export] public Node2D Train;
    [Export] public PathFollow2D PathFollow;
    #endregion -----------------------------------------------------------------



    public override void _Ready()
    {
        if (Train != null)
        {
            AddTrain(Train);
        }
    }

    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain(Node2D train)
    {
        train.CallDeferred("reparent", PathFollow);
        train.GetNode<AutoMover>("AutoMover").followPath2D = PathFollow;
    }
}
