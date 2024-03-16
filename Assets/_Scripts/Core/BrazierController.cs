using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Resources;
using UnityEngine.Rendering.Universal;
using BB.Combat;
using DG.Tweening;

public class BrazierController : MonoBehaviour
{
	[SerializeField] Color activeColor;
	[SerializeField] Color inactiveColor;
	[SerializeField] Sphere sphere;
	[SerializeField] Color activeLightColor;
	[SerializeField] Color inactiveLightColor;
	[SerializeField] GameObject brazierLight;
	[SerializeField] GameObject brazierBase;
	[SerializeField] GameObject brazierDestroyed;
	[SerializeField] Transform brazierSocket;
	private Health _health;
	private bool _isDead;
	private bool _isActive;
	private Light2D _sphereLight;
	private Light2D _sphereInnerLight;
	private Vector2 _sphereInSocketPos;
	private Collider2D _brazierCollider;
	private bool _gameStarted;
	private Tween _sphereFlicker;
	private void Awake() 
	{
		
		_sphereInSocketPos = brazierSocket.position;
		// sphere.transform.position = _sphereInSocketPos + Vector2.left * 5;
		_brazierCollider = GetComponent<Collider2D>();
	}
	private void Start() 
	{
		_health = GetComponent<Health>();
		_sphereLight = sphere.transform.GetChild(0).GetComponent<Light2D>();
		_sphereInnerLight = _sphereLight.transform.GetChild(0).GetComponent<Light2D>();
		Reset();
		_gameStarted = true;
		_sphereFlicker = SpereInnerLightFlicker();
	}
	private void Update() 
	{
		if (_isActive && (Vector2)sphere.transform.position != _sphereInSocketPos)
			sphere.transform.SetPositionAndRotation(_sphereInSocketPos, Quaternion.identity);

		if (_isDead == _health.IsDead()) return;

		_isDead = _health.IsDead();
		if (_isDead)
		{
			brazierBase.SetActive(false);
			brazierLight.SetActive(false);
			brazierDestroyed.SetActive(true);
			SphereInactive();
			GameStateManager.Instance.BrazierDestroyed();
		}
	}

	private void OnEnable() 
	{
		GameStateManager.GameRestarted += Reset;
		TimeController.DayEnded += DayEnded;
		TimeController.DayStarted += DayStarted;
	}

	private void OnDisable() 
	{
		GameStateManager.GameRestarted -= Reset;
		TimeController.DayEnded -= DayEnded;
		TimeController.DayStarted -= DayStarted;
	}

	private void DayStarted()
	{
		brazierLight.SetActive(true);
		_brazierCollider.enabled = false;
	}

	private void DayEnded()
	{
		brazierLight.SetActive(false);
		SphereInactive();
		_isActive = false;
		sphere.Throw(10* Vector2.left);
		DelayColliderActivation();
	}

	private void DelayColliderActivation()
	{
		StartCoroutine(ColliderActivation());
	}

	private IEnumerator ColliderActivation()
	{
		yield return new WaitForSeconds(2);
		_brazierCollider.enabled = true;
	}

	private Tween SpereInnerLightFlicker()
	{
		return DOTween.To(() => _sphereInnerLight.color.a, alpha => {
			Color color = _sphereInnerLight.color;
			color.a = alpha;
			_sphereInnerLight.color = color;
		}, 1f, 0.3f)
		.SetEase(Ease.InOutQuad)
		.SetLoops(-1, LoopType.Yoyo);
		// float flickerAlpha = Random.Range(0.81f, 1f);
		// Color color = _sphereInnerLight.color;
		// color.a = flickerAlpha;
		// _sphereInnerLight.color = color;
	}

	private void Reset()
	{
		_health.Revive();
		_isActive = false;
		_isDead = false;
		if (_gameStarted) // && !sphere.GetComponent<Renderer>().isVisible
			sphere.transform.position = (Vector2)brazierSocket.position + Vector2.left * 5f;

		brazierBase.SetActive(true);
		brazierLight.SetActive(true);
		brazierDestroyed.SetActive(false);
		_brazierCollider.enabled = true;
		SphereInactive();
	}

	private void SphereActive()
	{
		sphere.GetComponent<SpriteRenderer>().color = activeColor;
		_sphereLight.color = activeLightColor;
		sphere.GetComponent<Grabbable>().enabled = false;
		sphere.GetComponent<Collider2D>().enabled = false;
		sphere.transform.SetPositionAndRotation(_sphereInSocketPos, Quaternion.identity);

		_sphereFlicker?.Play();
	}

	private void SphereInactive()
	{
		sphere.GetComponent<SpriteRenderer>().color = inactiveColor;
		_sphereLight.color = inactiveLightColor;
		sphere.GetComponent<Grabbable>().enabled = true;
		sphere.GetComponent<Collider2D>().enabled = true;

		_sphereFlicker?.Pause();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.name == "Sphere")
		{
			sphere.GetComponent<Grabbable>().ReleaseObject();
			sphere.CallLanded();
			
			SphereActive();
			_isActive = true;
			TimeController.Instance.StartDay();
		}
	}
}
