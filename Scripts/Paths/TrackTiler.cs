using System.Collections.Generic;
using Godot;

/// <summary>
/// Renders the otherwise-invisible train paths as a tiled railroad track.
///
/// It treats every <see cref="PathNode2D"/> under its parent as part of one
/// shared track network. Each path's <see cref="Curve2D"/> is rasterised onto a
/// 64px grid; for every grid cell we accumulate a 4-bit mask of which
/// neighbours the track connects to (N/E/S/W). Because the masks from *all*
/// paths are merged into the same grid, the right tile is chosen automatically:
///
///   * a cell with two opposite connections   -> straight rail
///   * two adjacent connections               -> a curved corner (a turn)
///   * three connections                       -> a T-junction
///   * four connections                        -> a crossing (an intersection)
///
/// The network is rebuilt automatically whenever the set of paths (or their
/// geometry) changes, so it works with runtime-spawned and carryover paths.
///
/// Placed as the first child of the paths container, its tiles draw beneath the
/// trains while sitting above the stage background.
/// </summary>
public partial class TrackTiler : Node2D
{
    const int Cell = TrackTileset.TileSize; // 64
    const int N = TrackTileset.N, E = TrackTileset.E, S = TrackTileset.S, W = TrackTileset.W;

    TrackTileset tileset;
    ulong lastSignature = ulong.MaxValue;

    public override void _Ready()
    {
        tileset = new TrackTileset();
        ZIndex = 0; // above background; trains (later siblings) remain on top
    }

    public override void _Process(double delta)
    {
        // Cheap change-detection: rebuild only when the paths actually change.
        ulong sig = ComputeSignature();
        if (sig != lastSignature)
        {
            lastSignature = sig;
            Rebuild();
        }
    }

    // ------------------------------------------------------------------ paths
    IEnumerable<PathNode2D> Paths()
    {
        var parent = GetParent();
        if (parent == null) yield break;
        foreach (Node child in parent.GetChildren())
            if (child is PathNode2D p) yield return p;
    }

    ulong ComputeSignature()
    {
        ulong h = 1469598103934665603UL; // FNV-1a
        void Mix(long v) { h ^= (ulong)v; h *= 1099511628211UL; }

        foreach (var p in Paths())
        {
            var curve = p.Curve;
            if (curve == null) { Mix(-1); continue; }
            Mix(curve.PointCount);
            for (int i = 0; i < curve.PointCount; i++)
            {
                var pt = curve.GetPointPosition(i);
                Mix(Mathf.RoundToInt(pt.X));
                Mix(Mathf.RoundToInt(pt.Y));
            }
        }
        return h;
    }

    // --------------------------------------------------------------- rebuild
    void Rebuild()
    {
        foreach (Node child in GetChildren()) child.QueueFree();

        // 1) Accumulate connection masks per grid cell across every path.
        var masks = new Dictionary<Vector2I, int>();
        foreach (var p in Paths())
        {
            var curve = p.Curve;
            var path2d = p.Path2DNode;
            if (curve == null || path2d == null || curve.PointCount < 2) continue;

            for (int i = 0; i < curve.PointCount - 1; i++)
            {
                Vector2 a = ToLocal(path2d.ToGlobal(curve.GetPointPosition(i)));
                Vector2 b = ToLocal(path2d.ToGlobal(curve.GetPointPosition(i + 1)));
                Stamp(masks, ToCell(a), ToCell(b));
            }
        }

        // 2) Instantiate one tile sprite per cell.
        int placed = 0;
        foreach (var kv in masks)
        {
            if (!tileset.TryGet(kv.Value, out var tex, out float rot)) continue;
            AddChild(new Sprite2D
            {
                Texture = tex,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Position = new Vector2(kv.Key.X * Cell, kv.Key.Y * Cell),
                Rotation = rot,
            });
            placed++;
        }

        Log.Info($"[TrackTiler] rebuilt track: {placed} tiles over {masks.Count} cells "
               + $"({CountPaths()} paths).");
    }

    int CountPaths()
    {
        int n = 0;
        foreach (var _ in Paths()) n++;
        return n;
    }

    // ----------------------------------------------------------- grid helpers
    static Vector2I ToCell(Vector2 local)
        => new(Mathf.RoundToInt(local.X / Cell), Mathf.RoundToInt(local.Y / Cell));

    /// Connect two cells. We move along X then along Y, so a straight segment is
    /// rasterised exactly and an unexpected diagonal degrades to a clean L.
    static void Stamp(Dictionary<Vector2I, int> masks, Vector2I a, Vector2I b)
    {
        Vector2I cur = a;
        cur = Walk(masks, cur, new Vector2I(b.X, cur.Y));
        Walk(masks, cur, new Vector2I(cur.X, b.Y));
    }

    static Vector2I Walk(Dictionary<Vector2I, int> masks, Vector2I cur, Vector2I target)
    {
        while (cur != target)
        {
            var step = new Vector2I(
                System.Math.Sign(target.X - cur.X),
                System.Math.Sign(target.Y - cur.Y));
            Vector2I next = cur + step;
            int d = DirBit(step);
            masks[cur] = masks.GetValueOrDefault(cur) | d;
            masks[next] = masks.GetValueOrDefault(next) | Opposite(d);
            cur = next;
        }
        return cur;
    }

    static int DirBit(Vector2I step)
    {
        if (step.Y < 0) return N;
        if (step.Y > 0) return S;
        if (step.X > 0) return E;
        return W;
    }

    static int Opposite(int bit) => bit switch
    {
        N => S,
        S => N,
        E => W,
        W => E,
        _ => 0,
    };
}
