using Godot;

/// <summary>
/// The identity colours used to tell paths (and their trains) apart.
///
/// One colour per path/train slot, matching the train_1..train_5 numbering.
/// This is the single source of truth so the track tint and the train tint
/// always agree. Colours are kept light and saturated because they are applied
/// as <c>Modulate</c> (a multiply) over the grey pixel-art track, which keeps
/// the rails and ties readable while clearly tinting each line.
/// </summary>
public static class TrackPalette
{
    static readonly Color[] Palette =
    {
        new Color(1.00f, 0.46f, 0.42f), // 1 - coral red
        new Color(0.42f, 0.72f, 1.00f), // 2 - sky blue
        new Color(0.55f, 0.92f, 0.55f), // 3 - green
        new Color(1.00f, 0.82f, 0.38f), // 4 - amber
        new Color(0.82f, 0.56f, 1.00f), // 5 - violet
    };

    public static int Count => Palette.Length;

    /// <summary>Identity colour for a 0-based path/train slot (wraps if needed).</summary>
    public static Color For(int index)
    {
        if (index < 0) index = 0;
        return Palette[index % Palette.Length];
    }
}
