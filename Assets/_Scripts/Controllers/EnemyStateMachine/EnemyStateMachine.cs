using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using BB.Resources;
using BB.Movement;
using BB.Stats;
using System;
using TMPro;
using BB.Combat;

public class EnemyStateMachine : MonoBehaviour
{
	[SerializeField] float attackCoolDown = 1f;
	[SerializeField] private float attackRange = 0f;
	[SerializeField] MobAudio audioList;
	[SerializeField] Health castle;
	[SerializeField] GameObject hitParticles;
	[SerializeField] GameObject deathParticles;
	[SerializeField] GameObject dustExplosionParticles;
	[SerializeField] GameObject wallHitParticles;
	[SerializeField] ColorScheme colorScheme;
	[SerializeField] string UID;
	[SerializeField] public string RootState;
	[SerializeField] public string SubState;

	public EnemyBaseState currentState {get { return _currentState;} set {_currentState = value;}}
	public bool IsNearTarget { get {return _isNearTarget;} set {_isNearTarget = value;}}
	public float AttackCooldown { get { return attackCoolDown;}}
	public Animator Animator {get {return _animator;}}
	public int AttackTriggerCached {get {return _attackCashed;}}
	public int ImmobilisedTriggerCached {get {return _immobilizedCashed;}}
	public int WalkingTriggerCached {get {return _walkingCashed;}}
	public bool IsGrabbed {get {return _grabbable.IsGrabbed;} set {_grabbable.IsGrabbed = value;}}
	public bool IsAware {get {return _isAware;} set {_isAware = value;}}
	public bool IsSuspicious {get {return _isSuspicious;} set {_isSuspicious = value;}}
	public bool IsDead {get {return _health.IsDead();}}
	public bool IsFlung {get {return _grabbable.IsFlung;} set {_grabbable.IsFlung = value;}}
	public Rigidbody2D RB {get {return _rb;}}
	public Movement Mover {get {return _movement;}}
	public Health Target {get {return _target;} set {_target = value;}}
	public Vector2 OriginalSize {get {return _originalSize;} set {_originalSize = value;}}
	public Health Health {get {return _health;} set {_health = value;}}
	public float GrabbedPosY {get {return _posYBeforeGrabbed;} set {_posYBeforeGrabbed = value;}}
	public Vector2 ThrowForce {get {return _grabbable.ThrowForce;} set {_grabbable.ThrowForce = value;}}
	public float AttackDamage {get {return _stats.GetAttackDamage();}}
	public MobAudio AudioList {get {return audioList;}}
	public ObjectPool<EnemyStateMachine> Pool {get {return _pool;}}
	public float AttackRange {get {return attackRange;}}
	public float CollisionDamageThreshold {get {return collisionDamageThreshold;}}
	public float MonsterCollisionDamageMultiplier {get {return monsterCollisionDamageMultiplier;}}
	public Health Castle {get {return castle;} set {castle = value;}}
	public ColorScheme ColorScheme {get {return colorScheme;}}
	public string ID {get {return UID;}}
	public GameObject HitParticles {get {return hitParticles;}}
	public GameObject DeathParticles {get {return deathParticles;}}
	public GameObject DustExplosionParticles {get {return dustExplosionParticles;}}
	public GameObject WallHitParticles {get {return wallHitParticles;}}
	public Collider2D BodyCollider {get {return _bodyCollider;}}
	public SpriteRenderer Shadow {get {return _shadow;}}
	public IsometricObjectHandler IsometricHandler {get {return _ioHandle;}}
	public SecondOrderImitation SecondOrderAnimation {get {return _secondOrderImitation;}}
	[HideInInspector] public float StateSizeModifier, StateSpeedModifier, StateAttackSpeedCDModifier;
	[HideInInspector] public Eyes Pupils, Eyes;
	[HideInInspector] public bool IsReturningToPool, IsOnGround;
	[HideInInspector] public Color ColorStart, ColorEnd;
	[HideInInspector] public List<EnemyStateMachine> NearbyEnemies = new List<EnemyStateMachine>();

