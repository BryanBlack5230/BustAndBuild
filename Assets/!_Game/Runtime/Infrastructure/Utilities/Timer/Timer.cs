using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
	/// <summary>
	/// A self-contained timer utility using UniTask.
	/// Each instance is owned by its creator — no centralized manager required.
	/// Supports countdown, count-up, intervals, pause/resume, and rich callbacks.
	/// </summary>
	public class Timer : IDisposable
	{
		#region Events

		/// <summary>Fired every tick with elapsed/remaining time and normalized progress (0–1).</summary>
		public event Action<TimeSpan, float> OnTick;

		/// <summary>Fired when the timer completes naturally.</summary>
		public event Action OnComplete;

		/// <summary>Fired when the timer is paused.</summary>
		public event Action OnPause;

		/// <summary>Fired when the timer is resumed from pause.</summary>
		public event Action OnResume;

		/// <summary>Fired when the timer is cancelled via Stop() or Dispose() while active.</summary>
		public event Action OnCancel;

		/// <summary>Fired on each interval tick with the 1-based interval count.</summary>
		public event Action<int> OnInterval;

		#endregion

		#region Properties

		public TimerState State { get; private set; } = TimerState.Idle;
		public TimerMode Mode { get; private set; } = TimerMode.None;
		public TimeSpan Duration { get; private set; }
		public TimeSpan RemainingTime { get; private set; }
		public TimeSpan ElapsedTime { get; private set; }

		public float Progress => Mode switch
		{
			TimerMode.Countdown when Duration > TimeSpan.Zero
				=> Mathf.Clamp01(1f - (float)(RemainingTime.TotalSeconds / Duration.TotalSeconds)),
			TimerMode.CountUp when Duration > TimeSpan.Zero
				=> Mathf.Clamp01((float)(ElapsedTime.TotalSeconds / Duration.TotalSeconds)),
			_ => 0f,
		};

		/// <summary>When true, timer uses real wall-clock time and ignores Time.timeScale.</summary>
		public bool UseRealTime { get; private set; }

		/// <summary>Tick interval for OnTick callbacks. Default 100ms.</summary>
		public TimeSpan TickInterval { get; private set; } = TimeSpan.FromMilliseconds(100);

		#endregion

		#region Private Fields

		private readonly CancellationToken _externalToken;
		private CancellationTokenSource _cts;
		private UniTask _timerTask;
		private bool _isDisposed;

		// Real-time baseline — recomputed on Start and Resume so pause/resume math stays correct.
		private DateTime _runStartUtc;
		private TimeSpan _runStartRemaining;
		private TimeSpan _runStartElapsed;

		// Interval state — preserved across pause/resume.
		private int _intervalRepeatCount = -1;
		private int _intervalCurrentCount;

		#endregion

		#region Public Methods

		public Timer(CancellationToken cancellationToken = default)
		{
			_externalToken = cancellationToken;
		}

		/// <summary>Starts a countdown timer from the specified duration.</summary>
		public void StartCountdown(
			TimeSpan duration,
			Action<TimeSpan, float> onTick = null,
			Action onComplete = null,
			TimeSpan? tickInterval = null,
			bool useRealTime = false)
		{
			ThrowIfDisposed();
			Stop();

			Mode = TimerMode.Countdown;
			Duration = duration;
			RemainingTime = duration;
			ElapsedTime = TimeSpan.Zero;
			UseRealTime = useRealTime;
			TickInterval = tickInterval ?? TimeSpan.FromMilliseconds(100);

			if (onTick != null) OnTick += onTick;
			if (onComplete != null) OnComplete += onComplete;

			_runStartUtc = DateTime.UtcNow;
			_runStartRemaining = duration;

			_cts = CreateLinkedCts();
			_timerTask = RunCountdownAsync(_cts.Token);
		}

		/// <summary>Starts a count-up timer (stopwatch mode). Pass null duration for an open-ended stopwatch.</summary>
		public void StartCountUp(
			TimeSpan? duration = null,
			Action<TimeSpan, float> onTick = null,
			Action onComplete = null,
			TimeSpan? tickInterval = null,
			bool useRealTime = false)
		{
			ThrowIfDisposed();
			Stop();

			Mode = TimerMode.CountUp;
			Duration = duration ?? TimeSpan.Zero;
			ElapsedTime = TimeSpan.Zero;
			RemainingTime = TimeSpan.Zero;
			UseRealTime = useRealTime;
			TickInterval = tickInterval ?? TimeSpan.FromMilliseconds(100);

			if (onTick != null) OnTick += onTick;
			if (onComplete != null) OnComplete += onComplete;

			_runStartUtc = DateTime.UtcNow;
			_runStartElapsed = TimeSpan.Zero;

			_cts = CreateLinkedCts();
			_timerTask = RunCountUpAsync(_cts.Token);
		}

		/// <summary>Starts a repeating interval timer. Pass repeatCount &lt; 0 for infinite repeats.</summary>
		public void StartInterval(
			TimeSpan interval,
			int repeatCount = -1,
			Action<int> onInterval = null,
			Action onComplete = null,
			bool useRealTime = false)
		{
			ThrowIfDisposed();

			if (interval <= TimeSpan.Zero)
				throw new ArgumentException("Interval must be greater than zero", nameof(interval));

			Stop();

			Mode = TimerMode.Interval;
			Duration = interval;
			TickInterval = interval;
			UseRealTime = useRealTime;
			_intervalRepeatCount = repeatCount;
			_intervalCurrentCount = 0;
			RemainingTime = TimeSpan.Zero;
			ElapsedTime = TimeSpan.Zero;

			if (onInterval != null) OnInterval += onInterval;
			if (onComplete != null) OnComplete += onComplete;

			_cts = CreateLinkedCts();
			_timerTask = RunIntervalAsync(_cts.Token);
		}

		/// <summary>Pauses the timer. Resume() continues from the same mode and state.</summary>
		public void Pause()
		{
			if (_isDisposed || State != TimerState.Running) return;

			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			State = TimerState.Paused;
			OnPause?.Invoke();
		}

		/// <summary>Resumes a paused timer in its original mode.</summary>
		public void Resume()
		{
			if (_isDisposed || State != TimerState.Paused) return;

			_cts = CreateLinkedCts();

			switch (Mode)
			{
				case TimerMode.Countdown:
					_runStartUtc = DateTime.UtcNow;
					_runStartRemaining = RemainingTime;
					_timerTask = RunCountdownAsync(_cts.Token);
					break;
				case TimerMode.CountUp:
					_runStartUtc = DateTime.UtcNow;
					_runStartElapsed = ElapsedTime;
					_timerTask = RunCountUpAsync(_cts.Token);
					break;
				case TimerMode.Interval:
					_timerTask = RunIntervalAsync(_cts.Token);
					break;
				default:
					_cts.Dispose();
					_cts = null;
					return;
			}

			OnResume?.Invoke();
		}

		/// <summary>Stops the timer without completing it. Fires OnCancel if the timer was active.</summary>
		public void Stop()
		{
			if (_isDisposed) return;

			bool wasActive = State == TimerState.Running || State == TimerState.Paused;

			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			State = TimerState.Idle;
			Mode = TimerMode.None;
			RemainingTime = TimeSpan.Zero;
			ElapsedTime = TimeSpan.Zero;
			_intervalCurrentCount = 0;

			if (wasActive) OnCancel?.Invoke();
		}

		/// <summary>Stops and cleans up the timer. Instance cannot be reused after disposal.</summary>
		public void Dispose()
		{
			if (_isDisposed) return;

			bool wasActive = State == TimerState.Running || State == TimerState.Paused;

			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			if (wasActive) OnCancel?.Invoke();

			OnTick = null;
			OnComplete = null;
			OnPause = null;
			OnResume = null;
			OnCancel = null;
			OnInterval = null;

			State = TimerState.Idle;
			Mode = TimerMode.None;
			_isDisposed = true;
		}

		/// <summary>Formats RemainingTime. Default format handles durations over 24h.</summary>
		public string GetFormattedRemaining(string format = "mm\\:ss") => RemainingTime.ToString(format);

		/// <summary>Formats ElapsedTime. Default format handles durations over 24h.</summary>
		public string GetFormattedElapsed(string format = "mm\\:ss") => ElapsedTime.ToString(format);

		#endregion

		#region Private Methods

		private CancellationTokenSource CreateLinkedCts()
		{
			return _externalToken.CanBeCanceled
				? CancellationTokenSource.CreateLinkedTokenSource(_externalToken)
				: new CancellationTokenSource();
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed) throw new ObjectDisposedException(nameof(Timer));
		}

		private async UniTask RunCountdownAsync(CancellationToken token)
		{
			State = TimerState.Running;

			try
			{
				if (Duration <= TimeSpan.Zero)
				{
					RemainingTime = TimeSpan.Zero;
					ElapsedTime = TimeSpan.Zero;
					State = TimerState.Completed;
					OnComplete?.Invoke();
					return;
				}

				while (RemainingTime > TimeSpan.Zero)
				{
					await UniTask.Delay(TickInterval, ignoreTimeScale: UseRealTime, cancellationToken: token);
					if (token.IsCancellationRequested) return;

					if (UseRealTime)
						RemainingTime = _runStartRemaining - (DateTime.UtcNow - _runStartUtc);
					else
						RemainingTime -= TickInterval;

					if (RemainingTime < TimeSpan.Zero) RemainingTime = TimeSpan.Zero;
					ElapsedTime = Duration - RemainingTime;

					OnTick?.Invoke(RemainingTime, Progress);
				}

				if (State == TimerState.Running)
				{
					State = TimerState.Completed;
					OnComplete?.Invoke();
				}
			}
			catch (OperationCanceledException)
			{
				// Expected on cancellation
			}
		}

		private async UniTask RunCountUpAsync(CancellationToken token)
		{
			State = TimerState.Running;
			bool hasTarget = Duration > TimeSpan.Zero;

			try
			{
				while (!hasTarget || ElapsedTime < Duration)
				{
					await UniTask.Delay(TickInterval, ignoreTimeScale: UseRealTime, cancellationToken: token);
					if (token.IsCancellationRequested) return;

					if (UseRealTime)
						ElapsedTime = _runStartElapsed + (DateTime.UtcNow - _runStartUtc);
					else
						ElapsedTime += TickInterval;

					if (hasTarget && ElapsedTime > Duration) ElapsedTime = Duration;
					RemainingTime = hasTarget ? Duration - ElapsedTime : TimeSpan.Zero;

					OnTick?.Invoke(ElapsedTime, Progress);
				}

				if (hasTarget && State == TimerState.Running)
				{
					State = TimerState.Completed;
					OnComplete?.Invoke();
				}
			}
			catch (OperationCanceledException)
			{
				// Expected on cancellation
			}
		}

		private async UniTask RunIntervalAsync(CancellationToken token)
		{
			State = TimerState.Running;

			try
			{
				while (_intervalRepeatCount < 0 || _intervalCurrentCount < _intervalRepeatCount)
				{
					await UniTask.Delay(TickInterval, ignoreTimeScale: UseRealTime, cancellationToken: token);
					if (token.IsCancellationRequested) return;

					_intervalCurrentCount++;
					ElapsedTime = TimeSpan.FromTicks(TickInterval.Ticks * _intervalCurrentCount);
					float progress = _intervalRepeatCount > 0
						? Mathf.Clamp01((float)_intervalCurrentCount / _intervalRepeatCount)
						: 0f;

					OnInterval?.Invoke(_intervalCurrentCount);
					OnTick?.Invoke(ElapsedTime, progress);
				}

				if (State == TimerState.Running)
				{
					State = TimerState.Completed;
					OnComplete?.Invoke();
				}
			}
			catch (OperationCanceledException)
			{
				// Expected on cancellation
			}
		}

		#endregion
	}

	public enum TimerState
	{
		Idle,
		Running,
		Paused,
		Completed,
	}

	public enum TimerMode
	{
		None,
		Countdown,
		CountUp,
		Interval,
	}
}
