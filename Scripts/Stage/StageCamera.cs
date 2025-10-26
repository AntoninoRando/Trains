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
    private Vector2 initialPosition;
    private float trackingDuration = 1f;



    public event Action TransitionComplete;

    

    /// <summary>
    /// Check if the camera is currently tracking a train.
    /// </summary>
    public bool IsTracking => isTracking;

    public override void _Ready()
    {
        var viewport = GetViewport().GetVisibleRect();
        viewportSize = viewport.Size;
        initialPosition = Position;
    }

    /// <summary>
    /// Start tracking the winning train until it reaches the opposite edge.
    /// </summary>
    public void TrackTrain(Train train)
    {
        var trainNode = train.GetView<TrainNode2D>();
        var (trainX, trainY) = trainNode.GlobalPosition;

        if (trainX < viewportSize.X / 2 - 100)
        {
            var trainTargetX = viewportSize.X;
            var shiftAmount = trainTargetX - trainX;
            var tween = CreateTween();
            tween
                .TweenProperty(this, "offset:x", GlobalPosition.X - shiftAmount, trackingDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenCallback(Callable.From(StopTracking));
        }
        else if (trainX > viewportSize.X / 2 + 100)
        {
            var trainTargetX = 0;
            var shiftAmount = trainTargetX - trainX;
            var tween = CreateTween();
            tween
                .TweenProperty(this, "offset:x", GlobalPosition.X - shiftAmount, trackingDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenCallback(Callable.From(StopTracking));
        }
        else if (trainY < viewportSize.Y / 2 - 100)
        {
            var trainTargetY = viewportSize.Y;
            var shiftAmount = trainTargetY - trainY;
            var tween = CreateTween();
            tween
                .TweenProperty(this, "offset:y", GlobalPosition.Y - shiftAmount, trackingDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenCallback(Callable.From(StopTracking));
        }
        else if (trainY > viewportSize.Y / 2 + 100)
        {
            var trainTargetY = 0;
            var shiftAmount = trainTargetY - trainY;
            var tween = CreateTween();
            tween
                .TweenProperty(this, "offset:y", GlobalPosition.Y - shiftAmount, trackingDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenCallback(Callable.From(StopTracking));
        }

        targetTrain = train;
        isTracking = true;
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
