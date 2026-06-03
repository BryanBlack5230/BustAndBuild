using System;
using System.Collections.Generic;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Commands
{
    /// <summary>
    /// Type-keyed request hub. Each command type maps to <b>exactly one</b> handler — a second
    /// <see cref="Register{T}"/> for the same type throws. For fan-out, raise an event via
    /// <c>EventBus</c> instead.
    /// <para/>
    /// <b>When to use what:</b>
    /// <list type="bullet">
    ///   <item><b>Direct DI inject</b> — sender already holds (or can hold) the service. Cheapest, most readable. Default choice.</item>
    ///   <item><b>CommandDispatcher.Send</b> — "do X" where the sender shouldn't reference the handler's layer
    ///     (Input → Gameplay, UI → Domain), or where the command is data you'll queue / log / replay / network-sync.
    ///     If none of those apply, just inject the service.</item>
    ///   <item><b>EventBus.Raise</b> — "X happened", multiple unknown listeners may care. Past tense, no required receiver.</item>
    /// </list>
    /// </summary>
    public sealed class CommandDispatcher
    {
        private readonly Dictionary<Type, object> _handlerByType = new();

        public void Send<T>(in T command) where T : ICommand
        {
            if (!_handlerByType.TryGetValue(typeof(T), out var stored)) return;

            try { ((Action<T>)stored)(command); }
            catch (Exception e) { Log.Default.E(e); }
        }

        public IDisposable Register<T>(Action<T> handler) where T : ICommand
        {
            if (_handlerByType.ContainsKey(typeof(T)))
                throw new InvalidOperationException(
                    $"Command '{typeof(T).Name}' already has a registered handler. " +
                    "Commands are 1-to-1; use EventBus for fan-out.");

            _handlerByType[typeof(T)] = handler;
            return new Subscription<T>(this, handler);
        }

        private void Unregister<T>(Action<T> handler) where T : ICommand
        {
            // Only remove if the stored handler is the one we registered — guards against
            // disposing a stale subscription clobbering a later registration for the same type.
            if (_handlerByType.TryGetValue(typeof(T), out var stored) && (Action<T>)stored == handler)
                _handlerByType.Remove(typeof(T));
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
