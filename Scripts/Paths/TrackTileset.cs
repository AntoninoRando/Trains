using System.Collections.Generic;
using Godot;

/// <summary>
/// Maps a rail-connection mask to the right tile texture + rotation.
///
/// A tile is described by which of its four edges it connects to, encoded as a
/// 4-bit mask: N=1, E=2, S=4, W=8. Only five base shapes are needed because
/// every one of the 16 possible masks is one of these rotated by a multiple of
/// 90 degrees:
///
///   straight (E|W)      corner (S|E)      t-junction (E|S|W)      cross (all)      end (E)
///
/// The five 64x64 tile textures are real art assets, loaded once and shared by
/// every tile on screen via <see cref="GameAssets"/>. <see cref="TryGet"/>
/// returns the right texture and rotation for any mask, which is what makes the
/// track read correctly through straights, turns, T-junctions and crossings.
/// Swap the PNGs under <c>res://Assets/OfPaths/Rails/</c> to reskin the track.
/// </summary>
public sealed class TrackTileset
{
    public const int TileSize = 64;

    // ---- direction bits --------------------------------------------------
    public const int N = 1, E = 2, S = 4, W = 8;

    readonly Dictionary<int, (Texture2D tex, float rot)> map = new();

    public TrackTileset()
    {
        // Register each base shape in its canonical orientation; Register adds
        // the three further 90-degree rotations that the same texture covers.
        Register(GameAssets.Rail(RailKind.Straight), E | W);
        Register(GameAssets.Rail(RailKind.Corner),   S | E);
        Register(GameAssets.Rail(RailKind.Tee),      E | S | W);
        Register(GameAssets.Rail(RailKind.Cross),    N | E | S | W);
        Register(GameAssets.Rail(RailKind.End),      E);
    }

    /// <summary>Texture + rotation (radians) to draw for a connection mask (1..15).</summary>
    public bool TryGet(int mask, out Texture2D tex, out float rotation)
    {
        if (map.TryGetValue(mask, out var v)) { tex = v.tex; rotation = v.rot; return true; }
        tex = null; rotation = 0f; return false;
    }

    // ---- mask rotation ---------------------------------------------------

    /// <summary>Rotate a connection mask 90 degrees clockwise (N->E->S->W->N).
    /// This matches Godot's positive Node2D rotation (screen y points down), so
    /// a texture rotated by +90 degrees connects exactly the rotated edges.</summary>
    static int RotateCw(int m)
    {
        int r = 0;
        if ((m & N) != 0) r |= E;
        if ((m & E) != 0) r |= S;
        if ((m & S) != 0) r |= W;
        if ((m & W) != 0) r |= N;
        return r;
    }

    void Register(Texture2D tex, int canonical)
    {
        if (tex == null) return; // art not imported yet -> tiler simply skips it

        int m = canonical;
        for (int k = 0; k < 4; k++)
        {
            if (!map.ContainsKey(m)) map[m] = (tex, k * Mathf.Pi / 2f);
            m = RotateCw(m);
        }
    }
}
