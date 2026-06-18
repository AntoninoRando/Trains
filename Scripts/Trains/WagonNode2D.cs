using Godot;

/// <summary>
/// View for a <see cref="Wagon"/>: a small car, carrying its orb, that rides
/// the path behind the locomotive. The body is a shared texture asset (served
/// by <see cref="GameAssets"/>), tinted to its train's identity colour. It
/// builds its own collision area so the lengthened train can be bumped along
/// its whole body, not just the loco.
/// </summary>
[GlobalClass]
public partial class WagonNode2D : Node2D
{
    /// <summary>Collision area for this car. Built in _Ready; wire its
    /// OwnerTrain and BumpedTrain after the node has entered the tree.</summary>
    public TrainArea Area { get; private set; }

    Sprite2D sprite;
    Color bodyColor = Colors.White;

    const float HalfW = 14f;
    const float HalfH = 9f;

    public override void _Ready()
    {
        // Visual: one shared wagon texture, tinted per train.
        sprite = new Sprite2D
        {
            Texture = GameAssets.Wagon(),
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = bodyColor,
        };
        AddChild(sprite);

        // Collision sized to the car body.
        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(HalfW * 2f, HalfH * 2f) }
        };
        Area = new TrainArea { Name = "Area" };
        Area.AddChild(shape);
        AddChild(Area);
    }

    /// <summary>Tints the car to match its owning train.</summary>
    public void SetBodyColor(Color color)
    {
        bodyColor = color;
        if (sprite != null) sprite.Modulate = color;
    }
}
