using System;
using System.Collections.Generic;
using Godot;

public partial class Stage : Node
{
    [Export] TrainsSpawner trainsSpawner;
    private readonly List<(Path, string)> paths = [];
    const int PATHS_LIMIT = 5;
    PackedScene keyLabel = GD.Load<PackedScene>("res://Scenes/KeyLabel.tscn");

    readonly List<Control> keyLabels = [];
    readonly Queue<string> labelQueue = new();
    bool spawningLabel = false;

    public event Action<string> KeyRegistered;

    public override void _Ready()
    {
        trainsSpawner.SpawnedTrain += RegisterTrain;
    }

    public override void _Input(InputEvent @event)
    {
        if (paths.Count == 0) return;

        foreach (var item in paths)
        {
            (var path, var actionKey) = item;
            if (Input.IsActionPressed(actionKey)) path.Sprint();
            else path.StopSprint();
        }
    }

    void RegisterTrain(Train train, Path path)
    {
        if (paths.Count >= PATHS_LIMIT) return;

        var n = paths.Count + 1;
        var action_key = $"train_{n}";
        KeyRegistered?.Invoke(action_key);
        paths.Add((path, action_key));

        labelQueue.Enqueue(action_key);
        TrySpawnLabel();
    }

    async void TrySpawnLabel()
    {
        if (spawningLabel || labelQueue.Count == 0) return;

        spawningLabel = true;

        var action = labelQueue.Dequeue();
        var label = keyLabel.Instantiate<Control>();
        label.GetNode<RichTextLabel>("Label").Text = action;
        AddChild(label);
        keyLabels.Add(label);

        var viewport = GetViewport().GetVisibleRect();
        const float size = 40f;
        const float spacing = 50f;
        var startY = viewport.Size.Y;
        var finalY = viewport.Size.Y - size - 50;

        // start at bottom center
        label.Position = new Vector2(viewport.Size.X / 2 - size / 2, startY);

        // final horizontal positions for all labels
        var totalWidth = keyLabels.Count * (size + spacing) - spacing;
        var left = viewport.Size.X / 2 - totalWidth / 2;

        for (int i = 0; i < keyLabels.Count; i++)
        {
            var targetX = left + i * (size + spacing);
            var tween = CreateTween();
            if (keyLabels[i] == label)
            {
                tween.TweenProperty(keyLabels[i], "position", new Vector2(targetX, finalY), 0.3);
                await ToSignal(tween, Tween.SignalName.Finished);
            }
            else
            {
                tween.TweenProperty(keyLabels[i], "position:x", targetX, 0.3);
            }
        }

        spawningLabel = false;
        TrySpawnLabel();
    }
}