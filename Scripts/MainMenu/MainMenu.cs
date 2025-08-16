using Godot;

public partial class MainMenu : Control
{
    public void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
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
