using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    /// <summary>
    /// Extension methods and helper utilities for Timer.
    /// </summary>
    public static class TimerExtensions
    {
        #region TimeSpan Formatting

        public static string ToMMSS(this TimeSpan time) => time.ToString("mm\\:ss");

        public static string ToHHMMSS(this TimeSpan time) => time.ToString("hh\\:mm\\:ss");

        /// <summary>Formats as compact string (e.g., "2h 30m", "45s", "5m 12s").</summary>
        public static string ToCompactFormat(this TimeSpan time)
        {
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            if (time.TotalMinutes >= 1) return $"{time.Minutes}m {time.Seconds}s";
            return $"{time.Seconds}s";
        }

        /// <summary>Formats with days if needed (e.g., "2d 05:30:00" or "05:30:00").</summary>
        public static string ToDurationFormat(this TimeSpan time)
        {
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays}d {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        #endregion

        #region Timer Factory Methods

        /// <summary>
        /// Creates a real-time countdown timer that fires when the target UTC time is reached.
        /// Useful for daily resets, scheduled events, server-synced deadlines.
        /// </summary>
        public static Timer CreateScheduledTimer(this DateTime targetTimeUtc)
        {
            var remaining = targetTimeUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            var timer = new Timer();
            timer.StartCountdown(remaining, useRealTime: true);
            return timer;
        }

        /// <summary>Creates a real-time cooldown timer from total cooldown duration and elapsed time.</summary>
        public static Timer CreateCooldownTimer(this TimeSpan totalCooldown, TimeSpan elapsed)
        {
            var remaining = totalCooldown - elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            var timer = new Timer();
            timer.StartCountdown(remaining, useRealTime: true);
            return timer;
        }

        #endregion

        #region UI Binding Helpers

        /// <summary>Binds timer to a TextMeshPro text. Returns an IDisposable that unbinds on dispose.</summary>
        public static IDisposable BindToText(this Timer timer, TextMeshProUGUI textComponent, string format = "mm\\:ss")
        {
            void UpdateText(TimeSpan time, float progress) => textComponent.text = time.ToString(format);

            timer.OnTick += UpdateText;
            UpdateText(timer.RemainingTime, timer.Progress);

            return new TimerUnbinder(() => timer.OnTick -= UpdateText);
        }

        /// <summary>Binds timer to a UI Text.</summary>
        public static IDisposable BindToText(this Timer timer, Text textComponent, string format = "mm\\:ss")
        {
            void UpdateText(TimeSpan time, float progress) => textComponent.text = time.ToString(format);

            timer.OnTick += UpdateText;
            UpdateText(timer.RemainingTime, timer.Progress);

            return new TimerUnbinder(() => timer.OnTick -= UpdateText);
        }

        /// <summary>Binds timer progress (0→1) to an Image fill amount.</summary>
        public static IDisposable BindToFill(this Timer timer, Image image)
        {
            void UpdateFill(TimeSpan time, float progress) => image.fillAmount = progress;

            timer.OnTick += UpdateFill;
            UpdateFill(timer.RemainingTime, timer.Progress);

            return new TimerUnbinder(() => timer.OnTick -= UpdateFill);
        }

        /// <summary>Binds timer progress (1→0) to an Image fill amount. Useful for cooldown overlays that empty as time passes.</summary>
        public static IDisposable BindToFillInverse(this Timer timer, Image image)
        {
            void UpdateFill(TimeSpan time, float progress) => image.fillAmount = 1f - progress;

            timer.OnTick += UpdateFill;
            UpdateFill(timer.RemainingTime, timer.Progress);

            return new TimerUnbinder(() => timer.OnTick -= UpdateFill);
        }

        /// <summary>Binds timer progress (0→1) to a Slider value.</summary>
        public static IDisposable BindToSlider(this Timer timer, Slider slider)
        {
            void UpdateSlider(TimeSpan time, float progress) => slider.value = progress;

            timer.OnTick += UpdateSlider;
            UpdateSlider(timer.RemainingTime, timer.Progress);

            return new TimerUnbinder(() => timer.OnTick -= UpdateSlider);
        }

        #endregion

        #region Utility Classes

        private class TimerUnbinder : IDisposable
        {
            private Action _unbindAction;
            private bool _isDisposed;

            public TimerUnbinder(Action unbindAction) => _unbindAction = unbindAction;

            public void Dispose()
            {
                if (_isDisposed) return;
                _unbindAction?.Invoke();
                _unbindAction = null;
                _isDisposed = true;
            }
        }

        #endregion
    }
}
