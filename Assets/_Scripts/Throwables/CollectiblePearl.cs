using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CollectiblePearl : MonoBehaviour
{
	public CollectiblePearlManager Manager;
	private Transform _gfx;
	private SpriteRenderer _shadow;
	private Tween _bobTween;
	private OrthographObject _orthographObj;
	private Coroutine _autoDestroy;
	private Collider2D _collider;


	private void Awake()
	{
		_gfx = transform.GetChild(0);
		_orthographObj = GetComponent<OrthographObject>();
		_orthographObj.Init(transform);
		_collider = GetComponent<Collider2D>();

		_shadow = transform.GetChild(1).GetComponent<SpriteRenderer>();
	}

	public void Throw(Vector2 inertia, float landPosY)
	{
		Vector2 randomizedDirection = Random.insideUnitCircle.normalized;
		Vector2 force = (randomizedDirection * inertia) * Random.Range(0.001f, Manager.spawnRange);
		_orthographObj.Throw(force, landPosY);
	}

	private void Landed()
	{
		transform.DOScale(_orthographObj.ChangeSize(), 0.5f);
		_shadow.DOFade(1f, 0.5f);
		BobUp();
		_autoDestroy = StartCoroutine(AutoDestroy());
	}

	IEnumerator AutoDestroy()
	{
		float timePassed = 0f;

		while (timePassed < 5f)
		{
			timePassed += Time.deltaTime;
			yield return null;
		}
		transform.DOScale(0f, 0.5f).SetEase(Ease.InOutFlash).OnComplete(()=>{
			_bobTween?.Kill();
			ReturnToPool();
		});
	}

	private void BobUp()
	{
		_bobTween = _gfx.DOMoveY(_gfx.position.y + 0.5f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
	}

	public void Collect()
	{
		_bobTween?.Kill();
		_orthographObj.StopMovement();
		if (_autoDestroy != null)
			StopCoroutine(_autoDestroy);
		_gfx.DOLocalMoveY(0, 1f);
		_shadow.DOFade(0f, 0.1f);
		_collider.enabled = false;
		transform.DOMove(Manager.uiTarget.position, 1f).SetEase(Ease.InQuad).OnComplete(() => {
			Manager.CollectedCall();
			ReturnToPool();
		});
	}

	public void Reset()
	{
		_autoDestroy = null;
		_bobTween?.Kill();
		_orthographObj.StopMovement();
		_collider.enabled = true;
		_gfx.localPosition = Vector2.zero;
		Color color = _shadow.color;
		color.a = 1f;
		_shadow.color = color;
		transform.localScale = _orthographObj.GetOriginalSize();
	}

	public void ReturnToPool()
	{
		Manager.Release(this);
	}

	private void OnEnable() 
	{
		_orthographObj.OnLanded += Landed;
	}

	private void OnDisable() 
	{
		_orthographObj.OnLanded -= Landed;
	}
}
