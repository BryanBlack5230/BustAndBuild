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
	private Vector2 _targetPos;
	[SerializeField] float attackCoolDown = 1f;
	[SerializeField] private float attackRange = 0f;
	private ObjectPool<AIController> _pool;
	private Health _health;
	private Movement _movement;
	private Rigidbody2D _rb;
	private EnemyAudio _audio;
	private Animator _animator;
	private int _attackCashed = Animator.StringToHash("attack");
	private int _stopAttackCashed = Animator.StringToHash("stopAttack");
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
	public bool IsGrabbed {get {return _isGrabbed;} set {_isGrabbed = value;}}
	public bool IsAware {get {return _isAware;}}
	public bool IsSuspicious {get {return _isSuspicious;}}
	public bool IsDead {get {return _health.IsDead();}}
	public bool IsFlung {get {return _isFlying;}}
	public Rigidbody2D RB {get {return _rb;}}
	public Movement Mover {get {return _movement;}}
	public Health Target {get {return _target;} set {_target = value;}}
	public Vector2 OriginalSize {get {return _originalSize;} set {_originalSize = value;}}
	public Health Health {get {return _target;} set {_target = value;}}
	public float GrabbedPosY {get {return _posYBeforeGrabbed;} set {_posYBeforeGrabbed = value;}}
	public Vector2 ThrowForce {get {return _throwForce;} set {_throwForce = value;}}
	public float AttackDamage {get {return _stats.GetAttackDamage();}}
	public EnemyAudio Audio {get {return _audio;}}
	public ObjectPool<AIController> Pool {get {return _pool;}}
	public float AttackRange {get {return attackRange;}}


	void Start()
	{
		_states = new EnemyStateFactory(this);
		_currentState = _states.Walk();
		_currentState.EnterState();


		_targetPos = TargetPos(_target.transform);
		_health = GetComponent<Health>();
		_movement = GetComponent<Movement>();
		_rb = GetComponent<Rigidbody2D>();
		_audio = GetComponent<EnemyAudio>();
		_stats = GetComponent<BaseStats>();
		// _originalRotation = transform.rotation;
		_posYBeforeGrabbed = transform.position.y;


		_originalSize = transform.localScale;
	}

	void Update()
	{
		_currentState.UpdateStates();
	}

	private Vector2 TargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(transform.position.y) : target.position;
	}
	
}
