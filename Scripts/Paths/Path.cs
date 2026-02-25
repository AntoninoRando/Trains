using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class SpeedLayer
{
    public string Id { get; }
    public int Priority { get; }
    public Func<double, double> Transform { get; }

    public SpeedLayer(string id, int priority, Func<double, double> transform)
    {
        Id = id;
        Priority = priority;
        Transform = transform;
    }
}

public partial class Path : Node
{
    #region EXPORT FIELDS ──────────────────────────────────────────────────────
    [Export] public PathFollow2D PathFollow;
    [Export] public EndPathArea End;
    [Export] private double baseSpeed = 0.05;
    [Export] public double SprintMultiplier = 2;
    #endregion ─────────────────────────────────────────────────────────────────


    #region LOCAL FIELDS ───────────────────────────────────────────────────────
    private Vector2[] points;
    private List<SpeedLayer> speedLayers = [];

    public double Speed
    {
        get
        {
            double speed = baseSpeed;
            foreach (var layer in speedLayers.OrderBy(l => l.Priority))
            {
                speed = layer.Transform(speed);
            }
            return speed;
        }
    }

    public IEnumerable<Train> Trains => PathFollow.GetChildren().Where(t => t is Train).Cast<Train>();
    public Vector2[] Points => points;

    bool onSprint;
    public bool IsSprinting => onSprint;
    string assignedAction;
    #endregion ─────────────────────────────────────────────────────────────────



    #region EVENTS ─────────────────────────────────────────────────────────────
    public event Action SprintStarted;
    public event Action SprintStopped;
    #endregion ─────────────────────────────────────────────────────────────────



    #region GODOT LIFECYCLE ────────────────────────────────────────────────────
    override public void _Ready()
    {
        if (points == null)
        {
            LoadPoints();
        }
    }

    public override void _Process(double delta)
    {
        PathFollow.ProgressRatio += (float)(Speed * delta);
    }
    #endregion ─────────────────────────────────────────────────────────────────



    private void LoadPoints()
    {
        var gridSize = 64;
        var path2d = GetNode<Path2D>("Path2D");
        var curve = path2d.Curve;
        var totalLength = curve.GetBakedLength();

        var result = new List<Vector2>();
        for (float t = 0; t < totalLength; t += gridSize)
        {
            result.Add(curve.SampleBaked(t));
        }
        result.Add(curve.SampleBaked(totalLength)); // always include the end point

        points = result.ToArray();
    }

    public void AddSpeedLayer(string id, int priority, Func<double, double> transform)
    {
        RemoveSpeedLayer(id); // Remove existing layer with same id if present
        speedLayers.Add(new SpeedLayer(id, priority, transform));
    }

    public void RemoveSpeedLayer(string id)
    {
        speedLayers.RemoveAll(layer => layer.Id == id);
    }

    public void SetBaseSpeed(double speed)
    {
        baseSpeed = speed;
    }

    public void Sprint()
    {
        if (onSprint) return;
        AddSpeedLayer("sprint", 100, speed => speed * SprintMultiplier);
        onSprint = true;
        SprintStarted?.Invoke();
    }

    public void StopSprint()
    {
        if (!onSprint) return;
        RemoveSpeedLayer("sprint");
        onSprint = false;
        SprintStopped?.Invoke();
    }

    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain(Train train)
    {
        if (train.GetParent() != null)
        {
            train.Reparent(PathFollow);
        }
        else
        {
            PathFollow.AddChild(train);
        }
        train.Position *= 0;
        train.Path = this;
    }
}
