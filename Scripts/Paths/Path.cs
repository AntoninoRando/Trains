using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Path : Node
{
    #region FIELDS -------------------------------------------------------------
    [Export] public PathFollow2D PathFollow;
    [Export] public EndPathArea End;
    [Export] public double Speed = 0.05;
    [Export] public double SprintMultiplier = 2;
    #endregion -----------------------------------------------------------------



    public IEnumerable<Train> Trains =>
        PathFollow.GetChildren().Where(t => t is Train).Cast<Train>();

    bool onSprint;
    string assignedAction;

    public void Sprint()
    {
        if (onSprint) return;
        Speed *= SprintMultiplier;
        onSprint = true;
    }

    public void StopSprint()
    {
        if (!onSprint) return;
        Speed /= SprintMultiplier;
        onSprint = false;
    }


    public override void _Process(double delta)
    {
        PathFollow.ProgressRatio += (float)(Speed * delta);
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
    }
}
