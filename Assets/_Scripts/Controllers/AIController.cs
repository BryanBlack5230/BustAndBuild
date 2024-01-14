using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using BB.Resources;
using BB.Movement;
using BB.Stats;


public class AIController : MonoBehaviour
{
	[SerializeField] private Health target;
	private Vector2 _targetPos;
	[SerializeField] float attackCoolDown = 1f;
	[SerializeField] private float attackRange = 0f;
	[Range(0,1)][SerializeField] float maxSpeed = 1f;
	[SerializeField] Animator animator;
	private Health _health;
	private Movement _movement;
	private Quaternion _originalRotation;
	private float _posYBeforeGrabbed;
	private Vector3 _originalSize;
	private float _currentSpeed;
	private Rigidbody2D _rb;

	private EnemyAudio _audio;

	private int _attackCashed = Animator.StringToHash("attack");
	private int _stopAttackCashed = Animator.StringToHash("stopAttack");
	private float _timeSinceLastAttack = Mathf.Infinity;

	private bool _isGrabbed = false;
	private bool _isFlying = false;
	public float _throwPower;
	private int collisionDamageThreshold = 8;
	private float monsterCollisionDamageMultiplier = 1.5f;
	private ObjectPool<AIController> _pool;

	void Start()
	{
		_targetPos = TargetPos(target.transform);
		_health = GetComponent<Health>();
		_movement = GetComponent<Movement>();
		_rb = GetComponent<Rigidbody2D>();
		_audio = GetComponent<EnemyAudio>();
		_originalRotation = transform.rotation;
		_posYBeforeGrabbed = transform.position.y;

		_currentSpeed = maxSpeed;

		_originalSize = transform.localScale;
	}

	void Update()
	{
		_timeSinceLastAttack += Time.deltaTime;
		if (_health.IsDead()) return;
		if (_isGrabbed) return;

		if (_isFlying)
		{
			if (_rb.velocity.magnitude > 2f)
			{
				// SetArrow(_rb.velocity, 0.25f);
			}
			// else
				// RemoveArrow();
			float potentialLanding = Mathf.Clamp(_posYBeforeGrabbed + Random.Range(-1f, 1f), CoreHelper.groundBottomY, CoreHelper.groundTopY);
			if (transform.position.y <= potentialLanding)
			{
				// RemoveArrow();
				GroundSlam();
				if (_health.IsDead()) 
				{
					_audio.Play("death");
					return;
				} 
				ResumeWalking();
			}
			return;
		} 
		else 
		{
			UpdateSizeAndSpeedByDepth();
		}


		if (CloseToEnemy()) /*&& fighter.CanAttack(player)*/
		{
			AttackBehaviour(); //TODO
		}
		else
		{
			MovingBehaviour();
		}
	}

	private void AttackBehaviour()
	{
		if (_timeSinceLastAttack > attackCoolDown)
		{
			// This will trigger Hit()
			//animator.ResetTrigger(stopAttackCashed);
			animator.SetTrigger(_attackCashed);
			_timeSinceLastAttack = 0;
			//StartCoroutine(ResetAnimation(0.8f * attackCoolDown, stopAttackCashed));
		}
	}

	IEnumerator ResetAnimation(float time, int animationTrigger)
	{
		yield return new WaitForSeconds(time);
		animator.ResetTrigger(_attackCashed);
		animator.SetTrigger(animationTrigger);
	}

	private void MovingBehaviour()
	{
		_movement.MoveTo(_targetPos, _currentSpeed);
	}

	private bool CloseToEnemy()
	{
		return Vector3.Distance(_targetPos, transform.position) < attackRange;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
	}

	public void SetTarget(Health _target)
	{
		target = _target;
		_targetPos = TargetPos(target.transform);
	}

	public void SetAnimator(Animator _animator)
	{
		animator = _animator;
	}


