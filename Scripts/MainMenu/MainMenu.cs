using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        // Apply the saved display settings (resolution / mode / VSync) at launch.
        // The main menu is the first scene loaded, so this runs once on boot.
        GameSettings.Apply();

        // Scale this menu up to the window so it stays readable at high resolutions.
        GameSettings.EnableUiScaling(GetWindow());

        // Show the player's persistent gold so the shop's prices make sense.
        var goldLabel = GetNodeOrNull<Label>("GoldLabel");
        if (goldLabel != null) goldLabel.Text = $"Gold: {PlayerProfile.Gold}";
    }

    public void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }

    public void OnShopPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Shop.tscn");
    }

    public void OnSettingsPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Settings.tscn");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
