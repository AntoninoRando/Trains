using System.Collections.Generic;
using Godot;

/// <summary>
/// Generates a small pixel-art railroad tileset at runtime (no external image
/// files are required, so nothing needs importing). Every tile is a 64x64
/// <see cref="Texture2D"/> drawn from ballast + wooden ties + steel rails.
///
/// A tile is described by which of its four edges it connects to, encoded as a
/// 4-bit mask: N=1, E=2, S=4, W=8. Only five base shapes are needed because
/// every one of the 16 possible masks is one of these rotated by a multiple of
/// 90 degrees:
///
///   straight (E|W)      corner (S|E)      t-junction (E|S|W)      cross (all)      end (E)
///
/// <see cref="TryGet"/> returns the right texture and rotation for any mask,
/// which is what makes the track read correctly through straights, turns,
/// T-junctions and crossings.
/// </summary>
public sealed class TrackTileset
{
    public const int TileSize = 64;

    // ---- direction bits --------------------------------------------------
    public const int N = 1, E = 2, S = 4, W = 8;

    // ---- geometry (in tile pixels) --------------------------------------
    const int Center = 32;     // tile centre
    const int RailA = 22;      // first rail centre line  (centre - 10)
    const int RailB = 42;      // second rail centre line (centre + 10)
    const int BallastHalf = 22;// ballast bed half-width  -> spans 10..54
    const int TieHalfLen = 18; // ties span centre +/- 18 -> 14..50
    static readonly int[] TiePos = { 8, 24, 40, 56 };
    const int TieHalfW = 3;

    // ---- palette (pixel-art, tuned for the dark cave background) ---------
    static readonly Color Ballast0 = Rgb(96, 93, 86);
    static readonly Color BallastDark = Rgb(72, 70, 65);
    static readonly Color BallastLight = Rgb(122, 118, 110);
    static readonly Color Tie = Rgb(112, 76, 44);
    static readonly Color TieHi = Rgb(146, 102, 60);
    static readonly Color Rail = Rgb(150, 158, 166);
    static readonly Color RailHi = Rgb(214, 222, 228);
    static readonly Color RailLo = Rgb(92, 100, 108);
    static readonly Color Plate = Rgb(120, 128, 136);
    static readonly Color Bumper = Rgb(156, 54, 54);
    static readonly Color BumperHi = Rgb(198, 86, 86);

    readonly Dictionary<int, (Texture2D tex, float rot)> map = new();

    public TrackTileset()
    {
        // Build each base shape in a canonical orientation, then register the
        // four rotations that shape can satisfy.
        Register(BuildStraight(), E | W);   // 10 -> straight
        Register(BuildCorner(), S | E);     // 6  -> corner
        Register(BuildTee(), E | S | W);    // 14 -> t-junction
        Register(BuildCross(), N | E | S | W); // 15 -> cross
        Register(BuildEnd(), E);            // 2  -> end cap
    }

    /// <summary>Texture + rotation (radians) to draw for a connection mask (1..15).</summary>
    public bool TryGet(int mask, out Texture2D tex, out float rotation)
    {
        if (map.TryGetValue(mask, out var v)) { tex = v.tex; rotation = v.rot; return true; }
        tex = null; rotation = 0f; return false;
    }

