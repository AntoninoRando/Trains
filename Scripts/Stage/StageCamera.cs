using System;
using Godot;

/// <summary>
/// Controls the camera during stage transitions, tracking the winning train
/// until it reaches the opposite edge of the screen.
/// </summary>
public partial class StageCamera : Camera2D
{
    private Train targetTrain;
    private bool isTracking = false;
    private Vector2 viewportSize;
    private float trackingDuration = 1f;



    public event Action TransitionComplete;



    /// <summary>
    /// Check if the camera is currently tracking a train.
    /// </summary>
    public bool IsTracking => isTracking;

    /// <summary>
    /// Direction towards the border the winning train is touching
    /// (e.g. Vector2.Right when it touches the right border).
    /// Valid after a call to TrackTrain.
    /// </summary>
    public Vector2 ExitDirection { get; private set; }

    /// <summary>
    /// The offset the camera will have reached at the end of the transition.
    /// The next stage must be placed at this position so that the slide
    /// reveals its background. Valid after a call to TrackTrain.
    /// </summary>
    public Vector2 TargetOffset { get; private set; }

    public override void _Ready()
    {
        viewportSize = GetViewport().GetVisibleRect().Size;
    }

    /// <summary>
    /// Start tracking the winning train: the whole world slides in the
    /// direction opposite to the border the train is touching, so the train
    /// ends up on the opposite edge of the screen.
    /// </summary>
    public void TrackTrain(Train train)
    {
        var trainNode = ((IMouldable)train).GetView<TrainNode2D>();
        // Train position relative to the current view (the camera is centered
        // on the viewport, so the visible world starts at Offset).
        var screenPos = trainNode.GlobalPosition - Offset;

        ExitDirection = ComputeExitDirection(screenPos);

        if (ExitDirection == Vector2.Zero)
        {
            // No clear border touched: nothing to slide, complete right away.
            TargetOffset = Offset;
            Callable.From(StopTracking).CallDeferred();
            return;
        }

        // The train must end up on the opposite border: shift the world by
        // the distance between the train and that border.
        var targetScreenPos = new Vector2(
            ExitDirection.X > 0 ? 0 : ExitDirection.X < 0 ? viewportSize.X : screenPos.X,
            ExitDirection.Y > 0 ? 0 : ExitDirection.Y < 0 ? viewportSize.Y : screenPos.Y);
        TargetOffset = Offset + (screenPos - targetScreenPos);

        targetTrain = train;
        isTracking = true;

        var tween = CreateTween();
        tween
            .TweenProperty(this, "offset", TargetOffset, trackingDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(StopTracking));
    }

    /// <summary>
    /// Which border of the screen the train is closest to (based on the
    /// half of the viewport it occupies), or Vector2.Zero if it is too
    /// close to the center.
    /// </summary>
    private Vector2 ComputeExitDirection(Vector2 screenPos)
    {
        if (screenPos.X < viewportSize.X / 2 - 100) return Vector2.Left;
        if (screenPos.X > viewportSize.X / 2 + 100) return Vector2.Right;
        if (screenPos.Y < viewportSize.Y / 2 - 100) return Vector2.Up;
        if (screenPos.Y > viewportSize.Y / 2 + 100) return Vector2.Down;
        return Vector2.Zero;
    }

    /// <summary>
    /// Stop tracking and signal that transition is complete.
    /// </summary>
    private void StopTracking()
    {
        isTracking = false;
        targetTrain = null;
        TransitionComplete?.Invoke();
    }
}
