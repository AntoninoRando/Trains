using System;
using System.Collections.Generic;

public abstract class Model
{
    readonly Dictionary<Type, object> views = [];

    public void SetView<T>(T view) where T : class
    {
        views[typeof(T)] = view;
    }

    public T GetView<T>() where T : class
    {
        if (views.TryGetValue(typeof(T), out var view))
        {
            return view as T;
        }
        return null;
    }
}