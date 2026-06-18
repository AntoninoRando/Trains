/// <summary>
/// A collectible that a train scoops up as it rolls over it. Picking it up
/// couples a wagon (carrying the orb) to the train's tail: the train gets
/// longer and easier to hit, but earns extra gold if it reaches the end.
/// </summary>
public class MysticalOrb
{
    #region FIELDS -------------------------------------------------------------
    /// <summary>Extra gold this orb is worth when carried to the end.</summary>
    public int GoldValue = 10;

    bool collected;
    public bool Collected => collected;
    #endregion -----------------------------------------------------------------



    #region PUBLIC METHODS -----------------------------------------------------
    /// <summary>
    /// Tries to hand the orb to <paramref name="train"/>. Succeeds only once;
    /// on success a wagon is coupled to the train and true is returned so the
    /// view can play the pickup effect and disappear.
    /// </summary>
    public bool TryCollect(Train train)
    {
        if (collected || train == null) return false;

        collected = true;
        train.AttachWagon(new Wagon { GoldValue = GoldValue });
        return true;
    }
    #endregion -----------------------------------------------------------------
}
