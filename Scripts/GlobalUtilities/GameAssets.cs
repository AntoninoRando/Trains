using System.Collections.Generic;
using Godot;

/// <summary>Identifies which rail tile shape to fetch from the asset library.</summary>
public enum RailKind { Straight, Corner, Tee, Cross, End }

/// <summary>
/// Central, cached asset library. It is the single source of truth for every
/// art path in the game and the only place that talks to <see cref="ResourceLoader"/>.
///
/// Efficiency:
///  * Every asset is loaded at most once and the same instance is shared by all
///    consumers, so a hundred orbs cost one <see cref="SpriteFrames"/> and one
///    texture in memory rather than a hundred.
///  * <see cref="Preload"/> warms everything on a background thread at boot, so
///    the first orb/wagon/track to appear doesn't hitch the main thread.
///  * If art hasn't been imported yet the loader returns null (and warns once)
///    instead of crashing, so the game still runs with placeholder-less nodes.
/// </summary>
public static class GameAssets
{
    #region ASSET PATHS --------------------------------------------------------
    public const string OrbFrames    = "res://Assets/OfStageElements/MysticalOrb/orb.tres";
    public const string GateTickSound = "res://Assets/OfStageElements/Gate/gate_tick.wav";
    public const string WagonTexture = "res://Assets/OfTrains/Wagon/wagon.png";
    public const string RailStraight = "res://Assets/OfPaths/Rails/rail_straight.png";
    public const string RailCorner   = "res://Assets/OfPaths/Rails/rail_corner.png";
    public const string RailTee      = "res://Assets/OfPaths/Rails/rail_tee.png";
    public const string RailCross    = "res://Assets/OfPaths/Rails/rail_cross.png";
    public const string RailEnd      = "res://Assets/OfPaths/Rails/rail_end.png";

    static readonly string[] AllPaths =
    {
        OrbFrames, GateTickSound, WagonTexture,
        RailStraight, RailCorner, RailTee, RailCross, RailEnd,
    };
    #endregion -----------------------------------------------------------------



    #region STATE --------------------------------------------------------------
    static readonly Dictionary<string, Resource> cache = new();
    static readonly HashSet<string> threaded = new();
    static bool preloadStarted;
    #endregion -----------------------------------------------------------------



    #region PUBLIC API ---------------------------------------------------------
    /// <summary>
    /// Begins loading every asset on background threads. Safe to call more than
    /// once; only the first call does work. Assets are still available before
    /// this finishes — <see cref="Get{T}"/> simply blocks on the one it needs.
    /// </summary>
    public static void Preload()
    {
        if (preloadStarted) return;
        preloadStarted = true;

        foreach (var path in AllPaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            if (ResourceLoader.LoadThreadedRequest(path) == Error.Ok)
                threaded.Add(path);
        }
    }

    /// <summary>Cached, typed load. Returns the one shared instance for a path.</summary>
    public static T Get<T>(string path) where T : Resource
    {
        if (cache.TryGetValue(path, out var hit)) return hit as T;

        if (!ResourceLoader.Exists(path))
        {
            Log.Warning($"[GameAssets] asset not found (import it in Godot?): {path}");
            cache[path] = null;
            return null;
        }

        // Complete a background request if one is in flight; otherwise load now.
        Resource res = threaded.Contains(path)
            ? ResourceLoader.LoadThreadedGet(path)
            : ResourceLoader.Load(path);

        cache[path] = res;
        return res as T;
    }
    #endregion -----------------------------------------------------------------



    #region TYPED ACCESSORS ----------------------------------------------------
    public static SpriteFrames Orb() => Get<SpriteFrames>(OrbFrames);

    /// <summary>The metronome click played on every beat while gates are active.</summary>
    public static AudioStream GateTick() => Get<AudioStream>(GateTickSound);

    public static Texture2D Wagon() => Get<Texture2D>(WagonTexture);

    public static Texture2D Rail(RailKind kind) => Get<Texture2D>(kind switch
    {
        RailKind.Straight => RailStraight,
        RailKind.Corner   => RailCorner,
        RailKind.Tee      => RailTee,
        RailKind.Cross    => RailCross,
        _                 => RailEnd,
    });
    #endregion -----------------------------------------------------------------
}
