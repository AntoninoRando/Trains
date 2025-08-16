using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InputListener : Node
{
    // [Export] AutoMover Train1;

    // bool pressing = false;

    // public override void _Input(InputEvent @event)
    // {
    //     if (Input.IsKeyPressed(Key.Key1))
    //     {
    //         if (!pressing)
    //         {
    //             Train1.Speed *= 2;
    //             pressing = true;
    //         }
    //     }
    //     else
    //     {
    //         if (pressing)
    //         {
    //             Train1.Speed /= 2;
    //             pressing = false;
    //         }
    //     }
    // }

    public readonly static HashSet<Key> FreeKeys = [Key.Key1, Key.Key2, Key.Key3];

    public static Key TakeKey()
    {
        var x = FreeKeys.Take(1).First();
        FreeKeys.Remove(x);
        return x;
    }

}
