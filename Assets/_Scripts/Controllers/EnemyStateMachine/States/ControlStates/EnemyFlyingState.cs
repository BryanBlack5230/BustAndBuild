using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Resources;

public class EnemyFlyingState : EnemyBaseState
{
	public EnemyFlyingState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName){}
	private float _throwPower;
	private bool _isFlying;
	public override void CheckSwitchStates()
	{
		if (_context.IsGrabbed)
			SwitchState(_factory.Grabbed());
		else if (_context.IsDead)
			SwitchState(_factory.Dead());
		else if (!_isFlying)
			SwitchState(_factory.Walk());
	}

	public override void EnterState()
	{
		_context.IsFlung = false;
		_isFlying = true;
		Throw(_context.ThrowForce);
	}

	public override void ExitState()
	{
		_isFlying = false;
	}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		Debug.Log($"{_debugInfo}; flying with throwPower[{_throwPower}]");
		UpdateDistortion();

		if (HasLanded())
		{
			// RemoveArrow();//TODO
			GroundSlam();
		}

		CheckSwitchStates();
	}

	private bool HasLanded()
	{
		return _context.transform.position.y <= RandomizeLanding();
	}

	private void UpdateDistortion()
	{
		if (_context.RB.velocity.magnitude > 2f)
		{
			// SetArrow(_context.RB.velocity, 0.25f);//TODO
		}
		// else
		// RemoveArrow();
	}

	private void GroundSlam()
	{
		_isFlying = false;
		_context.GrabbedPosY = _context.transform.position.y;
		_context.Health.TakeDamage(_throwPower);
	}

	private void Throw(Vector2 throwForce)
	{
		// ChangeSize(CoreHelper.GetDepthModifier(transform.position.y));
		// StartCoroutine(DrawDistortion(0.5f, throwForce)); //TODO
		_context.RB.isKinematic = false;
		_context.RB.velocity = Vector2.zero;
		_context.RB.AddForce(throwForce, ForceMode2D.Impulse);

		_throwPower = CalculateThrowPower(throwForce.magnitude);

		float CalculateThrowPower(float force)
		{
			float res;
			switch (force)
			{
				case > 6 and < 8:
					res = 0.3f;
					break;
				case > 8:
					float normalizedValue = Mathf.InverseLerp(8, 10, force);
					res = Mathf.Lerp(0.3f, 0.5f, normalizedValue);
					break;
				default:
					res = 0;
					break;
			}
			return res * _context.Health.maxHealth;
		}
	}

	private float RandomizeLanding()
	{
		return Mathf.Clamp(_context.GrabbedPosY + Random.Range(-1f, 1f), 
							CoreHelper.groundBottomY, 
							CoreHelper.groundTopY);
	}

	public override void OnCollisionEnter2D(Collision2D other) 
	{
		if(other.gameObject.CompareTag("Border"))
		{
			Debug.Log($"{_debugInfo};took[{_throwPower * 0.5f}] damage from collision with [{other.gameObject.name}]");
			_context.Health.TakeDamage(_throwPower * 0.5f);
			_throwPower = 1.3f * _throwPower;
		} else if (other.gameObject.CompareTag("Enemy"))
		{
			if (_context.RB.velocity.magnitude > _context.CollisionDamageThreshold)
			{
				Health otherMonster = other.gameObject.GetComponent<Health>();
				int damage = Mathf.RoundToInt(_throwPower * _context.MonsterCollisionDamageMultiplier);
				_context.Health.TakeDamage(damage);
				otherMonster.TakeDamage(damage);

				// Apply impulse force to the other monster
				Vector2 impulseForce = _context.RB.velocity.normalized * damage * 0.1f; // Adjust the multiplier as needed
				otherMonster.GetComponent<EnemyStateMachine>().ApplyImpulse(impulseForce);
				Debug.Log($"{_debugInfo};took[{_throwPower * _context.MonsterCollisionDamageMultiplier}] damage from collision with [{otherMonster.GetComponent<EnemyStateMachine>().ID}]");
			}
		}
	}

	public override void OnAttackEvent(){}
}
