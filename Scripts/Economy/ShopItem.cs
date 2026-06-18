using Godot;

/// <summary>The four kinds of things gold can buy.</summary>
public enum ShopCategory { Upgrade, StageElement, Track, Cosmetic }

/// <summary>
/// One purchasable entry in the shop. Pure data: the gameplay never reads a
/// <see cref="ShopItem"/> directly, it reads the resolved effect through
/// <see cref="PlayerProfile"/> (e.g. <c>PlayerProfile.SprintBonus</c>). Adding a
/// new item here is enough to make it appear in the shop UI.
/// </summary>
public sealed class ShopItem
{
    #region IDENTITY -----------------------------------------------------------
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public ShopCategory Category;
    #endregion -----------------------------------------------------------------



    #region EFFECT -------------------------------------------------------------
    /// <summary>
    /// Effect key read by the gameplay for upgrades and stage elements:
    /// "base_gold", "sprint", "orb_value", "golden_orb", "extra_orb".
    /// Empty for pure cosmetics (their effect is the <see cref="Tint"/>).
    /// </summary>
    public string Effect = "";

    /// <summary>Numeric magnitude of <see cref="Effect"/> (e.g. +3 gold, +0.5x).</summary>
    public double Value;

    /// <summary>Tint for tracks and train liveries. White means "no change".</summary>
    public Color Tint = Colors.White;
    #endregion -----------------------------------------------------------------



    #region OWNERSHIP RULES ----------------------------------------------------
    /// <summary>An item that must be owned before this one can be bought (tiered
    /// upgrades), or null if it has no prerequisite.</summary>
    public string Prereq;

    /// <summary>Equippable items (tracks, liveries) can be selected after buying.</summary>
    public bool Equippable;

    /// <summary>Owned from the start at no cost (classic rails, default livery).</summary>
    public bool DefaultOwned;
    #endregion -----------------------------------------------------------------
}
