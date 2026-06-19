using Godot;

/// <summary>
/// View for a <see cref="Gate"/>. Drop it onto a path like the Mine or the Orb.
/// It listens to the shared <see cref="Metronome"/>, so it opens and closes in
/// time with the audible beat: the barrier bar lies across the track (red) when
/// shut and swings up out of the way (green) when open. Trains that reach a shut
/// gate are halted by the model and released the moment it opens.
/// </summary>
[GlobalClass]
public partial class GateNode2D : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    /// <summary>Beats the gate stays in its starting phase (see <see cref="StartsOpen"/>).</summary>
    [Export] public int OpenBeats = 2;

    /// <summary>Beats the gate stays in the opposite phase before flipping back.</summary>
    [Export] public int ClosedBeats = 2;

    /// <summary>If false the gate begins shut — place one open and one shut gate
    /// side by side for an alternating "on/off" pattern.</summary>
    [Export] public bool StartsOpen = true;

    [Export] Area2D GateArea;

    /// <summary>The swinging barrier (a Polygon2D in the scene). Optional.</summary>
    [Export] Node2D Bar;
    #endregion -----------------------------------------------------------------



    #region FIELDS -------------------------------------------------------------
    Gate gate;
    Metronome metronome;

    static readonly Color OpenColor = new(0.40f, 0.95f, 0.45f); // green: go
    static readonly Color ShutColor = new(0.95f, 0.30f, 0.30f); // red: stop
    const float OpenRotation = -1.45f; // ≈ -83°, barrier raised
    const float ShutRotation = 0f;     // barrier lying across the track
    #endregion -----------------------------------------------------------------



    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        gate = new Gate(StartsOpen) { OpenBeats = OpenBeats, ClosedBeats = ClosedBeats };
        gate.StateChanged += OnStateChanged;

        if (GateArea != null)
        {
            GateArea.AreaEntered += OnAreaEntered;
            GateArea.AreaExited += OnAreaExited;
        }

        // Join the shared beat and land on the correct phase immediately.
        metronome = Metronome.Ensure(this);
        metronome.AddGate();
        metronome.Ticked += OnTick;

        gate.SyncToTick(metronome.Tick);
        Render(gate.IsOpen, animate: false);
    }

    public override void _ExitTree()
    {
        if (metronome != null && GodotObject.IsInstanceValid(metronome))
        {
            metronome.Ticked -= OnTick;
            metronome.RemoveGate();
        }
    }
    #endregion -----------------------------------------------------------------



    #region PRIVATE METHODS ----------------------------------------------------
    void OnTick(long tickIndex) => gate.OnTick(tickIndex);

    void OnStateChanged(bool open) => Render(open, animate: true);

    void OnAreaEntered(Area2D area)
    {
        if (area is not TrainArea trainArea) return;
        // Only the locomotive (whose area's parent is the train) trips the gate;
        // a trailing wagon rolling over it is ignored.
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode != null) gate.TrainEntered(trainNode.TrainModel);
    }

    void OnAreaExited(Area2D area)
    {
        if (area is not TrainArea trainArea) return;
        var trainNode = trainArea.GetParentOrNull<TrainNode2D>();
        if (trainNode != null) gate.TrainExited(trainNode.TrainModel);
    }

    void Render(bool open, bool animate)
    {
        if (Bar == null) return;

        float targetRot = open ? OpenRotation : ShutRotation;
        Color targetCol = open ? OpenColor : ShutColor;

        if (!animate)
        {
            Bar.Rotation = targetRot;
            Bar.Modulate = targetCol;
            return;
        }

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(Bar, "rotation", targetRot, 0.18)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(Bar, "modulate", targetCol, 0.18);
    }
    #endregion -----------------------------------------------------------------
}
