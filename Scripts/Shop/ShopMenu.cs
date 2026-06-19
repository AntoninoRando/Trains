using System;
using Godot;

/// <summary>
/// The shop reached from the main menu. The whole UI is built in code from
/// <see cref="ShopCatalog"/>, so adding a catalog entry is enough to make a new
/// row appear here — no scene editing required. Buying and equipping go through
/// <see cref="PlayerProfile"/>, which persists immediately; the list is rebuilt
/// after every action so prices, affordability and "Equipped" badges stay correct.
/// </summary>
public partial class ShopMenu : Control
{
    Label goldLabel;
    VBoxContainer list;

    #region GODOT LIFECYCLE ----------------------------------------------------
    public override void _Ready()
    {
        // Scale the shop up to the window so it stays readable at high resolutions.
        GameSettings.EnableUiScaling(GetWindow());
        BuildChrome();
        Refresh();
    }
    #endregion -----------------------------------------------------------------



    #region LAYOUT -------------------------------------------------------------
    /// <summary>Builds the static frame: background, header and scrolling list.</summary>
    void BuildChrome()
    {
        var bg = new ColorRect { Color = new Color(0.10f, 0.12f, 0.16f) };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.OffsetLeft = 24; root.OffsetTop = 18; root.OffsetRight = -24; root.OffsetBottom = -18;
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        // Header: title ........ gold .... Back
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);

        var title = new Label { Text = "Shop" };
        title.AddThemeFontSizeOverride("font_size", 34);
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        goldLabel = new Label();
        goldLabel.AddThemeFontSizeOverride("font_size", 24);
        goldLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.80f, 0.25f));
        header.AddChild(goldLabel);

        var back = new Button { Text = "Back" };
        back.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        header.AddChild(back);

        root.AddChild(header);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        root.AddChild(scroll);

        list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(list);
    }
    #endregion -----------------------------------------------------------------



    #region ITEM LIST ----------------------------------------------------------
    /// <summary>Rebuilds the gold readout and every category's rows from scratch.</summary>
    void Refresh()
    {
        goldLabel.Text = $"Gold: {PlayerProfile.Gold}";

        foreach (Node child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }

        foreach (ShopCategory category in Enum.GetValues<ShopCategory>())
        {
            var heading = new Label { Text = CategoryName(category) };
            heading.AddThemeFontSizeOverride("font_size", 20);
            heading.AddThemeColorOverride("font_color", new Color(0.60f, 0.80f, 1f));
            list.AddChild(heading);

            foreach (var item in ShopCatalog.ByCategory(category))
                list.AddChild(BuildRow(item));
        }
    }

    /// <summary>One item row: name + description on the left, an action button right.</summary>
    Control BuildRow(ShopItem item)
    {
        var panel = new PanelContainer();

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);
        panel.AddChild(row);

        var texts = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

        var name = new Label { Text = item.Name };
        name.AddThemeFontSizeOverride("font_size", 17);
        texts.AddChild(name);

        var desc = new Label
        {
            Text = item.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        desc.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.82f));
        texts.AddChild(desc);

        row.AddChild(texts);
        row.AddChild(BuildAction(item));
        return panel;
    }

    /// <summary>The right-hand button, whose label and behaviour depend on state.</summary>
    Button BuildAction(ShopItem item)
    {
        var button = new Button { CustomMinimumSize = new Vector2(130, 0) };

        if (PlayerProfile.IsUnlocked(item.Id))
        {
            if (!item.Equippable)
            {
                button.Text = "Owned";
                button.Disabled = true;
            }
            else if (PlayerProfile.IsEquipped(item.Id))
            {
                button.Text = "Equipped";
                button.Disabled = true;
            }
            else
            {
                button.Text = "Equip";
                button.Pressed += () => { PlayerProfile.Equip(item.Id); Refresh(); };
            }
            return button;
        }

        // Not owned yet.
        button.Text = item.Cost > 0 ? $"Buy ({item.Cost})" : "Get";

        bool prereqMet = item.Prereq == null || PlayerProfile.IsUnlocked(item.Prereq);
        if (!prereqMet)
        {
            button.Text = "Locked";
            button.Disabled = true;
        }
        else if (!PlayerProfile.CanPurchase(item))
        {
            button.Disabled = true; // prereq met but can't afford it yet
        }
        else
        {
            button.Pressed += () => { PlayerProfile.TryPurchase(item); Refresh(); };
        }

        return button;
    }

    static string CategoryName(ShopCategory category) => category switch
    {
        ShopCategory.Upgrade => "Upgrades",
        ShopCategory.StageElement => "Stage Elements",
        ShopCategory.Track => "Tracks",
        ShopCategory.Cosmetic => "Train Liveries",
        _ => category.ToString(),
    };
    #endregion -----------------------------------------------------------------
}
