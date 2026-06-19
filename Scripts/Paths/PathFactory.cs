using Godot;

/// <summary>
/// Builds a fully-wired <see cref="PathNode2D"/> from a generated
/// <see cref="Curve2D"/>, replacing the old hand-made path scenes
/// (Assets/TrainsPaths/0001-0002.tscn). The produced tree mirrors those scenes:
///
///   PathNode2D (script)
///     Path2D                (owns the curve)
///       PathFollow2D        -> assigned to PathNode2D.PathFollow
///     End : EndPathArea     -> assigned to PathNode2D.End (the finish line)
///       CollisionShape2D
///
/// so everything downstream (TrackTiler, TrainsSpawner, StageNode2D, wagons)
/// keeps working unchanged.
/// </summary>
public static class PathFactory
{
    /// <summary>Side of the square finish trigger, in pixels (~one cell), large
    /// enough to reliably catch the locomotive crossing the edge.</summary>
    const float EndShapeSize = 56f;

    public static PathNode2D Build(Curve2D curve)
    {
        var root = new PathNode2D { Name = "GeneratedPath" };

        var path2D = new Path2D { Name = "Path2D", Curve = curve };
        root.AddChild(path2D);

        var follow = new PathFollow2D
        {
            Name = "PathFollow2D",
            Rotates = true,   // trains face along the track, including corners
            Loop = false,     // clamp at the end instead of wrapping to the start
        };
        path2D.AddChild(follow);

        var end = new EndPathArea { Name = "End" };
        // Last curve point is the off-screen exit; the finish line sits on the
        // on-screen edge node just before it (point count is always >= 3).
        int finishIndex = Mathf.Max(0, curve.PointCount - 2);
        end.Position = curve.GetPointPosition(finishIndex);
        // Scale the finish trigger with the block size so it stays proportional
        // to the (now larger) trains and track on big resolutions.
        float endSize = EndShapeSize * GameSettings.GridScale;
        end.AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new RectangleShape2D { Size = new Vector2(endSize, endSize) },
        });
        root.AddChild(end);

        // Wire the [Export] references the scene used to provide.
        root.PathFollow = follow;
        root.End = end;
        return root;
    }
}
