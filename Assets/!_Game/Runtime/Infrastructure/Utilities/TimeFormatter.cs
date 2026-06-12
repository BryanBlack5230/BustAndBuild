#nullable enable
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public enum TimeFormat
    {
        Digital,     // 01:30:45
        Abbreviated, // 1h 30m
        Full,        // 1 Hour 30 Minutes
        Compact,     // 1h30m (no spaces)
        Stopwatch    // 01:30.45 (milliseconds)
    }

    public enum TimeRounding
    {
        Floor,  // 59s -> 0m (good for "time played")
        Ceil,   // 59s -> 1m (use for cooldowns — never show "0m" while one is still active)
        Nearest // 59s -> 1m, 29s -> 0m
    }

    /// <summary>
    /// Duration/relative-time formatting and parsing.
    /// Adapted from UGFW (MIT, © 2026 MAK).
    /// </summary>
    public static class TimeFormatter
    {
        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = 3600;
        private const int SecondsPerDay = 86400;

        private static readonly Regex DurationTokenPattern =
            new(@"(\d*\.?\d+)\s*([dhms])", RegexOptions.Compiled);

        // --- Duration ---

        public static string FormatDuration(this float seconds, TimeFormat format = TimeFormat.Digital, int maxUnits = 3, TimeRounding rounding = TimeRounding.Floor)
            => FormatSpan(TimeSpan.FromSeconds(seconds), format, maxUnits, rounding);

        public static string FormatDuration(this int seconds, TimeFormat format = TimeFormat.Digital, int maxUnits = 3, TimeRounding rounding = TimeRounding.Floor)
            => FormatSpan(TimeSpan.FromSeconds(seconds), format, maxUnits, rounding);

        public static string FormatDuration(this double seconds, TimeFormat format = TimeFormat.Digital, int maxUnits = 3, TimeRounding rounding = TimeRounding.Floor)
            => FormatSpan(TimeSpan.FromSeconds(seconds), format, maxUnits, rounding);

        public static string FormatDuration(this TimeSpan span, TimeFormat format = TimeFormat.Digital, int maxUnits = 3, TimeRounding rounding = TimeRounding.Floor)
            => FormatSpan(span, format, maxUnits, rounding);

        /// <summary>Formats a "current / total" string, e.g. "01:30 / 02:00".</summary>
        public static string FormatProgress(this float current, float total, TimeFormat format = TimeFormat.Digital)
            => $"{current.FormatDuration(format)} / {total.FormatDuration(format)}";

        // --- Arrival / future / relative ---

        /// <summary>Clock time after the given number of seconds from now, e.g. "5:03 PM".</summary>
        public static string GetArrivalTime(this float seconds, string dateTimeFormat = "t")
            => DateTime.Now.AddSeconds(seconds).ToString(dateTimeFormat);

        /// <summary>Smart descriptions like "Today at 5:03 PM" or "Tomorrow at 9:00 AM".</summary>
        public static string GetArrivalDescription(this float seconds)
        {
            var future = DateTime.Now.AddSeconds(seconds);
            var now = DateTime.Now;
            if (future.Date == now.Date) return $"Today at {future:t}";
            if (future.Date == now.AddDays(1).Date) return $"Tomorrow at {future:t}";
            return $"{future:MMM dd} at {future:t}";
        }

        /// <summary>
        /// "Just now", "5m ago", "3h ago", "2d ago". Compares in UTC so it pairs
        /// with <see cref="DateTimeTimer"/>; <see cref="DateTimeKind.Unspecified"/>
        /// timestamps are assumed to be local time.
        /// </summary>
        public static string ToRelativeTime(this DateTime past)
        {
            double seconds = (DateTime.UtcNow - past.ToUniversalTime()).TotalSeconds;
            if (seconds < SecondsPerMinute) return "Just now";
            if (seconds < SecondsPerHour) return $"{(int)(seconds / SecondsPerMinute)}m ago";
            if (seconds < SecondsPerDay) return $"{(int)(seconds / SecondsPerHour)}h ago";
            return $"{(int)(seconds / SecondsPerDay)}d ago";
        }

        /// <summary>Switches format based on urgency: ≥1m digital ("02:30"), under 1m decimal seconds ("42.7").</summary>
        public static string FormatDynamic(this float seconds)
            => seconds >= SecondsPerMinute
                ? FormatSpan(TimeSpan.FromSeconds(seconds), TimeFormat.Digital, 3, TimeRounding.Floor)
                : seconds.ToString("0.0");

        // --- Parsing ---

        /// <summary>Parses "1h 30m", "90m", "1.5d", or "01:30:00" to seconds. Throws on unparsable input.</summary>
        public static double ParseTime(string input)
        {
            if (!TryParseTime(input, out double seconds))
                throw new FormatException($"Cannot parse time string \"{input}\"");

            return seconds;
        }

        /// <inheritdoc cref="ParseTime"/>
        public static bool TryParseTime(string? input, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string text = input!.Trim().ToLowerInvariant();

            if (text.Contains(":"))
            {
                if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan ts))
                {
                    seconds = ts.TotalSeconds;
                    return true;
                }

                // TimeSpan.Parse reads "90:30" as hours:minutes; fall back to minutes:seconds
                var parts = text.Split(':');
                if (parts.Length == 2 && TryParseInvariant(parts[0], out double m) && TryParseInvariant(parts[1], out double s))
                {
                    seconds = m * SecondsPerMinute + s;
                    return true;
                }

                return false;
            }

            var matches = DurationTokenPattern.Matches(text);
            if (matches.Count > 0)
            {
                double total = 0;
                foreach (Match match in matches)
                {
                    if (!TryParseInvariant(match.Groups[1].Value, out double value))
                        return false;

                    switch (match.Groups[2].Value)
                    {
                        case "d": total += value * SecondsPerDay; break;
                        case "h": total += value * SecondsPerHour; break;
                        case "m": total += value * SecondsPerMinute; break;
                        case "s": total += value; break;
                    }
                }

                seconds = total;
                return true;
            }

            if (TryParseInvariant(text, out double raw))
            {
                seconds = raw;
                return true;
            }

            return false;
        }

        private static bool TryParseInvariant(string text, out double value)
            => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        // --- Core formatting ---

        private static string FormatSpan(TimeSpan span, TimeFormat format, int maxUnits, TimeRounding rounding)
        {
            // Stopwatch keeps sub-second precision — whole-second rounding would
            // always zero the millisecond component.
            if (format == TimeFormat.Stopwatch)
            {
                if (span <= TimeSpan.Zero) return GetZeroString(format);
                return $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds / 10:D2}";
            }

            // Round the TOTAL seconds first so unit cascading works (59.5s -> 60s -> "1m").
            double totalSeconds = span.TotalSeconds;

            totalSeconds = rounding switch
            {
                TimeRounding.Ceil => Math.Ceiling(totalSeconds),
                TimeRounding.Nearest => Math.Round(totalSeconds),
                _ => Math.Floor(totalSeconds),
            };

            if (totalSeconds <= 0) return GetZeroString(format);

            span = TimeSpan.FromSeconds(totalSeconds);

            if (format == TimeFormat.Digital)
                return FormatDigital(span);

            return FormatNatural(span, format, maxUnits);
        }

        private static string FormatDigital(TimeSpan t)
        {
            if (t.TotalHours < 1) return $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
            if (t.TotalDays < 1) return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            return $"{t.Days:D2}:{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        private static string FormatNatural(TimeSpan t, TimeFormat format, int maxUnits)
        {
            var sb = new StringBuilder();
            int count = 0;
            bool full = format == TimeFormat.Full;
            string space = format == TimeFormat.Compact ? "" : " ";

            void Add(int value, string shortSuffix, string longSuffix)
            {
                if (count >= maxUnits) return;
                if (value > 0)
                {
                    sb.Append($"{value}{(full ? (value == 1 ? longSuffix.TrimEnd('s') : longSuffix) : shortSuffix)}{space}");
                    count++;
                }
            }

            Add(t.Days, "d", " Days");
            Add(t.Hours, "h", " Hours");
            Add(t.Minutes, "m", " Minutes");

            // Show seconds if there is room, or if nothing was shown yet (e.g. plain "30s")
            if (count < maxUnits && (t.Seconds > 0 || count == 0))
                sb.Append($"{t.Seconds}{(full ? (t.Seconds == 1 ? " Second" : " Seconds") : "s")}");

            return sb.ToString().Trim();
        }

        private static string GetZeroString(TimeFormat format)
        {
            switch (format)
            {
                case TimeFormat.Digital: return "00:00";
                case TimeFormat.Stopwatch: return "00:00.00";
                case TimeFormat.Full: return "0 Seconds";
                default: return "0s";
            }
        }
    }
}
