
public class MatchOrchestrator
{
    Stage stage;
    TrainsSpawner trainsSpawner;
    StageElementsSpawner stageElementsSpawner;

    public MatchOrchestrator()
    {
        Game.NewContext += OnNewContext;
    }


    private void OnNewContext(object context)
    {
        if (context is Stage s)
        {
            stage = s;
            stage.Starting += OnStageStarting;
            stageElementsSpawner = new StageElementsSpawner();
            trainsSpawner = new TrainsSpawner
            {
                SpawnedContainer = stage.PathsContainer
            };
            stage.AddChild(trainsSpawner); // Needed to process
            trainsSpawner.SpawnedTrain += stage.RegisterTrain;
        }
    }

    private void OnStageStarting()
    {
        trainsSpawner.StartStage();
        foreach (var (_, path, _) in trainsSpawner.TrainsData)
        {
            stageElementsSpawner.SpawnElement(path);
        }
    }
}