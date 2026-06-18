using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// The player's persistent progression, surviving across runs and app restarts:
/// gold banked from finished runs, the set of shop items unlocked, and which
/// track style / train livery is equipped. Persisted to <c>user://profile.cfg</c>.
///
/// Static (like <see cref="GameAssets"/>) so any scene can read it without an
/// autoload. During a run the gameplay never touches this directly — the run's
/// <see cref="Wallet"/> tracks live earnings, which are banked here via
/// <see cref="AddGold"/> when the run ends. The shop spends from here.
/// </summary>
public static class PlayerProfile
{
    const string SavePath = "user://profile.cfg";
    const string Section = "progress";
    const string DefaultTrack = "track_classic";
    const string DefaultSkin = "skin_default";

    static int gold;
    static readonly HashSet<string> unlocked = new();
    static string equippedTrack = DefaultTrack;
    static string equippedSkin = DefaultSkin;
    static bool loaded;



    #region LOAD / SAVE --------------------------------------------------------
    /// <summary>Loads the profile on first access so callers never see stale data.</summary>
    static void EnsureLoaded()
    {
        if (!loaded) Load();
    }

    public static void Load()
    {
        loaded = true;
        unlocked.Clear();
        gold = 0;
        equippedTrack = DefaultTrack;
        equippedSkin = DefaultSkin;

        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok) return; // first run: keep defaults

        gold = cfg.GetValue(Section, "gold", 0).AsInt32();
        string ids = cfg.GetValue(Section, "unlocked", "").AsString();
        foreach (var id in ids.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            unlocked.Add(id);
        equippedTrack = cfg.GetValue(Section, "track", DefaultTrack).AsString();
        equippedSkin = cfg.GetValue(Section, "skin", DefaultSkin).AsString();
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue(Section, "gold", gold);
        cfg.SetValue(Section, "unlocked", string.Join("\n", unlocked));
        cfg.SetValue(Section, "track", equippedTrack);
        cfg.SetValue(Section, "skin", equippedSkin);
        if (cfg.Save(SavePath) != Error.Ok)
            Log.Warning($"[PlayerProfile] could not save to {SavePath}");
    }
    #endregion -----------------------------------------------------------------



    #region GOLD ---------------------------------------------------------------
    public static int Gold { get { EnsureLoaded(); return gold; } }

    /// <summary>Banks gold earned during a run into the persistent total.</summary>
    public static void AddGold(int amount)
    {
        EnsureLoaded();
        if (amount <= 0) return;
        gold += amount;
        Save();
    }
    #endregion -----------------------------------------------------------------



    #region OWNERSHIP / PURCHASE -----------------------------------------------
    public static bool IsUnlocked(string id)
    {
        EnsureLoaded();
        var item = ShopCatalog.Get(id);
        if (item != null && item.DefaultOwned) return true;
        return unlocked.Contains(id);
    }

    /// <summary>True if the item is buyable right now (not owned, prereq met, affordable).</summary>
    public static bool CanPurchase(ShopItem item)
    {
        if (item == null) return false;
        EnsureLoaded();
        if (IsUnlocked(item.Id)) return false;
        if (item.Prereq != null && !IsUnlocked(item.Prereq)) return false;
        return gold >= item.Cost;
    }

    /// <summary>Buys an item if <see cref="CanPurchase"/>; equippables auto-equip.</summary>
    public static bool TryPurchase(ShopItem item)
    {
        if (!CanPurchase(item)) return false;

        gold -= item.Cost;
        unlocked.Add(item.Id);
        if (item.Equippable) Equip(item.Id);
        Save();
        Log.Info($"Bought '{item.Name}' for {item.Cost} gold (remaining {gold}).");
        return true;
    }
    #endregion -----------------------------------------------------------------



    #region EQUIP --------------------------------------------------------------
    public static string EquippedTrack { get { EnsureLoaded(); return equippedTrack; } }
    public static string EquippedSkin { get { EnsureLoaded(); return equippedSkin; } }

    public static void Equip(string id)
    {
        EnsureLoaded();
        var item = ShopCatalog.Get(id);
        if (item == null || !item.Equippable || !IsUnlocked(id)) return;

        if (item.Category == ShopCategory.Track) equippedTrack = id;
        else if (item.Category == ShopCategory.Cosmetic) equippedSkin = id;
        Save();
    }

    public static bool IsEquipped(string id)
    {
        EnsureLoaded();
        return id == equippedTrack || id == equippedSkin;
    }
    #endregion -----------------------------------------------------------------



    #region DERIVED GAMEPLAY EFFECTS -------------------------------------------
    /// <summary>Sum of <see cref="ShopItem.Value"/> over every owned item with this effect.</summary>
    static double SumEffect(string effect)
    {
        EnsureLoaded();
        double sum = 0;
        foreach (var item in ShopCatalog.Items)
            if (item.Effect == effect && IsUnlocked(item.Id))
                sum += item.Value;
        return sum;
    }

    /// <summary>Extra base gold per arrival from upgrades.</summary>
    public static int BaseGoldBonus => (int)SumEffect("base_gold");

    /// <summary>Extra sprint multiplier from upgrades.</summary>
    public static double SprintBonus => SumEffect("sprint");

    /// <summary>Extra gold per orb from upgrades plus the golden-orb bonus.</summary>
    public static int OrbValueBonus => (int)(SumEffect("orb_value") + SumEffect("golden_orb"));

    /// <summary>Orbs render gold and pay their bonus when this is owned.</summary>
    public static bool GoldenOrbs => IsUnlocked("element_golden_orb");

    /// <summary>An extra orb is spawned on each path when this is owned.</summary>
    public static bool ExtraOrb => IsUnlocked("element_lucky_charm");

    /// <summary>Tint multiplied onto every train (white = default livery).</summary>
    public static Color TrainTint => ShopCatalog.Get(EquippedSkin)?.Tint ?? Colors.White;

    /// <summary>The equipped track item (classic = per-train identity colours).</summary>
    public static ShopItem TrackStyle => ShopCatalog.Get(EquippedTrack);
    #endregion -----------------------------------------------------------------
}
