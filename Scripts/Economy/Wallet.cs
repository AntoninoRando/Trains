using System;

/// <summary>
/// Holds the player's gold for the current match. Pure logic: the view layer
/// subscribes to <see cref="Changed"/> to keep a HUD label in sync.
/// </summary>
public class Wallet
{
    int gold;
    public int Gold => gold;

    /// <summary>Raised with the new total whenever gold changes.</summary>
    public event Action<int> Changed;

    public void Add(int amount)
    {
        if (amount == 0) return;
        gold += amount;
        Changed?.Invoke(gold);
    }
}
