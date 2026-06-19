using Godot;

/// <summary>
/// View for a <see cref="Smoke"/> cloud. Drop it onto a path like the Mine, the
/// Orb or the Gate. A ring of soft grey puffs is painted every frame at a high
/// absolute <see cref="CanvasItem.ZIndex"/>, so it draws on top of the trains
/// (which sit at z 0) while staying under the UI (which lives on a CanvasLayer).
/// Any train whose body is inside the cloud's area is therefore hidden from the
/// player — including a train from another lane crossing through.
/// </summary>
[GlobalClass]
public partial class SmokeNode2D : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    /// <summary>Radius of the cloud (visual reach and detection area), in pixels.</summary>
    [Export] public float Radius = 72f;

    [Export] Area2D SmokeArea;
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    readonly Smoke smoke = new();

    struct Puff
    {
        public Vector2 Origin;  // resting offset from the cloud centre
        public float BaseRadius;
        public float Phase;     // desync the billow between puffs
        public float Drift;     // billow speed
        public Color Color;
    }

    Puff[] puffs;
    double time;

    const int ZAboveTrains = 20; // trains/wagons sit at z 0; UI is on a CanvasLayer
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        // Always paint over the trains, never under them.
        ZIndex = ZAboveTrains;
        ZAsRelative = false;

        if (SmokeArea != null)
        {
            SmokeArea.AreaEntered += OnAreaEntered;
            SmokeArea.AreaExited += OnAreaExited;

            // Size the detection circle to the cloud (a fresh shape so several
            // clouds of different sizes never share one resource).
            var cs = SmokeArea.GetNodeOrNull<CollisionShape2D>("Collision Shape");
            if (cs != null) cs.Shape = new CircleShape2D { Radius = Radius };
        }

        BuildPuffs();
    }

    public override void _Process(double delta)
    {
        time += delta;
        QueueRedraw(); // keep the cloud billowing
    }

    public override void _Draw()
    {
        if (puffs == null) return;

        foreach (var p in puffs)
        {
            float wobble = Mathf.Sin((float)time * p.Drift + p.Phase);
            float sway = Mathf.Cos((float)time * p.Drift * 0.8f + p.Phase);
            Vector2 centre = p.Origin + new Vector2(wobble, sway) * 4f;
            float radius = p.BaseRadius * (1f + 0.06f * wobble);
            DrawCircle(centre, radius, p.Color);
        }
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    /// <summary>Builds a tight cluster of overlapping puffs: their union reads as
    /// one cloud and their dense centre is effectively opaque, so a train under
    /// it cannot be seen. Seeded so every cloud is laid out identically.</summary>
    void BuildPuffs()
    {
        var rng = new RandomNumberGenerator { Seed = 0xC0FFEEUL };
        const int count = 11;
        puffs = new Puff[count];

        for (int i = 0; i < count; i++)
        {
            float angle = rng.RandfRange(0f, Mathf.Tau);
            float dist = rng.RandfRange(0f, Radius * 0.5f);
            float grey = rng.RandfRange(0.55f, 0.80f);

            puffs[i] = new Puff
            {
                Origin = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist,
                BaseRadius = rng.RandfRange(Radius * 0.55f, Radius * 0.78f),
                Phase = rng.RandfRange(0f, Mathf.Tau),
                Drift = rng.RandfRange(0.6f, 1.4f),
                Color = new Color(grey, grey, grey, rng.RandfRange(0.82f, 0.95f)),
            };
        }
    }

    void OnAreaEntered(Area2D area)
    {
        if (area is not TrainArea trainArea) return;
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode != null) smoke.Enter(trainNode.TrainModel);
    }

    void OnAreaExited(Area2D area)
    {
        if (area is not TrainArea trainArea) return;
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode != null) smoke.Exit(trainNode.TrainModel);
    }
    #endregion -----------------------------------------------------------------
}
