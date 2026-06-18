using System;
using Godot;

/// <summary>
/// In-game pause overlay. Toggled by the "pause" action (Esc), it pauses the
/// whole scene tree and offers Resume, Settings, and Main Menu. Settings opens
/// as an in-place overlay so the run in progress is preserved.
///
/// The node runs with <see cref="Node.ProcessModeEnum.Always"/> so it keeps
/// receiving input (and its buttons stay clickable) while the rest of the game
/// is paused — that's how it can pause and un-pause itself.
///
/// Main Menu is delegated to the match via <see cref="ExitRequested"/> so the
/// existing interrupt/earnings-banking path is reused.
/// </summary>
public partial class PauseMenu : Control
{
    /// <summary>Raised when the player chooses Main Menu (after un-pausing).</summary>
    public event Action ExitRequested;

    static readonly PackedScene SettingsScene = GD.Load<PackedScene>("res://Scenes/Settings.tscn");
    SettingsMenu settingsOverlay;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // stay live while the tree is paused
        Visible = false;

        GetNode<Button>("VBox/ResumeButton").Pressed += Resume;
        GetNode<Button>("VBox/SettingsButton").Pressed += OpenSettings;
        GetNode<Button>("VBox/MainMenuButton").Pressed += GoToMainMenu;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause")) return;

        // Esc steps back one layer: settings -> pause menu -> resume.
        if (settingsOverlay != null) CloseSettings();
        else if (Visible) Resume();
        else Open();

        GetViewport().SetInputAsHandled();
    }

    void Open()
    {
        Visible = true;
        GetTree().Paused = true;
    }

    void Resume()
    {
        CloseSettings();
        Visible = false;
        GetTree().Paused = false;
    }

    void OpenSettings()
    {
        if (settingsOverlay != null) return;

        settingsOverlay = SettingsScene.Instantiate<SettingsMenu>();
        settingsOverlay.OverlayMode = true;          // Back closes, doesn't change scene
        settingsOverlay.Closed += CloseSettings;
        AddChild(settingsOverlay);                   // drawn above the pause buttons
    }

    void CloseSettings()
    {
        if (settingsOverlay == null) return;
        settingsOverlay.Closed -= CloseSettings;
        settingsOverlay.QueueFree();
        settingsOverlay = null;
    }

    void GoToMainMenu()
    {
        // Un-pause first so the menu scene we hand off to isn't frozen.
        GetTree().Paused = false;
        ExitRequested?.Invoke();
    }
}
