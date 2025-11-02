using System;

/// <summary>
/// Represents a match encompassing several levels or rounds. That is, the
/// entire game session from the press of play button to the win, defeat, or
/// interrupt.
/// </summary>
public class Match
{
    Stage stage; public Stage Stage => stage;
    int stageNumber = 0; public int StageNumber => stageNumber;
    Train winningTrain = null; public Train WinningTrain => winningTrain;

    public event Action Started;
    public event Action Ended;


    public void Start()
    {
        stageNumber++;
        winningTrain = null;
        Started?.Invoke();
    }

    public void End()
    {
        Ended?.Invoke();
    }

    public void ChangeStage(Stage newStage)
    {
        stage = newStage;
        winningTrain = null;
    }
}

