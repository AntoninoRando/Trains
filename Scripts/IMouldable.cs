using System;
using System.Collections.Generic;

public interface IMouldable
{
    public void SetView<T>(T view)
    {
        // Keyed by (model instance, view type): each model has its own view.
        // Keying by type alone would make every new instance (e.g. the next
        // stage, or each spawned train) overwrite the previous one's view.
        ModelViews.views[(this, typeof(T))] = view;
    }

    public T GetView<T>() where T : class
    {
        if (ModelViews.views.TryGetValue((this, typeof(T)), out var view))
        {
            return view as T;
        }
        return null;
    }
}

public static class ModelViews
{
    public static readonly Dictionary<(object Model, Type ViewType), object> views = new();
}
