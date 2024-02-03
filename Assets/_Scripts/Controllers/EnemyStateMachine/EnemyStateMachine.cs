using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using BB.Resources;
using BB.Movement;
using BB.Stats;
using System;

public class EnemyStateMachine : MonoBehaviour
{
	[SerializeField] float attackCoolDown = 1f;
	[SerializeField] private float attackRange = 0f;
	[SerializeField] Health castle;
	[SerializeField] Color awareTopColor;
	[SerializeField] Color awareBotColor;
	[SerializeField] float awareBlendHeight;
	[SerializeField] Color suspiciousTopColor;
	[SerializeField] Color suspiciousBotColor;
	[SerializeField] float suspiciousBlendHeight;
	[SerializeField] string UID;
	public EnemyBaseState currentState {get { return _currentState;} set {_currentState = value;}}
	public bool IsNearTarget { get {return _isNearTarget;} set {_isNearTarget = value;}}
	public float AttackCooldown { get { return attackCoolDown;}}
	public Animator Animator {get {return _animator;}}
	public int AttackTriggerCached {get {return _attackCashed;}}
	public int ImmobilisedTriggerCached {get {return _immobilizedCashed;}}
	public int WalkingTriggerCached {get {return _walkingCashed;}}
	public bool IsGrabbed {get {return _isGrabbed;} set {_isGrabbed = value;}}
	public bool IsAware {get {return _isAware;} set {_isAware = value;}}
	public bool IsSuspicious {get {return _isSuspicious;} set {_isSuspicious = value;}}
	public bool IsDead {get {return _health.IsDead();}}
	public bool IsFlung {get {return _isFlying;} set {_isFlying = value;}}
	public Rigidbody2D RB {get {return _rb;}}
	public Movement Mover {get {return _movement;}}
	public Health Target {get {return _target;} set {_target = value;}}
	public Vector2 OriginalSize {get {return _originalSize;} set {_originalSize = value;}}
	public Health Health {get {return _health;} set {_health = value;}}
	public float GrabbedPosY {get {return _posYBeforeGrabbed;} set {_posYBeforeGrabbed = value;}}
	public Vector2 ThrowForce {get {return _throwForce;} set {_throwForce = value;}}
	public float AttackDamage {get {return _stats.GetAttackDamage();}}
	public EnemyAudio Audio {get {return _audio;}}
	public ObjectPool<EnemyStateMachine> Pool {get {return _pool;}}
	public float AttackRange {get {return attackRange;}}
	public float CollisionDamageThreshold {get {return collisionDamageThreshold;}}
	public float MonsterCollisionDamageMultiplier {get {return monsterCollisionDamageMultiplier;}}
	public Health Castle {get {return castle;} set {castle = value;}}
	public Color AwareTopColor {get {return awareTopColor;}}
	public Color AwareBotColor {get {return awareBotColor;}}
	public float AwareBlend {get {return awareBlendHeight;}}
	public Color SuspiciousTopColor {get {return suspiciousTopColor;}}
	public Color SuspiciousBotColor {get {return suspiciousBotColor;}}
	public float Suspiciouslend {get {return suspiciousBlendHeight;}}
	public string ID {get {return UID;}}
	public IsometricObjectHandler IsometricHandler {get {return _ioHandle;}}
	private EnemyBaseState _currentState;
	private EnemyStateFactory _states;
	private Health _target;
	private ObjectPool<EnemyStateMachine> _pool;
	private Health _health;
	private Movement _movement;
	private Rigidbody2D _rb;
	private EnemyAudio _audio;
	private BaseStats _stats;
	private IsometricObjectHandler _ioHandle;
	private Animator _animator;
	private int _attackCashed = Animator.StringToHash("attack");
	private int _immobilizedCashed = Animator.StringToHash("immobilized");
	private int _walkingCashed = Animator.StringToHash("walking");
	private float _posYBeforeGrabbed;
	private Vector3 _originalSize;
	private bool _isGrabbed = false;
	private bool _isFlying = false;
	private bool _isAware;
	private bool _isSuspicious;
	private bool _isNearTarget;
	private Vector2 _throwForce;
	private int collisionDamageThreshold = 8;
	private float monsterCollisionDamageMultiplier = 1.5f;

	void Awake()
	{
		// UID = System.Guid.NewGuid().ToString();
		UID = gameObject.GetInstanceID().ToString();
		_health = GetComponent<Health>();
		_movement = GetComponent<Movement>();
		_rb = GetComponent<Rigidbody2D>();
		_audio = GetComponent<EnemyAudio>();
		_stats = GetComponent<BaseStats>();
		_animator = transform.GetChild(0).GetComponent<Animator>();
		_ioHandle = GetComponent<IsometricObjectHandler>();

		_states = new EnemyStateFactory(this);
	}

	private void Start() 
	{
		_originalSize = transform.localScale;
		_posYBeforeGrabbed = transform.position.y;

		_currentState = _states.Aware();
		_currentState.EnterState();
	}

	void Update()
	{
		_currentState.UpdateStates();
	}

	private void OnCollisionEnter2D(Collision2D other) 
	{
		_currentState.OnCollisionEnter2D(other);
	}

	private void OnTriggerStay2D(Collider2D other) 
	{
		if (other.TryGetComponent<EnemyStateMachine>(out EnemyStateMachine enemy))
		{
			if (enemy.IsGrabbed)
				_isSuspicious = true;
		}
	}

	public void OnAttackEvent()
	{
		_currentState.OnAttackEvent();
	}

	public void ApplyImpulse(Vector2 externalImpulse)
	{
		_rb.AddForce(externalImpulse, ForceMode2D.Impulse);
	}

	public void SetPool(ObjectPool<EnemyStateMachine> pool) => _pool = pool;

	public void Destroy()
	{
		Destroy(this);
	}

	public void SetPosition(Vector2 position)
	{
		_rb.MovePosition(position);
	}

	public void ReturnToPool()
	{
		_currentState = _states.Dead();
		_currentState.EnterState();
	}

	public void Revive()
	{
		_currentState?.ExitState();
		_currentState = _states.Aware();
		_currentState.EnterState();
	}
	
}
