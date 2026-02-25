using System.Collections.Generic;

public partial class StageElementsSpawner
{
    readonly List<StageElement> spawnedElements = [];

    public void SpawnElement(Path path)
    {
        var element = StageElementsDirectory.Mine;

        path.AddChild(element);
        if (path.IsNodeReady())
        {
            /*
                May not be ready yet, and thus have no points.
            */
            AddElementToPath(path, element);
        }
        else
        {
            path.Ready += () => AddElementToPath(path, element);
        }
    }


    private void AddElementToPath(Path path, StageElement element)
    {
        element.GlobalPosition = path.Points[Game.RNG.Next(path.Points.Length)];
        spawnedElements.Add(element);

        Log.Info("Stage Element Spawned", new()
        {
            { "Element", element.Name },
            { "Path", path.Name },
            { "Position", element.GlobalPosition }
        });
    }
}
