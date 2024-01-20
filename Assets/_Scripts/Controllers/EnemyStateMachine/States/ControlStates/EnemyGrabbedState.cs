using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGrabbedState : EnemyBaseState
{
	public EnemyGrabbedState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory){}

	public override void CheckSwitchStates()
	{
		if (_context.IsFlung)
			SwitchState(_factory.Flung());
	}

	public override void EnterState()
	{
		_context.Animator.SetTrigger(_context.ImmobilisedTriggerCached);
		ChangeSize(1f);
	}

	public override void ExitState()
	{
		ChangeSize(CoreHelper.GetDepthModifier(_context.transform.position.y));
	}

	public override void InitializeSubState(){}

	public override void OnAttackEvent(){}

	public override void OnCollisionEnter2D(Collision2D other){}

	public override void UpdateState()
	{
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
