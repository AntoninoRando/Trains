using System.Diagnostics;
using Godot;

public partial class CompleteAnimation : Node
{
    [Export] ColorRect Top;
    [Export] ColorRect Right;
    [Export] ColorRect Bottom;
    [Export] ColorRect Left;
    [Export] ColorRect Left2;



    public override void _Ready()
    {
        Top.Scale = new Vector2(0, 1);
        Right.Scale = new Vector2(1, 0);
        Bottom.Scale = new Vector2(0, 1);
        Left.Scale = new Vector2(1, 0);
        Left2.Scale = new Vector2(1, 0);
    }


    public void Play()
    {
        Debug.Assert(Top != null, "TOP frame not set");
        Debug.Assert(Right != null, "RIGHT frame not set");
        Debug.Assert(Bottom != null, "BOTTOM frame not set");
        Debug.Assert(Left != null, "LEFT frame not set");

        Top.Scale = new Vector2(0, 1);
        Right.Scale = new Vector2(1, 0);
        Bottom.Scale = new Vector2(0, 1);
        Left.Scale = new Vector2(1, 0);
        Left2.Scale = new Vector2(1, 0);

        PartOne().Finished += () =>
            PartTwo().Finished += () =>
                PartThree().Finished += () => PartFour();
    }


    Tween PartOne()
    {
        var tween = CreateTween();
        tween.TweenProperty(Right, "scale:y", 1, 0.4);
        return tween;
    }

    Tween PartTwo()
    {
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(Top, "scale:x", 1, 0.4);
        tween.TweenProperty(Bottom, "scale:x", 1, 0.4);
        return tween;
    }

    Tween PartThree()
    {
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(Left, "scale:y", 1, 0.4);
        tween.TweenProperty(Left2, "scale:y", 1, 0.4);
        return tween;
    }

    Tween PartFour()
    {
        // var tween = CreateTween();
        // tween.SetParallel();
        // tween.TweenProperty(Right, "scale:y", 0, 0.4);
        // tween.TweenProperty(Top, "scale:x", 0, 0.2);
        // tween.TweenProperty(Bottom, "scale:x", 0, 0.4);
        // tween.TweenProperty(Left, "scale:y", 0, 0.4);
        // tween.TweenProperty(Left2, "scale:y", 0, 0.4);
        // return tween;
        return null;
    }
}