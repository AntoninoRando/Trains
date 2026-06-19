using System;
using Godot;

/// <summary>
/// The Settings screen. Populates the resolution and window-mode dropdowns and
/// the VSync switch from <see cref="GameSettings"/>, and applies + persists every
/// change immediately through the matching <c>GameSettings.SetX</c> call.
///
/// Works both as a standalone screen (opened from the main menu) and as an
/// in-place overlay (opened from the pause menu): set <see cref="OverlayMode"/>
/// before adding it to the tree and listen to <see cref="Closed"/> — then Back
/// closes the overlay instead of changing scene, so a run in progress survives.
/// </summary>
public partial class SettingsMenu : Control
{
    /// <summary>When true, Back fires <see cref="Closed"/> instead of returning
    /// to the main menu. Set by the pause menu before instancing.</summary>
    public bool OverlayMode = false;

    /// <summary>Raised when Back is pressed in <see cref="OverlayMode"/>.</summary>
    public event Action Closed;

    OptionButton resolutionOption;
    OptionButton modeOption;
    CheckButton vsyncCheck;

    // Guards the change handlers while we seed control values programmatically,
    // so syncing the UI can't loop back into apply/save.
    bool syncing;

    public override void _Ready()
    {
        // As a standalone screen, scale up to the window so it's readable at high
        // resolutions. As a pause overlay, leave gameplay's native scaling alone.
        if (!OverlayMode) GameSettings.EnableUiScaling(GetWindow());

        resolutionOption = GetNode<OptionButton>("VBoxContainer/ResolutionRow/ResolutionOption");
        modeOption = GetNode<OptionButton>("VBoxContainer/ModeRow/ModeOption");
        vsyncCheck = GetNode<CheckButton>("VBoxContainer/VSyncRow/VSyncCheck");

        // Static item lists (AddItem doesn't emit a selection).
        resolutionOption.Clear();
        foreach (GameSettings.Resolution r in GameSettings.Resolutions)
            resolutionOption.AddItem(r.ToString());

        modeOption.Clear();
        foreach (string name in GameSettings.ModeNames)
            modeOption.AddItem(name);

        // Wire handlers, then seed values (guarded) so seeding can't feed back.
        resolutionOption.ItemSelected += OnResolutionSelected;
        modeOption.ItemSelected += OnModeSelected;
        vsyncCheck.Toggled += OnVSyncToggled;

        SyncControls();
    }

    /// <summary>Pushes the current <see cref="GameSettings"/> values into the
    /// controls without triggering the change handlers.</summary>
    void SyncControls()
    {
        syncing = true;
        resolutionOption.Selected = GameSettings.ResolutionIndex;
        modeOption.Selected = (int)GameSettings.Mode;
        vsyncCheck.ButtonPressed = GameSettings.VSync;
        UpdateResolutionEnabled();
        syncing = false;
    }

    void OnResolutionSelected(long index)
    {
        if (syncing) return;
        GameSettings.SetResolutionIndex((int)index);
    }

    void OnModeSelected(long index)
    {
        if (syncing) return;
        GameSettings.SetMode((GameSettings.DisplayMode)(int)index);
        UpdateResolutionEnabled();
    }

    void OnVSyncToggled(bool enabled)
    {
        if (syncing) return;
        GameSettings.SetVSync(enabled);
    }

    /// <summary>Resolution has no effect in fullscreen (native size), so grey it out.</summary>
    void UpdateResolutionEnabled()
        => resolutionOption.Disabled = GameSettings.Mode == GameSettings.DisplayMode.Fullscreen;

    public void OnResetPressed()
    {
        GameSettings.ResetToDefaults();
        SyncControls();
    }

    public void OnBackPressed()
    {
        if (OverlayMode)
        {
            Closed?.Invoke();
            return;
        }
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
