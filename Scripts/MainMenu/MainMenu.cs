using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
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
        GD.Print("Settings pressed");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
