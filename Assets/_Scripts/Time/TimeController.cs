using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TimeController : MonoBehaviour
{
	[SerializeField] float dayLength;
	[SerializeField] Gradient sunLight;
	[SerializeField] Light2D globalLight;
	[SerializeField] float dayPercent;
	[SerializeField] ParticleSystem stars;

	public static TimeController Instance;
	public static event Action DayStarted;
	public static event Action DayEnded;

	private float _minutesInDay = 1440; // 24 hours
	private float _minuteLenght => dayLength / _minutesInDay;
	private TimeSpan _currentTime;
	private bool _dayInProgress;

	private Renderer _starRenderer;

	
	private void Awake() 
	{ 
		if (Instance != null && Instance != this) 
			Destroy(this);
		else 
			Instance = this;

		_starRenderer = stars.GetComponent<Renderer>();

		GameStateManager.OnBrazierDestroyed += OnBrazierDestroyed;
	}

	private void OnDestroy() 
	{
		GameStateManager.OnBrazierDestroyed -= OnBrazierDestroyed;
	}

	public void StartDay()
	{
		if (!_dayInProgress) StartCoroutine(StartClock());
	}

	private IEnumerator StartClock()
	{
		_dayInProgress = true;
		DayStarted?.Invoke();
		stars.Stop();
		for (int i = 0; i < _minutesInDay; i++)
		{
			yield return AddMinute();
			dayPercent = PercentOfDay();
			globalLight.color = sunLight.Evaluate(dayPercent);
		}
		stars.Play();
		DayEnded?.Invoke();
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
		float beforeEvening = 0.89f;
		if (dayPercent < beforeEvening || !Mathf.Approximately(dayPercent, 0))
		{
			_currentTime += TimeSpan.FromMinutes(beforeEvening * _minutesInDay - _currentTime.TotalMinutes % _minutesInDay);
			StopAllCoroutines();
			StartCoroutine(SunGoesDown());
		}
	}

    private IEnumerator SunGoesDown()
    {
        int minutesLeft = Mathf.CeilToInt((float)(_minutesInDay - _currentTime.TotalMinutes % _minutesInDay));
		for (int i = 0; i < minutesLeft; i++)
		{
			yield return AddMinute();
			dayPercent = PercentOfDay();
			globalLight.color = sunLight.Evaluate(dayPercent);
		}
		stars.Play();
		DayEnded?.Invoke();
		_dayInProgress = false;
    }
}
