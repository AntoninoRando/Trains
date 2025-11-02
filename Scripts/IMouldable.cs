using System;
using System.Collections.Generic;

public interface IMouldable
{
    public void SetView<T>(T view)
    {
        ModelViews.views[typeof(T)] = view;
    }

    public T GetView<T>() where T : class
    {
        if (ModelViews.views.TryGetValue(typeof(T), out var view))
        {
            return view as T;
        }
        return null;
    }
}

public static class ModelViews
{
    public static readonly Dictionary<Type, object> views = new();
}