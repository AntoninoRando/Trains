using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The single source of truth for everything that can be bought with gold.
/// Static, pure data (like <see cref="GameAssets"/> is for art). The shop UI is
/// generated from <see cref="Items"/>, and <see cref="PlayerProfile"/> resolves
/// the gameplay effects from the items the player owns.
/// </summary>
public static class ShopCatalog
{
    public static readonly IReadOnlyList<ShopItem> Items = new List<ShopItem>
    {
        // ---- Upgrades: permanent boosts read by the gameplay --------------
        new ShopItem { Id = "upgrade_base_gold_1", Name = "Richer Cargo I",
            Description = "+3 gold every time a train reaches the end.",
            Cost = 50, Category = ShopCategory.Upgrade, Effect = "base_gold", Value = 3 },
        new ShopItem { Id = "upgrade_base_gold_2", Name = "Richer Cargo II",
            Description = "+5 more arrival gold (stacks with I).",
            Cost = 140, Category = ShopCategory.Upgrade, Effect = "base_gold", Value = 5,
            Prereq = "upgrade_base_gold_1" },

        new ShopItem { Id = "upgrade_sprint_1", Name = "Turbo Boiler I",
            Description = "+0.5x sprint speed.",
            Cost = 90, Category = ShopCategory.Upgrade, Effect = "sprint", Value = 0.5 },
        new ShopItem { Id = "upgrade_sprint_2", Name = "Turbo Boiler II",
            Description = "+0.5x more sprint speed (stacks with I).",
            Cost = 200, Category = ShopCategory.Upgrade, Effect = "sprint", Value = 0.5,
            Prereq = "upgrade_sprint_1" },

        new ShopItem { Id = "upgrade_orb_1", Name = "Orb Polish I",
            Description = "Mystical Orbs are worth +5 gold.",
            Cost = 70, Category = ShopCategory.Upgrade, Effect = "orb_value", Value = 5 },
        new ShopItem { Id = "upgrade_orb_2", Name = "Orb Polish II",
            Description = "Mystical Orbs are worth +10 more gold (stacks with I).",
            Cost = 160, Category = ShopCategory.Upgrade, Effect = "orb_value", Value = 10,
            Prereq = "upgrade_orb_1" },

        new ShopItem { Id = "upgrade_gate_open_1", Name = "Gate Greaser",
            Description = "Rhythm Gates stay open one extra beat.",
            Cost = 150, Category = ShopCategory.Upgrade, Effect = "gate_open", Value = 1,
            Prereq = "element_rhythm_gates" },

        new ShopItem { Id = "upgrade_smoke_shrink_1", Name = "Fog Lamps",
            Description = "Smoke clouds are smaller, so a train is hidden for less of the track.",
            Cost = 140, Category = ShopCategory.Upgrade, Effect = "smoke_shrink", Value = 24,
            Prereq = "element_smoke" },

        // ---- Stage elements: change what appears in a run -----------------
        new ShopItem { Id = "element_golden_orb", Name = "Golden Orbs",
            Description = "Orbs turn gold and pay an extra +15 gold each.",
            Cost = 220, Category = ShopCategory.StageElement, Effect = "golden_orb", Value = 15 },
        new ShopItem { Id = "element_lucky_charm", Name = "Lucky Charm",
            Description = "An extra Mystical Orb appears on every path.",
            Cost = 260, Category = ShopCategory.StageElement, Effect = "extra_orb" },
        new ShopItem { Id = "element_rhythm_gates", Name = "Rhythm Gates",
            Description = "Beat-timed gates appear on every path: time your sprint to roll through while they're open.",
            Cost = 200, Category = ShopCategory.StageElement, Effect = "rhythm_gates" },
        new ShopItem { Id = "element_smoke", Name = "Smoke Screens",
            Description = "Drifting smoke clouds sit on every path and hide any train that rolls through them.",
            Cost = 180, Category = ShopCategory.StageElement, Effect = "smoke" },

        // ---- Tracks: equippable rail styles -------------------------------
        new ShopItem { Id = "track_classic", Name = "Classic Rails",
            Description = "The original per-train coloured rails.",
            Cost = 0, Category = ShopCategory.Track, Equippable = true, DefaultOwned = true,
            Tint = Colors.White },
        new ShopItem { Id = "track_gold", Name = "Golden Rails",
            Description = "Gleaming gold-tinted railroad.",
            Cost = 180, Category = ShopCategory.Track, Equippable = true,
            Tint = new Color(1f, 0.84f, 0.30f) },
        new ShopItem { Id = "track_neon", Name = "Neon Rails",
            Description = "Vivid cyan neon railroad.",
            Cost = 180, Category = ShopCategory.Track, Equippable = true,
            Tint = new Color(0.25f, 1f, 0.95f) },
        new ShopItem { Id = "track_crimson", Name = "Crimson Rails",
            Description = "Deep crimson railroad.",
            Cost = 150, Category = ShopCategory.Track, Equippable = true,
            Tint = new Color(1f, 0.30f, 0.35f) },

        // ---- Cosmetics: equippable train liveries -------------------------
        new ShopItem { Id = "skin_default", Name = "Default Livery",
            Description = "Standard identity colours.",
            Cost = 0, Category = ShopCategory.Cosmetic, Equippable = true, DefaultOwned = true,
            Tint = Colors.White },
        new ShopItem { Id = "skin_gold", Name = "Gold Plated",
            Description = "A warm gilded sheen over every train.",
            Cost = 150, Category = ShopCategory.Cosmetic, Equippable = true,
            Tint = new Color(1f, 0.86f, 0.45f) },
        new ShopItem { Id = "skin_midnight", Name = "Midnight",
            Description = "Cool dark-blue liveries.",
            Cost = 150, Category = ShopCategory.Cosmetic, Equippable = true,
            Tint = new Color(0.55f, 0.62f, 0.95f) },
        new ShopItem { Id = "skin_toxic", Name = "Toxic",
            Description = "Radiant toxic-green liveries.",
            Cost = 150, Category = ShopCategory.Cosmetic, Equippable = true,
            Tint = new Color(0.55f, 1f, 0.45f) },
    };

    static readonly Dictionary<string, ShopItem> byId = Items.ToDictionary(i => i.Id);

    /// <summary>Looks up an item by id, or null if there is no such item.</summary>
    public static ShopItem Get(string id)
        => id != null && byId.TryGetValue(id, out var item) ? item : null;

    /// <summary>Every item in a category, in catalog order.</summary>
    public static IEnumerable<ShopItem> ByCategory(ShopCategory category)
        => Items.Where(i => i.Category == category);
}
