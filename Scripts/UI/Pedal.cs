using Godot;

public partial class Pedal : Node2D
{
    #region EXPORT FIELDS ------------------------------------------------------
    [Export] private AnimatedSprite2D pedalBackground;
    [Export] private AnimatedSprite2D pedalForeground;
    [Export] private RichTextLabel KeyLabel;
    #endregion -----------------------------------------------------------------


    public void AssignToPath(Path path)
    {
        path.SprintStarted += Push;
        path.SprintStopped += Release;
    }

    public void ShowKey(string key)
    {
        KeyLabel.Visible = true;
        KeyLabel.Text = key;
    }

    public void Push()
    {
        pedalBackground.Play("Push");
        pedalForeground.Play("Push");
    }

    public void Release()
    {
        pedalBackground.Play("Release");
        pedalForeground.Play("Release");
    }
}
