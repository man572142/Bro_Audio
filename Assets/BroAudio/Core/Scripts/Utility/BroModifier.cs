using System;
using System.Collections.Generic;

public abstract class BroModifier : IDisposable
{
    private List<Action> _resetActions = null;

    public void Dispose()
    {
        if (_resetActions != null)
        {
            foreach (var act in _resetActions)
            {
                act?.Invoke();
            }
        }
    }

    protected void AddResetAction(Action action)
    {
        _resetActions ??= new List<Action>();
        _resetActions.Add(action);
    }
}