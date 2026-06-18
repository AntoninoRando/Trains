using Godot;

/// <summary>
/// View for a <see cref="MysticalOrb"/>. Drop it onto a path (like the Mine).
/// The looping idle animation comes from a shared <see cref="SpriteFrames"/>
/// asset (served by <see cref="GameAssets"/>); when a train's body rolls over
/// it, the orb is collected — coupling a wagon to that train — and pops away.
/// </summary>
[GlobalClass]
public partial class MysticalOrbNode2D : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] public int GoldValue = 10;
    [Export] Area2D OrbArea;
    [Export] AnimatedSprite2D Sprite;
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    readonly MysticalOrb orb = new();
    bool collected;
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        // Shop upgrades ("Orb Polish") and the Golden Orbs unlock raise an orb's worth.
        orb.GoldValue = GoldValue + PlayerProfile.OrbValueBonus;
        OrbArea.AreaEntered += OnAreaEntered;

        // One shared SpriteFrames instance backs every orb on screen.
        if (Sprite != null)
        {
            Sprite.TextureFilter = TextureFilterEnum.Nearest; // crisp pixel art
            Sprite.SpriteFrames = GameAssets.Orb();
            if (Sprite.SpriteFrames != null) Sprite.Play("idle");
        }

        // Golden Orbs unlock: tint the orb gold and make it read as more valuable.
        if (PlayerProfile.GoldenOrbs)
        {
            Modulate = new Color(1f, 0.84f, 0.30f);
            Scale *= 1.15f;
        }
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    void OnAreaEntered(Area2D area)
    {
        if (collected) return;
        if (area is not TrainArea trainArea) return;

        // Only a locomotive (whose area's parent is the train) can scoop an orb;
        // a trailing wagon rolling over it is ignored.
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode == null) return;

        if (orb.TryCollect(trainNode.TrainModel))
            Collect();
    }

    void Collect()
    {
        collected = true;

        // Stop detecting further trains and pop out of view, then free.
        OrbArea.SetDeferred(Area2D.PropertyName.Monitoring, false);

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.22)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0f, 0.18);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
    #endregion -----------------------------------------------------------------
}
