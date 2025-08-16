using Godot;
using System;

public partial class InputListener : Node
{
    [Export] AutoMover Train1;

    bool pressing = false;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsKeyPressed(Key.Key1))
        {
            if (!pressing)
            {
                Train1.Speed *= 2;
                pressing = true;
            }
        }
        else
        {
            if (pressing)
            {
                Train1.Speed /= 2;
                pressing = false;
            }
        }
    }

}
