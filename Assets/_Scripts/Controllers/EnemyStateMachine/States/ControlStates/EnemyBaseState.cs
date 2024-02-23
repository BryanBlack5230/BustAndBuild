using System;
using UnityEngine;

public abstract class EnemyBaseState
{
	protected bool _isRootState = false;
	protected EnemyStateMachine _context;
	protected EnemyStateFactory _factory;
	protected EnemyBaseState _currentSuperState;
	protected EnemyBaseState _currentSubState;
	protected string _debugInfo;
	protected EnemyStates _stateName;

	public EnemyBaseState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	{
		_context = currentContext;
		_factory = factory;
		_stateName = stateName;
	}
	
	public abstract void EnterState();
	public abstract void UpdateState();
	public abstract void ExitState();
	public abstract void CheckSwitchStates();
	public abstract void InitializeSubState();
	public abstract void OnAttackEvent();
	public abstract void OnBodyCollision(Collision2D other);
	public abstract void OnIncomingCollisionTrigger(Collider2D other);
	public void UpdateStates()
	{
		UpdateDebugInfo();
		Debug.Log($"{_debugInfo};updating {_stateName.ToString()}");
		UpdateState();
		if (_currentSubState != null)
			_currentSubState.UpdateStates();
	}
	public void SwitchState(EnemyBaseState newState, EnemyBaseState subState = null)
	{
		Debug.Log($"{_debugInfo};switch state to [{newState}]");
		ExitState();
		if (_isRootState)
		{
			_context.currentState = newState;
			newState.SetSubState(subState);
		}
		else if (_currentSuperState != null)
			_currentSuperState.SetSubState(newState);
			
		newState.EnterState();
	}
	protected void SetSuperState(EnemyBaseState newSuperState)
	{
		_currentSuperState = newSuperState;
	}
	protected void SetSubState(EnemyBaseState newSubState)
	{
		_currentSubState = newSubState;
		newSubState?.SetSuperState(this);
	}

	protected void UpdateDebugInfo()
	{
		string rootName = (_currentSuperState != null) ? _currentSuperState._stateName.ToString() : _stateName.ToString();
		string subName = (_currentSubState != null) ? _currentSubState._stateName.ToString() : _stateName.ToString();
		string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
		_debugInfo = $"[{timeStamp}];[{GetType().Name}];[{_context.ID}];[x:{_context.transform.position.x},y:{_context.transform.position.y}];[{rootName}];[{subName}]";

		_context.RootState = rootName;
		_context.SubState = subName;
	}

	public void HandleDeath()
	{
		_context.Health.TakeDamage(_context.Health.maxHealth);
		_currentSubState?.ExitState();
		SwitchState(_factory.Dead());
	}

	public void SwitchSubState(EnemyBaseState newSubState)
	{
		if (_isRootState)
		{
			_currentSubState.ExitState();
			SetSubState(newSubState);
		}
		else
		{
			ExitState();
			_currentSuperState.SetSubState(newSubState);
		}
		newSubState.EnterState();
	}

}
