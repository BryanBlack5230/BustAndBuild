using UnityEngine;

public abstract class EnemyBaseState
{
	protected bool _isRootState = false;
	protected EnemyStateMachine _context;
	protected EnemyStateFactory _factory;
	protected EnemyBaseState _currentSuperState;
	protected EnemyBaseState _currentSubState;

	public EnemyBaseState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	{
		_context = currentContext;
		_factory = factory;
	}
	public abstract void EnterState();
	public abstract void UpdateState();
	public abstract void ExitState();
	public abstract void CheckSwitchStates();
	public abstract void InitializeSubState();
	public abstract void OnAttackEvent();
	public abstract void OnCollisionEnter2D(Collision2D other);
	public void UpdateStates()
	{
		UpdateState();
		if (_currentSubState != null)
			_currentSubState.UpdateStates();
	}
	protected void SwitchState(EnemyBaseState newState)
	{
		ExitState();
		newState.EnterState();
		if (_isRootState)
			_context.currentState = newState;
		else if (_currentSuperState != null)
			_currentSuperState.SetSubState(newState);
	}
	protected void SetSuperState(EnemyBaseState newSuperState)
	{
		_currentSuperState = newSuperState;
	}
	protected void SetSubState(EnemyBaseState newSubState)
	{
		_currentSubState = newSubState;
		newSubState.SetSuperState(this);
		// newSubState.EnterState();
	}

}
