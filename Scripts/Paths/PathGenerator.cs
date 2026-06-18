using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Generates the stage's train paths at runtime instead of loading hand-made
/// scenes. Each path is an axis-aligned polyline that enters from one screen
/// edge, wanders across the <see cref="Grid"/> to fill the screen, and exits at
/// another edge. Geometry is grid-aligned so it renders cleanly through
/// <see cref="TrackTiler"/> (straights, corners, and shared crossings).
///
/// NOTE ON BALANCING: this first pass only guarantees paths that *fill the
/// screen* with roughly comparable lengths. Making them genuinely fair (equal
/// effective length / difficulty, controlled crossings) is a later step — see
/// <see cref="BalanceTargetLength"/>, the single knob the routing leans on, and
/// the weighting in <see cref="Wander"/> where a smarter cost function would go.
/// </summary>
public static class PathGenerator
{
    // Four unit steps: N, E, S, W (matches TrackTiler's direction bits).
    static readonly Vector2I[] Dirs =
    {
        new(0, -1), new(1, 0), new(0, 1), new(-1, 0),
    };

    /// <summary>
    /// Builds <paramref name="count"/> paths sized to the grid. Pass a seeded
    /// <see cref="Random"/> for reproducible stages; null picks a fresh one.
    /// </summary>
    public static List<Curve2D> Generate(Grid grid, int count, Random rng = null)
    {
        rng ??= new Random();
        count = Math.Max(1, count);

        int target = BalanceTargetLength(grid);

        var curves = new List<Curve2D>(count);
        for (int i = 0; i < count; i++)
            curves.Add(GenerateOne(grid, rng, target));
        return curves;
    }

    /// <summary>The length (in cells) the router aims each path toward, so paths
    /// on the same screen stay comparable. The future balancing pass should
    /// replace this with a per-path measure of *effective* race length.</summary>
    static int BalanceTargetLength(Grid grid) => grid.Cols + grid.Rows;

    static Curve2D GenerateOne(Grid grid, Random rng, int targetLen)
    {
        // 1) Pick two distinct edges and a node on each.
        GridEdge startEdge = (GridEdge)rng.Next(0, 4);
        GridEdge endEdge = (GridEdge)(((int)startEdge + 1 + rng.Next(0, 3)) % 4);
        Vector2I start = grid.RandomNodeOnEdge(startEdge, rng);
        Vector2I end = grid.RandomNodeOnEdge(endEdge, rng);

        // 2) Wander to fill space, then guarantee arrival; keep only corners.
        List<Vector2I> route = Simplify(Wander(grid, rng, start, end, targetLen));

        // 3) Materialise the curve, extending one cell off-screen at each end so
        //    the train spawns and finishes just outside the view (as before).
        var curve = new Curve2D();
        curve.AddPoint(grid.NodeToWorld(grid.OffscreenNode(start, startEdge)));
        foreach (var node in route)
            curve.AddPoint(grid.NodeToWorld(node));
        curve.AddPoint(grid.NodeToWorld(grid.OffscreenNode(end, endEdge)));
        return curve;
    }

    /// <summary>
    /// A biased random walk from <paramref name="start"/> to <paramref name="end"/>:
    /// it favours unvisited cells (to spread across the screen) and, past the
    /// target length, pulls toward the finish. Never reverses on itself.
    /// Always terminated with a greedy run so the route reaches the finish.
    /// </summary>
    static List<Vector2I> Wander(Grid grid, Random rng, Vector2I start, Vector2I end, int targetLen)
    {
        var route = new List<Vector2I> { start };
        var visited = new HashSet<Vector2I> { start };
        Vector2I cur = start;
        Vector2I prev = new(int.MinValue, int.MinValue);
        int budget = (grid.Cols + grid.Rows) * 4 + targetLen;

        for (int steps = 0; steps < budget; steps++)
        {
            if (cur == end && route.Count > targetLen / 2) break;

            var options = new List<Vector2I>(4);
            var weights = new List<int>(4);
            int totalWeight = 0;
            foreach (var d in Dirs)
            {
                Vector2I next = cur + d;
                if (!grid.InBounds(next) || next == prev) continue;

                int w = 1;
                bool closer = Manhattan(next, end) < Manhattan(cur, end);
                if (closer) w += 1;                              // gentle homing
                if (route.Count >= targetLen && closer) w += 8;  // converge once long enough
                if (!visited.Contains(next)) w += 2;             // prefer fresh ground -> fills space

                options.Add(next);
                weights.Add(w);
                totalWeight += w;
            }

            if (options.Count == 0) break; // boxed in; the greedy run below finishes it

            int pick = rng.Next(0, totalWeight);
            int idx = 0;
            while (idx < weights.Count - 1 && (pick -= weights[idx]) >= 0) idx++;

            prev = cur;
            cur = options[idx];
            route.Add(cur);
            visited.Add(cur);
        }

        AppendManhattan(route, cur, end);
        return route;
    }

    /// <summary>Greedy axis-aligned run (X then Y) appended to guarantee the
    /// route ends exactly on the finish node.</summary>
    static void AppendManhattan(List<Vector2I> route, Vector2I from, Vector2I to)
    {
        Vector2I cur = from;
        while (cur.X != to.X)
        {
            cur = new Vector2I(cur.X + Math.Sign(to.X - cur.X), cur.Y);
            route.Add(cur);
        }
        while (cur.Y != to.Y)
        {
            cur = new Vector2I(cur.X, cur.Y + Math.Sign(to.Y - cur.Y));
            route.Add(cur);
        }
    }

    /// <summary>Collapses straight runs, keeping only the corner nodes (and the
    /// two endpoints). The resulting compact polyline matches how the hand-made
    /// curves stored only their bends.</summary>
    static List<Vector2I> Simplify(List<Vector2I> route)
    {
        if (route.Count <= 2) return route;

        var simplified = new List<Vector2I> { route[0] };
        for (int i = 1; i < route.Count - 1; i++)
        {
            Vector2I inDir = route[i] - route[i - 1];
            Vector2I outDir = route[i + 1] - route[i];
            if (inDir != outDir) simplified.Add(route[i]); // direction changed -> a corner
        }
        simplified.Add(route[^1]);
        return simplified;
    }

    static int Manhattan(Vector2I a, Vector2I b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
