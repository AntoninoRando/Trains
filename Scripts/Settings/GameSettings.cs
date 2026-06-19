using System;
using Godot;

/// <summary>
/// The player's persistent display settings (resolution, window mode, VSync),
/// surviving across runs and app restarts. Persisted to <c>user://settings.cfg</c>.
///
/// Static like <see cref="PlayerProfile"/> so any scene can read or change it
/// without an autoload. <see cref="Apply"/> pushes the current settings to the
/// live window via <see cref="DisplayServer"/>; the setters apply-and-save in one
/// step so the Settings screen just calls them. <see cref="Apply"/> is also run
/// once at launch (from the main menu) so saved settings take effect on boot.
/// </summary>
public static class GameSettings
{
    const string SavePath = "user://settings.cfg";
    const string Section = "display";

    /// <summary>A selectable window size.</summary>
    public readonly record struct Resolution(int Width, int Height)
    {
        public override string ToString() => $"{Width} × {Height}";
    }

    public enum DisplayMode
    {
        Windowed,
        Borderless,
        Fullscreen,
    }

    /// <summary>Offered window sizes: common 16:9, then 21:9 ultrawide, then 4:3.
    /// The dynamic path grid fills any of these, so every aspect works.</summary>
    public static readonly Resolution[] Resolutions =
    {
        new(1280, 720),   // 16:9
        new(1366, 768),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(2560, 1080),  // 21:9
        new(3440, 1440),  // 21:9
        new(1024, 768),   // 4:3
    };

    public static readonly string[] ModeNames = { "Windowed", "Borderless", "Fullscreen" };

    static int resolutionIndex;
    static DisplayMode mode = DisplayMode.Windowed;
    static bool vsync = true;
    static bool loaded;



    #region LOAD / SAVE --------------------------------------------------------
    static void EnsureLoaded()
    {
        if (!loaded) Load();
    }

    public static void Load()
    {
        loaded = true;
        resolutionIndex = DefaultResolutionIndex();
        mode = DisplayMode.Windowed;
        vsync = true;

        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok) return; // first run: keep defaults

