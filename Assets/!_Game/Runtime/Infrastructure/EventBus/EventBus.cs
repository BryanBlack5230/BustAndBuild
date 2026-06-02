using System;
using System.Collections.Generic;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure
{
    public static class EventBus
    {
        public delegate void EventHandler<T>(in T evt) where T : IEvent;

        private static int _depth;
        private static readonly List<Action> Drainers = new(16);
        private static readonly List<Action> Clearers = new(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            _depth = 0;
            for (var i = 0; i < Clearers.Count; i++)
            {
                try { Clearers[i](); }
                catch (Exception e) { Log.Default.E(e); }
            }
        }

        public static IDisposable Subscribe<T>(EventHandler<T> handler, int priority = 0) where T : IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (_depth > 0) Listeners<T>.EnqueueAdd(handler, priority);
            else Listeners<T>.Add(handler, priority);

            return new Subscription<T>(handler);
        }

        public static void Unsubscribe<T>(EventHandler<T> handler) where T : IEvent
        {
            if (handler == null) return;

            if (_depth > 0) Listeners<T>.EnqueueRemove(handler);
            else Listeners<T>.Remove(handler);
        }

        public static void Raise<T>(in T evt) where T : IEvent
        {
            var list = Listeners<T>.Sorted;
            var count = list.Count;
            if (count == 0) return;

            _depth++;
            try
            {
                for (var i = 0; i < count; i++)
                {
                    try { list[i].Handler(in evt); }
                    catch (Exception e) { Log.Default.E(e); }
                }
            }
            finally
            {
                _depth--;
                if (_depth == 0) DrainAll();
            }
        }

        internal static void RegisterType(Action drainer, Action clearer)
        {
            Drainers.Add(drainer);
            Clearers.Add(clearer);
        }

        private static void DrainAll()
        {
            for (var i = 0; i < Drainers.Count; i++)
            {
                try { Drainers[i](); }
                catch (Exception e) { Log.Default.E(e); }
            }
        }

        private static class Listeners<T> where T : IEvent
        {
            public static readonly List<Listener> Sorted = new(4);
            private static readonly List<PendingOp> Pending = new(4);
            private static bool _hasPending;

            static Listeners()
            {
                RegisterType(Drain, Clear);
            }

            public static void Add(EventHandler<T> handler, int priority)
            {
                var index = FindInsertIndex(priority);
                Sorted.Insert(index, new Listener(handler, priority));
            }

            public static void Remove(EventHandler<T> handler)
            {
                for (var i = Sorted.Count - 1; i >= 0; i--)
                {
                    if (!Sorted[i].Handler.Equals(handler)) continue;
                    Sorted.RemoveAt(i);
                    return;
                }
            }

            public static void EnqueueAdd(EventHandler<T> handler, int priority)
            {
                Pending.Add(new PendingOp(isAdd: true, handler, priority));
                _hasPending = true;
            }

            public static void EnqueueRemove(EventHandler<T> handler)
            {
                Pending.Add(new PendingOp(isAdd: false, handler, 0));
                _hasPending = true;
            }

            private static void Drain()
            {
                if (!_hasPending) return;

                for (var i = 0; i < Pending.Count; i++)
                {
                    var op = Pending[i];
                    if (op.IsAdd) Add(op.Handler, op.Priority);
                    else Remove(op.Handler);
                }

                Pending.Clear();
                _hasPending = false;
            }

            private static void Clear()
            {
                Sorted.Clear();
                Pending.Clear();
                _hasPending = false;
            }

            private static int FindInsertIndex(int priority)
            {
                // Sorted descending by priority. Equal-priority insertions go AFTER older
                // ones, so listeners with the same priority fire in subscription order.
                var start = 0;
                var end = Sorted.Count;
                while (end > start)
                {
                    var mid = start + ((end - start) >> 1);
                    if (Sorted[mid].Priority >= priority) start = mid + 1;
                    else end = mid;
                }
                return start;
            }

            public readonly struct Listener
            {
                public readonly EventHandler<T> Handler;
                public readonly int Priority;

                public Listener(EventHandler<T> handler, int priority)
                {
                    Handler = handler;
                    Priority = priority;
                }
            }

            private readonly struct PendingOp
            {
                public readonly bool IsAdd;
                public readonly EventHandler<T> Handler;
                public readonly int Priority;

                public PendingOp(bool isAdd, EventHandler<T> handler, int priority)
                {
                    IsAdd = isAdd;
                    Handler = handler;
                    Priority = priority;
                }
            }
        }

        private sealed class Subscription<T> : IDisposable where T : IEvent
        {
            private EventHandler<T> _handler;

            public Subscription(EventHandler<T> handler) => _handler = handler;

            public void Dispose()
            {
                if (_handler == null) return;
                Unsubscribe(_handler);
                _handler = null;
            }
        }
    }
}
