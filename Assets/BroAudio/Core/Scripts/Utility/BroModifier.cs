using System;
using System.Collections.Generic;

namespace Ami.Extension
{
    public abstract class BroModifier<T> : IDisposable where T : class
    {
        private List<Action> _resetActions = null;
        protected T Base { get; private set; }

        protected BroModifier(T @base)
        {
            Base = @base;
        }

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

        protected void AddResetAction(ref bool hasAdded, Action action)
        {
            if (!hasAdded)
            {
                _resetActions ??= new List<Action>();
                _resetActions.Add(action);
                hasAdded = true;
            }
        }
    } 
}