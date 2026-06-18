using System;
using System.Collections.Generic;
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

    /// <summary>The Path2D that owns this path's curve (parent of PathFollow).</summary>
    public Path2D Path2DNode => PathFollow?.GetParent() as Path2D;

    /// <summary>The geometry of this path, used by the track renderer.</summary>
    public Curve2D Curve => Path2DNode?.Curve;

    /// <summary>Identity colour for this path's track and train.
    /// Alpha 0 means "unassigned" (the renderer then falls back to a palette
    /// colour picked by path order).</summary>
    public Color TrackColor { get; set; } = new Color(0, 0, 0, 0);
    #endregion -----------------------------------------------------------------


    
    bool onSprint;
    public bool IsSprinting => onSprint;
    string assignedAction;

    public event Action SprintStarted;
    public event Action SprintStopped;



    #region WAGONS -------------------------------------------------------------
    /// <summary>Distance, in curve pixels, between consecutive cars.</summary>
    const float WagonSpacing = 42f;

    /// <summary>Trailing followers; each rides this path a fixed distance behind
    /// the loco (slot 1 is the first wagon, slot 2 the next, and so on).</summary>
    readonly List<(PathFollow2D Follower, int Slot)> wagonFollowers = [];

    /// <summary>
    /// Couples a wagon car onto this path. It gets its own PathFollow2D so it
    /// tracks the curve (corners included) a fixed distance behind the loco.
    /// </summary>
    /// <param name="slot">1-based position in the line (1 = directly behind the loco).</param>
    public void AttachWagon(WagonNode2D wagonNode, int slot)
    {
        var path2D = Path2DNode;
        if (path2D == null || PathFollow == null) return;

        var follower = new PathFollow2D
        {
            Name = $"WagonFollow{slot}",
            Loop = false,   // clamp at the start instead of wrapping to the end
            Rotates = true,
        };
        path2D.AddChild(follower);
        follower.AddChild(wagonNode);
        wagonNode.Position = Vector2.Zero;

        follower.Progress = Mathf.Max(0f, PathFollow.Progress - WagonSpacing * slot);
        wagonFollowers.Add((follower, slot));
    }
    #endregion -----------------------------------------------------------------


    
    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        path.BaseSpeed = baseSpeed;
        // "Turbo Boiler" upgrades bought in the shop add to the sprint multiplier.
        path.SprintMultiplier = sprintMultiplier + PlayerProfile.SprintBonus;
        path.TrainAdded += AddTrain2D;
    }

    public override void _Process(double delta)
    {
        PathFollow.ProgressRatio += (float)(path.Speed * delta);

        // Trail each wagon a fixed distance behind the loco along the curve.
        if (wagonFollowers.Count > 0)
        {
            float leadProgress = PathFollow.Progress;
            foreach (var (follower, slot) in wagonFollowers)
                follower.Progress = Mathf.Max(0f, leadProgress - WagonSpacing * slot);
        }
    }
    #endregion -----------------------------------------------------------------



    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain2D(Train train)
    {
        var trainNode2D = ((IMouldable)train).GetView<TrainNode2D>();

        if (trainNode2D.GetParent() != null) trainNode2D.Reparent(PathFollow);
        else PathFollow.AddChild(trainNode2D);
        trainNode2D.Position = Vector2.Zero;
    }
}
