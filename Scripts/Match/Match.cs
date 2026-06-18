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

    readonly Wallet wallet = new(); public Wallet Wallet => wallet;

    /// <summary>Gold earned just for getting a train to the end.</summary>
    public int BaseArrivalGold = 5;

    public event Action Started;
    public event Action Ended;


    /// <summary>
    /// Pays out when a train reaches the end: a base reward plus the gold value
    /// of every Mystical Orb the train carried there on its wagons.
    /// </summary>
    public void RewardArrival(Train train)
    {
        int reward = BaseArrivalGold;
        if (train != null)
            foreach (var wagon in train.Wagons)
                reward += wagon.GoldValue;

        wallet.Add(reward);
        Log.Info($"Train arrived carrying {train?.Wagons.Count ?? 0} orb(s); awarded {reward} gold (total {wallet.Gold}).");
    }


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