	public void Throw(Vector2 throwForce)
	{
		_isGrabbed = false;
		ChangeSize(CoreHelper.GetDepthModifier(transform.position.y));
		// StartCoroutine(DrawDistortion(0.5f, throwForce)); //TODO
		_rb.isKinematic = false;
		_rb.velocity = Vector2.zero;
		_rb.AddForce(throwForce, ForceMode2D.Impulse);
		_isFlying = true;
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
			return res * _health.maxHealth;
		}
	}

	private void GroundSlam()
	{
		_isFlying = false;
		_posYBeforeGrabbed = transform.position.y;
		_health.TakeDamage(_throwPower);
	}

	private void ResumeWalking()
	{
		StartCoroutine(StartWalk());
	}

	private IEnumerator StartWalk()
	{
		yield return null;
		_rb.velocity = Vector2.zero;
		_rb.isKinematic = true;
		_targetPos = TargetPos(target.transform);
		_movement.StartMoveAction(_targetPos, _currentSpeed);
		_rb.angularVelocity = 0f;
		_rb.SetRotation(_originalRotation);
		transform.rotation = _originalRotation;
	}

	public void GrabbedBehaviour()
	{
		if (!_isGrabbed)
		{
			_isGrabbed = true;
			ChangeSize(1f);
			_movement.Cancel();
		}
	}

	public void SetPosition(Vector2 position)
	{
		_rb.MovePosition(position);
	}

	private void UpdateSizeAndSpeedByDepth()
	{
		float depthModifier =  CoreHelper.GetDepthModifier(transform.position.y);

		transform.localScale = new Vector3(_originalSize.x * depthModifier,
										   _originalSize.y * depthModifier,
										   _originalSize.z);

		_currentSpeed = maxSpeed * depthModifier;
	}

	private void OnCollisionEnter2D(Collision2D other) {

		if(other.gameObject.CompareTag("Border"))
		{
			_health.TakeDamage(_throwPower * 0.5f);
			_throwPower = 1.3f * _throwPower;
		} else if (other.gameObject.CompareTag("Enemy"))
		{
			if (_rb.velocity.magnitude > collisionDamageThreshold)
			{
				Health otherMonster = other.gameObject.GetComponent<Health>();
				int damage = Mathf.RoundToInt(_throwPower * monsterCollisionDamageMultiplier);
				_health.TakeDamage(damage);
				otherMonster.TakeDamage(damage);

				// Apply impulse force to the other monster
				Vector2 impulseForce = _rb.velocity.normalized * damage * 0.1f; // Adjust the multiplier as needed
				otherMonster.GetComponent<AIController>().ApplyImpulse(impulseForce);
			}
		}
	}

	private void ChangeSize(float targetSize)
	{
		StartCoroutine(LerpChange(targetSize));

		IEnumerator LerpChange(float targetSize)
		{
			float size = transform.localScale.x;

			for (float t = 0.001f; t < 0.1f; t +=Time.deltaTime)
			{
				float newSize = Mathf.Lerp(size, targetSize, t / 0.1f);
				transform.localScale = new Vector3(_originalSize.x * newSize, 
												   _originalSize.y * newSize,
													transform.localScale.z);
				yield return null;
			}

			transform.localScale = new Vector3(_originalSize.x * targetSize, 
												_originalSize.y * targetSize,
												transform.localScale.z);
		}
	}

	public void ApplyImpulse(Vector2 externalImpulse)
	{
		_rb.AddForce(externalImpulse, ForceMode2D.Impulse);
	}

	public bool IsFlying()
	{
		return _isFlying;
	}

	public void SetPool(ObjectPool<AIController> pool) => _pool = pool;

	public void OnDeath()
	{
		_movement.Cancel();
		_rb.isKinematic = true;
		transform.GetChild(0).gameObject.SetActive(false);
		_audio.Play("death");
		StartCoroutine(PlayDeatAudio());
		
		IEnumerator PlayDeatAudio() 
		{
			while (_audio.isPlaying){
				yield return null;
			}
			ReturnToPool();
		}
	}

	public void ReturnToPool()
	{
		if (_pool != null)
			_pool.Release(this);
		else
			Destroy(gameObject);
	}

	public void Revive()
	{
		if (_health == null) _health = GetComponent<Health>();
		_health.Revive();
		transform.GetChild(0).gameObject.SetActive(true);
		ResumeWalking();
	}

	public void OnAttackEvent()
	{
		target.TakeDamage(GetComponent<BaseStats>().GetAttackDamage());
		_audio.Play("attack");
	}

	private Vector2 TargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(transform.position.y) : target.position;
	}

}
