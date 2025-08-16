using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Path : Node
{
    #region FIELDS -------------------------------------------------------------
    [Export] public PathFollow2D PathFollow;
    [Export] public EndPathArea End;
    [Export] public double Speed = 0.05;
    #endregion -----------------------------------------------------------------



    public IEnumerable<Train> Trains =>
        PathFollow.GetChildren().Where(t => t is Train).Cast<Train>();

    bool speeded;
    Key assignedKey;


    public override void _Input(InputEvent @event)
    {
        if (Input.IsKeyPressed(assignedKey))
        {
            if (!speeded)
            {
                Speed *= 2;
                speeded = true;
            }
        }
        else
        {
            if (speeded)
            {
                Speed /= 2;
                speeded = false;
            }
        }
    }

    /// <summary>
    /// Adds a train to this path.
    /// </summary>
    public void AddTrain(Node2D train)
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
        assignedKey = InputListener.TakeKey();

        // var mover = new AutoMover();
        // train.GetNode<AutoMover>("AutoMover").FollowPath2D = PathFollow;
    }

    public override void _Process(double delta)
    {
        PathFollow.ProgressRatio += (float)(Speed * delta);
    }
}