        int w = cfg.GetValue(Section, "width", Resolutions[resolutionIndex].Width).AsInt32();
        int h = cfg.GetValue(Section, "height", Resolutions[resolutionIndex].Height).AsInt32();
        resolutionIndex = ClosestResolutionIndex(w, h);
        mode = (DisplayMode)Mathf.Clamp(cfg.GetValue(Section, "mode", 0).AsInt32(), 0, 2);
        vsync = cfg.GetValue(Section, "vsync", true).AsBool();
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        Resolution r = Resolutions[resolutionIndex];
        cfg.SetValue(Section, "width", r.Width);
        cfg.SetValue(Section, "height", r.Height);
        cfg.SetValue(Section, "mode", (int)mode);
        cfg.SetValue(Section, "vsync", vsync);
        if (cfg.Save(SavePath) != Error.Ok)
            Log.Warning($"[GameSettings] could not save to {SavePath}");
    }
    #endregion -----------------------------------------------------------------



    #region ACCESSORS ----------------------------------------------------------
    public static int ResolutionIndex { get { EnsureLoaded(); return resolutionIndex; } }
    public static DisplayMode Mode { get { EnsureLoaded(); return mode; } }
    public static bool VSync { get { EnsureLoaded(); return vsync; } }
    public static Resolution CurrentResolution { get { EnsureLoaded(); return Resolutions[resolutionIndex]; } }
    #endregion -----------------------------------------------------------------



    #region SETTERS (apply + persist) ------------------------------------------
    public static void SetResolutionIndex(int index)
    {
        EnsureLoaded();
        resolutionIndex = Mathf.Clamp(index, 0, Resolutions.Length - 1);
        Save();
        Apply();
    }

    public static void SetMode(DisplayMode newMode)
    {
        EnsureLoaded();
        mode = newMode;
        Save();
        Apply();
    }

    public static void SetVSync(bool enabled)
    {
        EnsureLoaded();
        vsync = enabled;
        Save();
        Apply();
    }

    /// <summary>Restores the out-of-the-box defaults (screen-fitted resolution,
    /// windowed, VSync on), then saves and applies them.</summary>
    public static void ResetToDefaults()
    {
        EnsureLoaded();
        resolutionIndex = DefaultResolutionIndex();
        mode = DisplayMode.Windowed;
        vsync = true;
        Save();
        Apply();
    }
    #endregion -----------------------------------------------------------------



    #region GRID SIZE ----------------------------------------------------------
    /// <summary>
    /// On-screen size (px) of one grid block for the current resolution. Bigger
    /// screens use bigger blocks so the track and trains don't look tiny — the
    /// fixed 64px block only suited the original ~720p build.
    ///
    /// TUNABLE: adjust this table to taste. Lower values pack more, smaller
    /// blocks onto the screen; higher values give fewer, larger blocks. 64 is the
    /// track tile art size, so it renders crispest; other values scale the tiles.
    /// </summary>
    public static int GridCellSize
    {
        get
        {
            EnsureLoaded();
            return CurrentResolution.Height switch
            {
                <= 720  => 64,
                <= 900  => 80,
                <= 1080 => 96,
                <= 1440 => 128,
                _       => 160,
            };
        }
    }

    /// <summary>How much to scale the 64px track art (and trains) so they match
    /// <see cref="GridCellSize"/>.</summary>
    public static float GridScale => GridCellSize / 64f;
    #endregion -----------------------------------------------------------------



    #region UI SCALING ---------------------------------------------------------
    /// <summary>The resolution the menu / shop Control scenes were laid out for.
    /// Used as the base when scaling them up to the chosen window size.</summary>
    public static readonly Vector2I UiBaseSize = new(1152, 648);

    /// <summary>
    /// Scales a Control scene (main menu, shop, standalone settings) to the window
    /// so it stays readable at high resolutions. Uses <c>canvas_items</c> stretch
    /// with an expanding aspect, so the UI fills the screen without letterbox bars.
    /// Gameplay must NOT use this — see <see cref="DisableUiScaling"/>.
    /// </summary>
    public static void EnableUiScaling(Window window)
    {
        if (window == null) return;
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = UiBaseSize;
    }

    /// <summary>
    /// Restores 1:1 native rendering (used by gameplay) so the viewport reports
    /// the true window size and the dynamic path grid fills the real screen.
    /// </summary>
    public static void DisableUiScaling(Window window)
    {
        if (window == null) return;
        window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
    }
    #endregion -----------------------------------------------------------------



    #region APPLY --------------------------------------------------------------
    /// <summary>Pushes the current settings to the live window.</summary>
    public static void Apply()
    {
        EnsureLoaded();

        DisplayServer.WindowSetVsyncMode(
            vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        switch (mode)
        {
            case DisplayMode.Fullscreen:
                // Godot's Fullscreen is a borderless window spanning the screen;
                // the OS keeps the native resolution, which the grid fills.
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;

            case DisplayMode.Borderless:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                ApplyWindowedSize();
                break;

            default: // Windowed
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                ApplyWindowedSize();
                break;
        }
    }

    static void ApplyWindowedSize()
    {
        Resolution r = Resolutions[resolutionIndex];
        var size = new Vector2I(r.Width, r.Height);
        DisplayServer.WindowSetSize(size);

        // Centre the window on its current screen.
        int screen = DisplayServer.WindowGetCurrentScreen();
        Vector2I screenPos = DisplayServer.ScreenGetPosition(screen);
        Vector2I screenSize = DisplayServer.ScreenGetSize(screen);
        DisplayServer.WindowSetPosition(screenPos + (screenSize - size) / 2);
    }
    #endregion -----------------------------------------------------------------



    #region DEFAULTS / LOOKUP --------------------------------------------------
    /// <summary>The largest offered resolution that fits the current screen, so
    /// first-run defaults look right on small and large monitors alike.</summary>
    static int DefaultResolutionIndex()
    {
        Vector2I screen = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
        int best = 0;
        long bestArea = 0;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            Resolution r = Resolutions[i];
            long area = (long)r.Width * r.Height;
            if (r.Width <= screen.X && r.Height <= screen.Y && area > bestArea)
            {
                bestArea = area;
                best = i;
            }
        }
        return best;
    }

    /// <summary>Index of the saved size, or the nearest offered one by pixel area.</summary>
    static int ClosestResolutionIndex(int w, int h)
    {
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].Width == w && Resolutions[i].Height == h) return i;

        int best = 0;
        long bestDiff = long.MaxValue;
        long target = (long)w * h;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            long diff = Math.Abs((long)Resolutions[i].Width * Resolutions[i].Height - target);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }
    #endregion -----------------------------------------------------------------
}
