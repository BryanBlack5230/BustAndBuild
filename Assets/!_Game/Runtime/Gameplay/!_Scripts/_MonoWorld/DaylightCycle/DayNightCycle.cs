#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    public sealed class DayNightCycle : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameFinishListener, IDisposable
    {
        private enum Phase { Idle, Day, Sundown }

        private readonly DayNightSetting _setting;
        private readonly DaylightHandler _daylight;

        private Phase _phase = Phase.Idle;
        private float _elapsedSeconds;
        private int _elapsedMinutes;
        private TimeSpan _currentTime;
        private float _dayPercent;

        private float _sundownStartPercent;
        private float _sundownElapsed;

        private CancellationTokenSource? _cts;

        private readonly IDisposable _startDaySub;
        private readonly IDisposable _forceFinishSub;

        public DayNightCycle(DayNightSetting setting, DaylightHandler daylight, CommandDispatcher dispatcher)
        {
            _setting = setting;
            _daylight = daylight;

            _startDaySub = dispatcher.Register<StartDayCommand>(OnStartDayCommand);
            _forceFinishSub = dispatcher.Register<ForceFinishDayCommand>(OnForceFinishCommand);
        }

        public void Dispose()
        {
            _startDaySub.Dispose();
            _forceFinishSub.Dispose();
            CancelLoop();
        }

        private void OnStartDayCommand(StartDayCommand _) => StartDay();
        private void OnForceFinishCommand(ForceFinishDayCommand _) => ForceFinishDay();

        public void OnStartGame() => StartDay();
        public void OnFinishGame() { CancelLoop(); ResetState(); }
        public void OnPause() => CancelLoop();
        public void OnResume()
        {
            switch (_phase)
            {
                case Phase.Day:     LaunchDayLoop();     break;
                case Phase.Sundown: LaunchSundownLoop(); break;
            }
        }

        public void StartDay()
        {
            if (_phase != Phase.Idle) return;

            ResetState();
            _phase = Phase.Day;
            EventBus.Raise(new DayStartedEvent());
            LaunchDayLoop();
        }

        public void ForceFinishDay()
        {
            if (_phase != Phase.Day) return;

            CancelLoop();
            _phase = Phase.Sundown;
            _sundownStartPercent = _dayPercent;
            _sundownElapsed = 0f;
            LaunchSundownLoop();
        }

        private void ResetState()
        {
            _phase = Phase.Idle;
            _elapsedSeconds = 0f;
            _elapsedMinutes = 0;
            _currentTime = TimeSpan.Zero;
            _dayPercent = 0f;
            _daylight.SetDaylightTo(0f);
        }

        private void LaunchDayLoop()
        {
            CancelLoop();
            _cts = new CancellationTokenSource();
            RunDayAsync(_cts.Token).Forget();
        }

        private void LaunchSundownLoop()
        {
            CancelLoop();
            _cts = new CancellationTokenSource();
            RunSundownAsync(_cts.Token).Forget();
        }

        private void CancelLoop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask RunDayAsync(CancellationToken ct)
        {
            try
            {
                var minuteLength = _setting.DayLength / RuntimeConstants.Daylight.MinutesInDay;

                while (_elapsedSeconds < _setting.DayLength)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    _elapsedSeconds += Time.deltaTime;

                    var newMinute = (int)(_elapsedSeconds / minuteLength);
                    if (newMinute > _elapsedMinutes)
                    {
                        _currentTime += TimeSpan.FromMinutes(newMinute - _elapsedMinutes);
                        _elapsedMinutes = newMinute;
                    }

                    _dayPercent = Mathf.Clamp01(_elapsedSeconds / _setting.DayLength);
                    _daylight.SetDaylightTo(_dayPercent);
                }

                CompleteDay();
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask RunSundownAsync(CancellationToken ct)
        {
            try
            {
                while (_sundownElapsed < _setting.SunDownDuration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    _sundownElapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(_sundownElapsed / _setting.SunDownDuration);
                    _dayPercent = Mathf.Lerp(_sundownStartPercent, 1f, t);
                    _daylight.SetDaylightTo(_dayPercent);
                }

                _dayPercent = 1f;
                _daylight.SetDaylightTo(1f);
                CompleteDay();
            }
            catch (OperationCanceledException) { }
        }

        private void CompleteDay()
        {
            _phase = Phase.Idle;
            EventBus.Raise(new DayEndedEvent());

            if (_setting.IsLooping)
            {
                StartDay();
            }
        }
    }
}
