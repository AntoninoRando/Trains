using System;
using Godot;


public partial class PathNode2D : Node2D
{
    #region FIELDS -------------------------------------------------------------
    [Export] public PathFollow2D PathFollow;
    [Export] public EndPathArea End;
    [Export] private double baseSpeed = 0.05;
    [Export] private double sprintMultiplier = 2;

    readonly Path path = new();
    public Path PathModel => path;
    #endregion -----------------------------------------------------------------

    bool onSprint;
    public bool IsSprinting => onSprint;
    string assignedAction;

    public event Action SprintStarted;
    public event Action SprintStopped;


    
    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        path.BaseSpeed = baseSpeed;
        path.SprintMultiplier = sprintMultiplier;
        path.TrainAdded += AddTrain2D;
    }

    public override void _Process(double delta)
    {
        PathFollow.ProgressRatio += (float)(path.Speed * delta);
    }
    #endregion -----------------------------------------------------------------



    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain2D(Train train)
    {
        var trainNode2D = train.GetView<TrainNode2D>();

        if (trainNode2D.GetParent() != null) trainNode2D.Reparent(PathFollow);
        else PathFollow.AddChild(trainNode2D);
        trainNode2D.Position = Vector2.Zero;
    }
}
