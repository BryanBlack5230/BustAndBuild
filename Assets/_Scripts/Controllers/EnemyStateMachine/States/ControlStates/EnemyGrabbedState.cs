using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyGrabbedState : EnemyBaseState
{
	public EnemyGrabbedState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName){}

	public override void CheckSwitchStates()
	{
		if (_context.IsFlung)
			SwitchState(_factory.Flung());
	}

	public override void EnterState()
	{
		_context.IsOnGround = false;
		_context.RB.angularVelocity = 0f;
		_context.RB.SetRotation(Quaternion.identity);
		_context.GrabbedPosY = _context.transform.position.y;
		_context.Animator.SetTrigger(_context.ImmobilisedTriggerCached);
		_context.Shadow.DOFade(0f, 0.5f);
		ChangeSize(1f);
	}

	public override void ExitState()
	{
		// ChangeSize(CoreHelper.GetDepthModifier(_context.transform.position.y));
	}

	public override void InitializeSubState(){}

	public override void OnAttackEvent(){}

	public override void OnBodyCollision(Collision2D other){}
	public override void OnIncomingCollisionTrigger(Collider2D other){}

	public override void UpdateState()
	{
		Debug.Log($"{_debugInfo}; i'm grabbed, halp");
		CheckSwitchStates();
	}

	private void ChangeSize(float targetSize)
	{
		_context.transform.localScale = new Vector2(_context.OriginalSize.x * targetSize, 
													_context.OriginalSize.y * targetSize);
		// StartCoroutine(LerpChange(targetSize));//TODO

		// IEnumerator LerpChange(float targetSize)
		// {
		// 	float size = transform.localScale.x;

		// 	for (float t = 0.001f; t < 0.1f; t +=Time.deltaTime)
		// 	{
		// 		float newSize = Mathf.Lerp(size, targetSize, t / 0.1f);
		// 		transform.localScale = new Vector3(_originalSize.x * newSize, 
		// 										   _originalSize.y * newSize,
		// 											transform.localScale.z);
		// 		yield return null;
		// 	}

		// 	transform.localScale = new Vector3(_originalSize.x * targetSize, 
		// 										_originalSize.y * targetSize,
		// 										transform.localScale.z);
		// }
	}
}
