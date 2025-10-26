using Godot;

/// <summary>
/// Represents a mine area where trains stop to mine resources.
/// </summary>
[GlobalClass]
public partial class MineNode2D : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] public double MiningDuration = 3.0;
    [Export] Area2D MineArea;
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    readonly Mine mine = new();
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        MineArea.AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        mine.Update(delta);
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    void OnAreaEntered(Area2D area)
    {
        if (area is not TrainArea trainArea) return;

        var train = trainArea.GetParentOrNull<TrainNode2D>();
        if (train != null)
        {
            mine.StartMining(train.TrainModel);
        }
    }
    #endregion -----------------------------------------------------------------
}
