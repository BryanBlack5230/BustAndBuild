using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using BB.Resources;
using BB.Movement;
using BB.Stats;

public class EnemyStateMachine : MonoBehaviour
{
	EnemyBaseState _currentState;
	EnemyStateFactory _states;
	public EnemyBaseState currentState {get { return _currentState;} set {_currentState = value;}}
	public bool IsNearTarget { get {return _isNearTarget;} set {_isNearTarget = value;}}
	private Health _target;
	[SerializeField] float attackCoolDown = 1f;
	[SerializeField] private float attackRange = 0f;
	[SerializeField] Health castle;
	private ObjectPool<EnemyStateMachine> _pool;
	private Health _health;
	private Movement _movement;
	private Rigidbody2D _rb;
	private EnemyAudio _audio;
	private Animator _animator;
	private int _attackCashed = Animator.StringToHash("attack");
	private int _stopAttackCashed = Animator.StringToHash("stopAttack");
	private int _immobilizedCashed = Animator.StringToHash("immobilized");
	private int _walkingCashed = Animator.StringToHash("walking");
	// private Quaternion _originalRotation; can it be changet to Quaternion.identity;?
	private float _posYBeforeGrabbed;
	private Vector3 _originalSize;
	private float _currentSpeed;
	private float _timeSinceLastAttack = Mathf.Infinity;
	private bool _isGrabbed = false;
	private bool _isFlying = false;
	public float _throwPower;
	private int collisionDamageThreshold = 8;
	private float monsterCollisionDamageMultiplier = 1.5f;
    private bool _isAware;
    private bool _isSuspicious;
    private bool _isNearTarget;
    private Vector2 _throwForce;
    private BaseStats _stats;

    public float AttackCooldown { get { return attackCoolDown;}}
	public Animator Animator {get {return _animator;}}
	public int AttackTriggerCached {get {return _attackCashed;}}
	public int ImmobilisedTriggerCached {get {return _immobilizedCashed;}}
	public int WalkingTriggerCached {get {return _walkingCashed;}}
	public bool IsGrabbed {get {return _isGrabbed;} set {_isGrabbed = value;}}
	public bool IsAware {get {return _isAware;}}
	public bool IsSuspicious {get {return _isSuspicious;}}
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
	public Health Castle {get {return castle;}}

	void Awake()
	{
		_health = GetComponent<Health>();
		_movement = GetComponent<Movement>();
		_rb = GetComponent<Rigidbody2D>();
		_audio = GetComponent<EnemyAudio>();
		_stats = GetComponent<BaseStats>();
		_animator = transform.GetChild(0).GetComponent<Animator>();

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

	private Vector2 TargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(transform.position.y) : target.position;
	}

	private void OnCollisionEnter2D(Collision2D other) 
	{
		_currentState.OnCollisionEnter2D(other);
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
	
}
