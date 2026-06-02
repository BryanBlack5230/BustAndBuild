using System;
using System.Collections.Generic;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Commands
{
    public sealed class CommandDispatcher
    {
        private readonly Dictionary<Type, object> _handlersByType = new();

        public void Send<T>(in T command) where T : ICommand
        {
            if (!_handlersByType.TryGetValue(typeof(T), out var stored)) return;

            var list = (List<Action<T>>)stored;
            for (var i = 0; i < list.Count; i++)
            {
                try { list[i](command); }
                catch (Exception e) { Log.Default.E(e); }
            }
        }

        public IDisposable Register<T>(Action<T> handler) where T : ICommand
        {
            GetOrCreate<T>().Add(handler);
            return new Subscription<T>(this, handler);
        }

        private List<Action<T>> GetOrCreate<T>() where T : ICommand
        {
            if (!_handlersByType.TryGetValue(typeof(T), out var stored))
            {
                stored = new List<Action<T>>(4);
                _handlersByType[typeof(T)] = stored;
            }
            return (List<Action<T>>)stored;
        }

        private void Unregister<T>(Action<T> handler) where T : ICommand
        {
            if (_handlersByType.TryGetValue(typeof(T), out var stored))
                ((List<Action<T>>)stored).Remove(handler);
        }

        private sealed class Subscription<T> : IDisposable where T : ICommand
        {
            private readonly CommandDispatcher _dispatcher;
            private readonly Action<T> _handler;
            private bool _disposed;

            public Subscription(CommandDispatcher dispatcher, Action<T> handler)
            {
                _dispatcher = dispatcher;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _dispatcher.Unregister(_handler);
            }
        }
    }
}