	private EnemyBaseState _currentState;
	private EnemyStateFactory _states;
	private Health _target, _health;
	private ObjectPool<EnemyStateMachine> _pool;
	private Movement _movement;
	private Rigidbody2D _rb;
	private BaseStats _stats;
	private IsometricObjectHandler _ioHandle;
	private Animator _animator;
	private Collider2D _bodyCollider;
	private int _attackCashed = Animator.StringToHash("attack");
	private int _immobilizedCashed = Animator.StringToHash("immobilized");
	private int _walkingCashed = Animator.StringToHash("walking");
	private float _posYBeforeGrabbed;
	private Vector3 _originalSize;
	private bool _isAware, _isSuspicious, _isNearTarget;
	private int collisionDamageThreshold = 8;
	private float monsterCollisionDamageMultiplier = 1.5f;
	private Transform _debugText;
	private TextMeshProUGUI _rootStateText, _subStateText;
	private Transform _gfx;
	private SpriteRenderer _shadow;
	private Grabbable _grabbable;
	private SecondOrderImitation _secondOrderImitation;

	void Awake()
	{
		UID = gameObject.GetInstanceID().ToString();
		_health = GetComponent<Health>();
		_movement = GetComponent<Movement>();
		_rb = GetComponent<Rigidbody2D>();
		_stats = GetComponent<BaseStats>();
		// _animator = transform.GetChild(0).GetComponent<Animator>();
		_ioHandle = GetComponent<IsometricObjectHandler>();

		_gfx =  transform.GetChild(0);
		Transform vfx =  transform.GetChild(1);
		_debugText = transform.GetChild(2);

		_debugText.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"[{UID}]";
		_rootStateText = _debugText.GetChild(1).GetComponent<TextMeshProUGUI>();
		_subStateText = _debugText.GetChild(2).GetComponent<TextMeshProUGUI>();

		_shadow = vfx.GetChild(0).GetComponent<SpriteRenderer>();

		Transform body = _gfx.GetChild(4);
		_bodyCollider = body.GetComponent<Collider2D>();
		_grabbable = body.GetComponent<Grabbable>();
		_secondOrderImitation = body.GetChild(1).GetComponent<SecondOrderImitation>();
		Transform face = body.GetChild(2);
		
		Pupils.left = face.GetChild(0);
		Pupils.right = face.GetChild(1);
		Eyes.left = face.GetChild(2);
		Eyes.right = face.GetChild(3);

		_states = new EnemyStateFactory(this);
	}

	private void Start() 
	{
		_originalSize = transform.localScale;
		_posYBeforeGrabbed = transform.position.y;

		_currentState = _states.Aware();
		_currentState.EnterState();

		SetDebugText();
	}

	void Update()
	{
		_currentState.UpdateStates();
		SetDebugText();
	}

	private void SetDebugText()
	{
		_rootStateText.text = $"[{RootState}]";
		_subStateText.text = $"[{SubState}]";
	}

	public void OnBodyCollision(Collision2D other) 
	{
		_currentState.OnBodyCollision(other);
	}

	public void OnIncomingCollisionTrigger(Collider2D other)
	{
		_currentState.OnIncomingCollisionTrigger(other);
	}

	private void OnTriggerStay2D(Collider2D other) 
	{
		if (other.TryGetComponent<EnemyStateMachine>(out EnemyStateMachine enemy))
		{
			if (enemy.IsGrabbed)
				_isSuspicious = true;

			if (!NearbyEnemies.Contains(enemy))
				NearbyEnemies.Add(enemy);
		}
	}

	private void OnTriggerExit2D(Collider2D other) 
	{
		if (other.TryGetComponent<EnemyStateMachine>(out EnemyStateMachine enemy))
		{
			if (NearbyEnemies.Contains(enemy))
				NearbyEnemies.Remove(enemy);
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
		Destroy(gameObject);
	}

	public void SetPosition(Vector2 position)
	{
		_rb.MovePosition(position);
	}

	public void ReturnToPool()
	{
		IsReturningToPool = true;
		_currentState.HandleDeath();
	}

	public void Revive()
	{
		_currentState?.ExitState();
		_currentState = _states.Aware();
		_currentState.EnterState();
	}

	public void TakeDamage(float damage)
	{
		_health.TakeDamage(damage);
	}

	public void Throw(Vector2 throwForce)
	{
		IsFlung = true;
		_posYBeforeGrabbed = transform.position.y;
		Animator.SetTrigger(_immobilizedCashed);
		_grabbable.ThrowForce = throwForce;
		_currentState.SwitchSubState(_states.Flung());
	}

	private void OnEnable() {
		GameStateManager.OnBrazierDestroyed += OnBrazierDestroyed;
		_grabbable.OnSetPosition += SetPosition;
	}

	private void OnDisable() {
		GameStateManager.OnBrazierDestroyed -= OnBrazierDestroyed;
		_grabbable.OnSetPosition -= SetPosition;
	}

	private void OnBrazierDestroyed()
	{
		_target = ConstantTargets.Sea.GetComponent<Health>();
	}
}
