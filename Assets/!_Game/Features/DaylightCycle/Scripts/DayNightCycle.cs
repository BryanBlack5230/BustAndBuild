using System;
using System.Collections;
using UnityEngine;

namespace GameManagement
{
	public class DayNightCycle : MonoBehaviour, IGameStartListener, IGamePauseListener, IGameResumeListener, IGameFinishListener, IGameUpdateListener
	{
		[SerializeField] private float _dayLength;
		[SerializeField] private float _dayPercent;
		[SerializeField] private DaylightHandler _daylight;

		public static event Action OnDayStarted;
		public static event Action OnDayEnded;

		private int _minutesInDay = 1440; // 24 hours
		private float _minuteLenght => _dayLength / _minutesInDay;
		private TimeSpan _currentTime;
		[SerializeField] private bool _dayInProgress;
		private int _elapsedMinutes;


		private void Awake() 
		{ 
			// GameStateManager.OnBrazierDestroyed += OnBrazierDestroyed;
			// GameStateManager.GameRestarted += Reset;
		}

		private void OnDestroy() 
		{
			// GameStateManager.OnBrazierDestroyed -= OnBrazierDestroyed;
			// GameStateManager.GameRestarted -= Reset;
		}

		private void Reset()
		{
			_dayInProgress = false;
			StopAllCoroutines();
			_currentTime += TimeSpan.FromMinutes(_minutesInDay - _currentTime.TotalMinutes % _minutesInDay);
			_daylight.SetDaylightTo(0);
		}

		public void StartDay()
		{
			if (!_dayInProgress) StartCoroutine(StartClock());
		}

		private IEnumerator StartClock()
		{
			_dayInProgress = true;
			OnDayStarted?.Invoke();
			for (int i = 0; i < _minutesInDay; i++)
			{
				yield return AddMinute();
				_dayPercent = PercentOfDay();
				_daylight.SetDaylightTo(_dayPercent);
			}
			OnDayEnded?.Invoke();
			_dayInProgress = false;
		}

		private IEnumerator AddMinute()
		{
			_currentTime += TimeSpan.FromMinutes(1);
			yield return new WaitForSeconds(_minuteLenght);
		}

		private float PercentOfDay()
		{
			return (float) _currentTime.TotalMinutes % _minutesInDay / _minutesInDay;
		}

		private void OnBrazierDestroyed()
		{
			var beforeEvening = 0.95f;
			if (_dayPercent < beforeEvening || !Mathf.Approximately(_dayPercent, 0))
			{
				_currentTime += TimeSpan.FromMinutes(beforeEvening * _minutesInDay - _currentTime.TotalMinutes % _minutesInDay);
				StopAllCoroutines();
				StartCoroutine(SunGoesDown());
			}
		}

	    private IEnumerator SunGoesDown()
	    {
	        var minutesLeft = Mathf.CeilToInt((float)(_minutesInDay - _currentTime.TotalMinutes % _minutesInDay));
			for (var i = 0; i < minutesLeft; i++)
			{
				yield return AddMinute();
				_dayPercent = PercentOfDay();
				_daylight.SetDaylightTo(_dayPercent);
			}
			OnDayEnded?.Invoke();
			_dayInProgress = false;
	    }

	    public void OnStartGame()
	    {
		    _dayInProgress = true;
		    OnDayStarted?.Invoke();
	    }

	    public void OnPause()
	    {
		    _dayInProgress = false;
	    }

	    public void OnResume()
	    {
		    _dayInProgress = true;
	    }

	    public void OnFinishGame()
	    {
		    _dayInProgress = false;
	    }

	    public void OnUpdate(float deltaTime)
	    {
		    _elapsedMinutes = (_elapsedMinutes + 1 ) % _minutesInDay;
		    if (_elapsedMinutes >= _minutesInDay)
		    {
			    OnDayEnded?.Invoke();
			    _currentTime += TimeSpan.FromMinutes(_minutesInDay - _currentTime.TotalMinutes % _minutesInDay);
			    _daylight.SetDaylightTo(0);
			    OnDayStarted?.Invoke();
		    }
		    else
		    {
			    _currentTime += TimeSpan.FromMinutes(1);
			    _dayPercent = PercentOfDay();
			    _daylight.SetDaylightTo(_dayPercent);
		    }
		    
	    }
	}
}

