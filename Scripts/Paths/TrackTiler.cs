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
/// Each tile is tinted by the colour of the path that owns it, so paths (and
/// their trains) are easy to tell apart. Cells used by more than one path
/// (overlaps and crossings) are tinted with the average of those paths' colours,
/// which reads as "shared track".
///
/// The network is rebuilt automatically whenever the set of paths, their
/// geometry, or their colours change, so it works with runtime-spawned and
/// carryover paths.
///
/// Placed as the first child of the paths container, its tiles draw beneath the
/// trains while sitting above the stage background.
/// </summary>
public partial class TrackTiler : Node2D
{
    /// <summary>World size of one grid cell. The grid uses this to space its
    /// nodes (a multiple of the 64px tile art), so the stage sets it to match
    /// <see cref="GameSettings.GridCellSize"/>. Tiles are scaled to fill it.</summary>
    public int CellSize { get; set; } = TrackTileset.TileSize; // 64 by default

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
            // colour, so re-tinting a path also triggers a rebuild
            Color c = p.TrackColor;
            Mix(Mathf.RoundToInt(c.R * 255) | (Mathf.RoundToInt(c.G * 255) << 8)
                | (Mathf.RoundToInt(c.B * 255) << 16) | (Mathf.RoundToInt(c.A * 255) << 24));
        }
        return h;
    }

    // --------------------------------------------------------------- rebuild
    void Rebuild()
    {
        foreach (Node child in GetChildren()) child.QueueFree();

        // Gather the paths and the identity colour each one paints with.
        var pathNodes = new List<PathNode2D>();
        var pathColors = new List<Color>();
        foreach (var p in Paths())
        {
            pathColors.Add(ResolveColor(p, pathNodes.Count));
            pathNodes.Add(p);
        }

        // 1) Accumulate, per grid cell, the connection mask and the set of paths
        //    that pass through it.
        var masks = new Dictionary<Vector2I, int>();
        var cellPaths = new Dictionary<Vector2I, HashSet<int>>();

        for (int pi = 0; pi < pathNodes.Count; pi++)
        {
            var curve = pathNodes[pi].Curve;
            var path2d = pathNodes[pi].Path2DNode;
            if (curve == null || path2d == null || curve.PointCount < 2) continue;

            for (int i = 0; i < curve.PointCount - 1; i++)
            {
                Vector2 a = ToLocal(path2d.ToGlobal(curve.GetPointPosition(i)));
                Vector2 b = ToLocal(path2d.ToGlobal(curve.GetPointPosition(i + 1)));
                Stamp(masks, cellPaths, pi, ToCell(a), ToCell(b));
            }
        }

        // The equipped track style: "classic" keeps the per-train identity
        // colours; any other purchased style paints every rail with its tint.
        var style = PlayerProfile.TrackStyle;
        bool classicStyle = style == null || style.Id == "track_classic";

        // 2) Instantiate one tinted tile sprite per cell.
        int placed = 0;
        foreach (var kv in masks)
        {
            if (!tileset.TryGet(kv.Value, out var tex, out float rot)) continue;
            float tileScale = (float)CellSize / TrackTileset.TileSize;
            AddChild(new Sprite2D
            {
                Texture = tex,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Position = new Vector2(kv.Key.X * CellSize, kv.Key.Y * CellSize),
                Scale = new Vector2(tileScale, tileScale),
                Rotation = rot,
                Modulate = classicStyle
                    ? BlendColor(cellPaths.GetValueOrDefault(kv.Key), pathColors)
                    : style.Tint,
            });
            placed++;
        }

        Log.Info($"[TrackTiler] rebuilt track: {placed} tiles over {masks.Count} cells "
               + $"({pathNodes.Count} paths).");
    }

    static Color ResolveColor(PathNode2D p, int index)
        => p.TrackColor.A > 0f ? p.TrackColor : TrackPalette.For(index);

    // Average the colours of every path that uses a cell (white if somehow none).
    static Color BlendColor(HashSet<int> set, List<Color> colors)
    {
        if (set == null || set.Count == 0) return new Color(1, 1, 1);
        float r = 0, g = 0, b = 0;
        foreach (int i in set)
        {
            Color c = colors[i];
            r += c.R; g += c.G; b += c.B;
        }
        return new Color(r / set.Count, g / set.Count, b / set.Count);
    }

    // ----------------------------------------------------------- grid helpers
    Vector2I ToCell(Vector2 local)
        => new(Mathf.RoundToInt(local.X / CellSize), Mathf.RoundToInt(local.Y / CellSize));

    /// Connect two cells. We move along X then along Y, so a straight segment is
    /// rasterised exactly and an unexpected diagonal degrades to a clean L.
    static void Stamp(Dictionary<Vector2I, int> masks, Dictionary<Vector2I, HashSet<int>> cellPaths,
                      int pi, Vector2I a, Vector2I b)
    {
        Vector2I cur = a;
        cur = Walk(masks, cellPaths, pi, cur, new Vector2I(b.X, cur.Y));
        Walk(masks, cellPaths, pi, cur, new Vector2I(cur.X, b.Y));
    }

    static Vector2I Walk(Dictionary<Vector2I, int> masks, Dictionary<Vector2I, HashSet<int>> cellPaths,
                         int pi, Vector2I cur, Vector2I target)
    {
        Mark(cellPaths, cur, pi);
        while (cur != target)
        {
            var step = new Vector2I(
                System.Math.Sign(target.X - cur.X),
                System.Math.Sign(target.Y - cur.Y));
            Vector2I next = cur + step;
            int d = DirBit(step);
            masks[cur] = masks.GetValueOrDefault(cur) | d;
            masks[next] = masks.GetValueOrDefault(next) | Opposite(d);
            Mark(cellPaths, next, pi);
            cur = next;
        }
        return cur;
    }

    static void Mark(Dictionary<Vector2I, HashSet<int>> cellPaths, Vector2I cell, int pi)
    {
        if (!cellPaths.TryGetValue(cell, out var set))
        {
            set = new HashSet<int>();
            cellPaths[cell] = set;
        }
        set.Add(pi);
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
