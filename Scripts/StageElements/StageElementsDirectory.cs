using Godot;

public static class StageElementsDirectory
{
    const string path = "res://Scenes/StageElements/";

    static public PackedScene SceneMine = GD.Load<PackedScene>(path + "Mine.tscn");
    static public Mine Mine => SceneMine.Instantiate<Mine>();
}