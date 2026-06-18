using System;
using Godot;

/// <summary>
/// A virtual grid of square blocks sized to the current viewport. This is a
/// pure geometry helper (no game logic): it converts between grid nodes and
/// world pixels and samples nodes on the screen edges.
///
/// The grid is anchored at the origin and every node lands on a multiple of
/// <see cref="CellSize"/> pixels — the same convention <see cref="TrackTiler"/>
/// uses to rasterise curves — so generated paths and the rendered track stay
/// aligned. Cell size is fixed (64px) so the pixel-art tiles stay crisp; a
/// larger screen simply yields more cells.
///
/// "Nodes" are the grid intersections, indexed [0..Cols] x [0..Rows]. A path is
/// a polyline through these nodes; <see cref="TrackTiler"/> stamps a tile for
/// every cell the polyline crosses.
/// </summary>
public sealed class Grid
{
    /// <summary>Side length, in pixels, of one block. Matches the track tiles.</summary>
    public int CellSize { get; }

    /// <summary>Number of cells across — the largest valid column node index.</summary>
    public int Cols { get; }

    /// <summary>Number of cells down — the largest valid row node index.</summary>
    public int Rows { get; }

    public Grid(float viewportWidth, float viewportHeight, int cellSize = 64)
    {
        CellSize = Math.Max(1, cellSize);
        // Round (not floor) to the nearest cell so the outer grid nodes land on
        // the window boundary — generated paths then begin and end right at the
        // visible edges, with at most a half-cell of slack. Clamp to a sane
        // minimum so generation always has room to work, even on a tiny window.
        Cols = Math.Max(3, Mathf.RoundToInt(viewportWidth / CellSize));
        Rows = Math.Max(3, Mathf.RoundToInt(viewportHeight / CellSize));
    }

    /// <summary>World-pixel position of a grid node (its top-left convention is
    /// shared with the track renderer, which centres tiles on the same point).</summary>
    public Vector2 NodeToWorld(Vector2I node) => new(node.X * CellSize, node.Y * CellSize);

    /// <summary>True when the node lies on the on-screen grid (edges included).</summary>
    public bool InBounds(Vector2I node)
        => node.X >= 0 && node.X <= Cols && node.Y >= 0 && node.Y <= Rows;

    /// <summary>A random on-screen node lying on the given screen edge.</summary>
    public Vector2I RandomNodeOnEdge(GridEdge edge, Random rng) => edge switch
    {
        GridEdge.Top    => new Vector2I(rng.Next(0, Cols + 1), 0),
        GridEdge.Bottom => new Vector2I(rng.Next(0, Cols + 1), Rows),
        GridEdge.Left   => new Vector2I(0, rng.Next(0, Rows + 1)),
        GridEdge.Right  => new Vector2I(Cols, rng.Next(0, Rows + 1)),
        _               => new Vector2I(0, 0),
    };

    /// <summary>The node one cell beyond the screen, just outside <paramref name="edge"/>.
    /// Used so trains spawn (and finish) off-screen, as the hand-made paths did.</summary>
    public Vector2I OffscreenNode(Vector2I edgeNode, GridEdge edge) => edge switch
    {
        GridEdge.Top    => new Vector2I(edgeNode.X, -1),
        GridEdge.Bottom => new Vector2I(edgeNode.X, Rows + 1),
        GridEdge.Left   => new Vector2I(-1, edgeNode.Y),
        GridEdge.Right  => new Vector2I(Cols + 1, edgeNode.Y),
        _               => edgeNode,
    };
}

/// <summary>The four screen edges a path can enter from or exit to.</summary>
public enum GridEdge
{
    Top,
    Right,
    Bottom,
    Left,
}