    // ======================================================================
    //  Mask rotation
    // ======================================================================

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
        int m = canonical;
        for (int k = 0; k < 4; k++)
        {
            if (!map.ContainsKey(m)) map[m] = (tex, k * Mathf.Pi / 2f);
            m = RotateCw(m);
        }
    }

    // ======================================================================
    //  Tile painters
    // ======================================================================

    static Image NewImage()
        => Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);

    static Texture2D Finish(Image img) => ImageTexture.CreateFromImage(img);

    // Straight, running East-West.
    static Texture2D BuildStraight()
    {
        var img = NewImage();
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                if (Mathf.Abs(y - Center) <= BallastHalf)
                    img.SetPixel(x, y, BallastAt(x, y));

        foreach (int tx in TiePos) VTie(img, tx, 0, TileSize);
        HRail(img, RailA, 0, TileSize);
        HRail(img, RailB, 0, TileSize);
        return Finish(img);
    }

    // 90-degree curve connecting the South and East edges, arc centred on the
    // tile's bottom-right corner so the rail spacing is preserved into the
    // neighbouring straight tiles.
    static Texture2D BuildCorner()
    {
        var img = NewImage();
        const float ox = TileSize, oy = TileSize; // arc centre = (64,64)

        // ballast
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float dx = x + 0.5f - ox, dy = y + 0.5f - oy;
                if (dx > 0 || dy > 0) continue;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r >= Center - BallastHalf && r <= Center + BallastHalf)
                    img.SetPixel(x, y, BallastAt(x, y));
            }

        // radial ties
        float[] tieAng = { 9, 27, 45, 63, 81 };
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float dx = x + 0.5f - ox, dy = y + 0.5f - oy;
                if (dx > 0 || dy > 0) continue;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < Center - TieHalfLen || r > Center + TieHalfLen) continue;
                float t = Mathf.RadToDeg(Mathf.Atan2(-dy, -dx)); // 0..90
                foreach (float a in tieAng)
                    if (Mathf.Abs(t - a) <= 3f)
                    { img.SetPixel(x, y, r > 46 ? TieHi : Tie); break; }
            }

        // curved rails
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float dx = x + 0.5f - ox, dy = y + 0.5f - oy;
                if (dx > 0 || dy > 0) continue;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                RailRing(img, x, y, r, RailA);
                RailRing(img, x, y, r, RailB);
            }
        return Finish(img);
    }

    // T-junction: straight East-West bar with a branch going South.
    static Texture2D BuildTee()
    {
        var img = NewImage();
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                bool bar = Mathf.Abs(y - Center) <= BallastHalf;
                bool branch = Mathf.Abs(x - Center) <= BallastHalf && y >= Center;
                if (bar || branch) img.SetPixel(x, y, BallastAt(x, y));
            }

        foreach (int tx in TiePos) VTie(img, tx, 0, TileSize);     // bar ties
        HTie(img, 40, Center, TileSize);                            // branch ties
        HTie(img, 56, Center, TileSize);

        VRail(img, RailA, Center - BallastHalf, TileSize);          // branch rails
        VRail(img, RailB, Center - BallastHalf, TileSize);
        HRail(img, RailA, 0, TileSize);                             // bar rails (on top)
        HRail(img, RailB, 0, TileSize);
        return Finish(img);
    }

    // 4-way crossing.
    static Texture2D BuildCross()
    {
        var img = NewImage();
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                if (Mathf.Abs(y - Center) <= BallastHalf || Mathf.Abs(x - Center) <= BallastHalf)
                    img.SetPixel(x, y, BallastAt(x, y));
            }

        // centre crossing plate (under the rails)
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                if (Mathf.Abs(x - Center) + Mathf.Abs(y - Center) <= 9)
                    img.SetPixel(x, y, Plate);

        // ties only in the outer arms to keep the centre readable
        VTie(img, 8, 0, TileSize); VTie(img, 56, 0, TileSize);
        HTie(img, 8, 0, TileSize); HTie(img, 56, 0, TileSize);

        HRail(img, RailA, 0, TileSize); HRail(img, RailB, 0, TileSize);
        VRail(img, RailA, 0, TileSize); VRail(img, RailB, 0, TileSize);
        return Finish(img);
    }

    // Dead-end: rail enters from the East and stops at a buffer on the West.
    static Texture2D BuildEnd()
    {
        var img = NewImage();
        for (int y = 0; y < TileSize; y++)
            for (int x = 16; x < TileSize; x++)
                if (Mathf.Abs(y - Center) <= BallastHalf)
                    img.SetPixel(x, y, BallastAt(x, y));

        foreach (int tx in new[] { 30, 44, 58 }) VTie(img, tx, 0, TileSize);
        HRail(img, RailA, 22, TileSize);
        HRail(img, RailB, 22, TileSize);

        for (int y = 14; y <= 49; y++)
            for (int x = 16; x <= 21; x++)
                img.SetPixel(x, y, x == 16 ? BumperHi : Bumper);
        return Finish(img);
    }

    // ======================================================================
    //  Pixel helpers
    // ======================================================================

    static Color Rgb(int r, int g, int b, float a = 1f)
        => new Color(r / 255f, g / 255f, b / 255f, a);

    // Deterministic gravel speckle so tiles look textured but never flicker.
    static Color BallastAt(int x, int y)
    {
        uint h = (uint)((x * 73856093) ^ (y * 19349663));
        h ^= h >> 13; h *= 2654435761u; h ^= h >> 16;
        uint v = h % 5u;
        return v == 0 ? BallastDark : v == 1 ? BallastLight : Ballast0;
    }

    static void SafeSet(Image img, int x, int y, Color c)
    {
        if (x >= 0 && y >= 0 && x < TileSize && y < TileSize) img.SetPixel(x, y, c);
    }

    // horizontal rail centred on row ry, columns [x0,x1)
    static void HRail(Image img, int ry, int x0, int x1)
    {
        for (int x = x0; x < x1; x++)
        {
            SafeSet(img, x, ry - 2, RailLo);
            SafeSet(img, x, ry - 1, RailHi);
            SafeSet(img, x, ry, Rail);
            SafeSet(img, x, ry + 1, RailLo);
        }
    }

    // vertical rail centred on column cx, rows [y0,y1)
    static void VRail(Image img, int cx, int y0, int y1)
    {
        for (int y = y0; y < y1; y++)
        {
            SafeSet(img, cx - 2, y, RailLo);
            SafeSet(img, cx - 1, y, RailHi);
            SafeSet(img, cx, y, Rail);
            SafeSet(img, cx + 1, y, RailLo);
        }
    }

    // a point on a curved rail: draw if radius r is within ~1.6px of ring rc
    static void RailRing(Image img, int x, int y, float r, float rc)
    {
        float d = r - rc;
        if (Mathf.Abs(d) > 1.6f) return;
        Color c = d < -0.6f ? RailHi : d > 0.6f ? RailLo : Rail;
        SafeSet(img, x, y, c);
    }

    // vertical wooden tie (for horizontal track) centred on column tx
    static void VTie(Image img, int tx, int y0, int y1)
    {
        int ya = Mathf.Max(y0, Center - TieHalfLen);
        int yb = Mathf.Min(y1, Center + TieHalfLen);
        for (int y = ya; y < yb; y++)
            for (int x = tx - TieHalfW; x <= tx + TieHalfW - 1; x++)
                SafeSet(img, x, y, x == tx - TieHalfW ? TieHi : Tie);
    }

    // horizontal wooden tie (for vertical track) centred on row ty
    static void HTie(Image img, int ty, int x0, int x1)
    {
        int xa = Mathf.Max(x0, Center - TieHalfLen);
        int xb = Mathf.Min(x1, Center + TieHalfLen);
        for (int x = xa; x < xb; x++)
            for (int y = ty - TieHalfW; y <= ty + TieHalfW - 1; y++)
                SafeSet(img, x, y, y == ty - TieHalfW ? TieHi : Tie);
    }
}
