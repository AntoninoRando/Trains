using System;

/// <summary>
/// Represents a match encompassing several levels or rounds. That is, the
/// entire game session from the press of play button to the win, defeat, or
/// interrupt.
/// </summary>
public class Match : IMouldable
{
    Stage stage; public Stage Stage => stage;
    int stageNumber = 0; public int StageNumber => stageNumber;
    Train winningTrain; public Train WinningTrain { get => winningTrain; set => winningTrain = value; }

    public event Action Started;
    public event Action Ended;


    public void Start()
    {
        stageNumber++;
        winningTrain = null;
        Started?.Invoke();
    }

    public void Interrupt()
    {
        Log.Info("Match interrupted");
        End();
    }

    public void Lose()
    {
        Log.Info("Match lost");
        End();
    }

    void End()
    {
        stage.StopTrains();
        Ended?.Invoke();
    }

    public void ChangeStage(Stage newStage)
    {
        stage = newStage;
    }
}

