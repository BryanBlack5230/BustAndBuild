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
	private TrailRenderer _trail;
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
		_trail = _context.transform.GetChild(0).transform.GetChild(1).gameObject.GetComponent<TrailRenderer>();
		_context.IsFlung = false;
		_isFlying = true;
		_context.IsOnGround = false;
		Throw(_context.ThrowForce);
	}

	public override void ExitState()
	{
		_isFlying = false;
		_trail.enabled = false;
	}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		Debug.Log($"{_debugInfo};flying with throwPower[{_throwPower}]");
		UpdateDistortion();

		if (HasLanded())
		{
			_trail.enabled = false;
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
			_trail.enabled = true;
		else
			_trail.enabled = false;
	}

	private void GroundSlam()
	{
		_context.BodyCollider.enabled = true;
		_isFlying = false;
		_context.GrabbedPosY = _context.transform.position.y;
		_context.Health.TakeDamage(_throwPower);
		if (_throwPower > 0)
		{
			ApplyImpactForce();
			var deathParticles = ParticleSettings(_context.DeathParticles);
			PlayParticle(_context.DustExplosionParticles);
			ParticleStart(deathParticles);
			HitEffects();
		}
	}

	private void ApplyImpactForce()
	{
		foreach (var closeEnemy in _context.NearbyEnemies)
		{
			float distance = Vector2.Distance(closeEnemy.transform.position, _context.transform.position);
			if (distance > 1.2f) continue;

			Vector2 direction = (closeEnemy.transform.position - _context.transform.position).normalized;

			// Calculate impact force based on falling angle
			float angle = Vector2.Angle(Vector2.down, _context.RB.velocity.normalized);
			float force = 10f * Mathf.Cos(angle * Mathf.Deg2Rad);

			// Apply the force
			
			closeEnemy.Throw(direction * force);
		}
	}

	private void Throw(Vector2 throwForce)
	{
		if (CoreHelper.groundTopY > _context.transform.position.y)
			throwForce = throwForce.normalized * 1.5f;
		
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

	public override void OnBodyCollision(Collision2D other) 
	{
		if(other.gameObject.CompareTag("Border"))
		{
			HandleBorderCollision(other);
		}
		else if (other.gameObject.CompareTag("Enemy"))
			HandleEnemyCollision(other);
	}

	public override void OnIncomingCollisionTrigger(Collider2D other)
	{
		if (other.gameObject.TryGetComponent<EnemyStateMachine>(out var otherSM))
		{
			if (otherSM.IsOnGround)
			{
				_context.BodyCollider.enabled = false;
				return;
			}
		}
	}

	private void HandleEnemyCollision(Collision2D other)
	{
		if (_context.RB.velocity.magnitude <= _context.CollisionDamageThreshold) return;

		if (other.gameObject.TryGetComponent<EnemyStateMachine>(out var otherSM))
		{
			int damage = Mathf.RoundToInt(_throwPower * _context.MonsterCollisionDamageMultiplier);

			_context.Health.TakeDamage(damage);
			HitEffects();
			otherSM.TakeDamage(damage);

			SoundManager.Instance.PlaySound(otherSM.AudioList.GetClip(SoundType.Hurt));

			Vector2 impulseForce = _context.RB.velocity.normalized * damage * 0.1f;
			otherSM.ApplyImpulse(impulseForce);
			Debug.Log($"{_debugInfo};took[{_throwPower * _context.MonsterCollisionDamageMultiplier}] damage from collision with [{otherSM.ID}]");
		}

	}

	private void HandleBorderCollision(Collision2D other)
	{
		Debug.Log($"{_debugInfo};took[{_throwPower * 0.5f}] damage from collision with [{other.gameObject.name}]");
		_context.Health.TakeDamage(_throwPower * 0.5f);
		HitEffects();
		_throwPower = 1.3f * _throwPower;
	}

	private void HitEffects()
	{
		SoundManager.Instance.PlaySound(_context.AudioList.GetClip(SoundType.Hurt));
		
		var hitParticles = ParticleSettings(_context.HitParticles);
		ParticleStart(hitParticles);
	}

	private GameObject ParticleSettings(GameObject target)
	{
		var result = target;

		var ps = result.GetComponent<ParticleSystem>();
		var main = ps.main;

		var emission = ps.emission;
		var burst = new ParticleSystem.Burst();
		burst.count = _throwPower * 0.3f;
		emission.SetBurst(0, burst);

		return result;
	}

	private void ParticleStart(GameObject particle)
	{
		var instance = Object.Instantiate(particle, _context.transform.position, _context.transform.rotation);
		if (instance.transform.childCount > 0)
		{
			var child = instance.transform.GetChild(0);
			var childMat = child.GetComponent<ParticleSystem>().GetComponent<Renderer>().material;

			childMat.SetColor("_ColorBot", _context.ColorStart);
			childMat.SetColor("_ColorTop", _context.ColorEnd);
		}
		var ps = instance.GetComponent<ParticleSystem>();

		var mat = ps.GetComponent<Renderer>().material;

		mat.SetColor("_ColorBot", _context.ColorStart);
		mat.SetColor("_ColorTop", _context.ColorEnd);

		ps.Play();
		Object.Destroy(instance, ps.main.duration);
	}

	private void PlayParticle(GameObject particle)
	{
		var instance = Object.Instantiate(particle, _context.transform.position, Quaternion.identity);
		var ps = instance.GetComponent<ParticleSystem>();
		ps.Play();
		Object.Destroy(instance, ps.main.duration);
	}

	public override void OnAttackEvent(){}

	
}
