#nullable enable
using System;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    /// <summary>
    /// Calendar-time timer anchored to absolute UTC moments, so it keeps working
    /// across sessions and offline periods (beacon auto-repair, auto-save intervals,
    /// offline progression windows).
    /// </summary>
    public sealed class DateTimeTimer
    {
        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Duration { get; }

        public bool IsComplete => End <= DateTime.UtcNow;
        public bool IsStarted => Start <= DateTime.UtcNow;

        /// <summary>
        /// Time left until <see cref="End"/>. Full <see cref="Duration"/> when the
        /// start point is still in the future, zero once complete.
        /// </summary>
        public TimeSpan Remaining
        {
            get
            {
                if (IsComplete)
                    return TimeSpan.Zero;
                return IsStarted ? End - DateTime.UtcNow : Duration;
            }
        }

        /// <summary>
        /// Time passed since <see cref="Start"/>. Zero when not started yet,
        /// full <see cref="Duration"/> once complete.
        /// </summary>
        public TimeSpan Elapsed
        {
            get
            {
                if (IsComplete)
                    return Duration;
                return IsStarted ? DateTime.UtcNow - Start : TimeSpan.Zero;
            }
        }

        public DateTimeTimer(DateTime end) : this(DateTime.UtcNow, end)
        {
        }

        public DateTimeTimer(TimeSpan duration) : this(DateTime.UtcNow, DateTime.UtcNow.Add(duration))
        {
        }

        public DateTimeTimer(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
            Duration = end - start;
        }

        public override string ToString() => $"DateTimeTimer from {Start} to {End} (duration: {Duration})";
    }
}
