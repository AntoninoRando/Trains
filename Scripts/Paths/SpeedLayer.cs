
using System;

public class SpeedLayer
{
    public string Id { get; }
    public int Priority { get; }
    public Func<double, double> Transform { get; }

    public SpeedLayer(string id, int priority, Func<double, double> transform)
    {
        Id = id;
        Priority = priority;
        Transform = transform;
    }
}
