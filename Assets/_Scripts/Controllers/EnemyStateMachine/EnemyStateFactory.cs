using System;
using System.Collections.Generic;

public enum EnemyStates 
{
	walking,
	attacking,
	grabbed,
	flung,
	dead,
	aware,
	suspicious,
	scared,
	angry
}
public class EnemyStateFactory
{
	EnemyStateMachine _context;
	Dictionary<EnemyStates, EnemyBaseState> _states = new Dictionary<EnemyStates, EnemyBaseState>();

	public EnemyStateFactory(EnemyStateMachine currentContext)
	{
		_context = currentContext;
		_states[EnemyStates.walking] = new EnemyWalkingState(_context, this, EnemyStates.walking);
		_states[EnemyStates.attacking] = new EnemyAttackingState(_context, this, EnemyStates.attacking);
		_states[EnemyStates.grabbed] = new EnemyGrabbedState(_context, this, EnemyStates.grabbed);
		_states[EnemyStates.flung] = new EnemyFlyingState(_context, this, EnemyStates.flung);
		_states[EnemyStates.dead] = new EnemyDeadState(_context, this, EnemyStates.dead);
		_states[EnemyStates.aware] = new EnemyAwareState(_context, this, EnemyStates.aware);
		_states[EnemyStates.suspicious] = new EnemySuspiciousState(_context, this, EnemyStates.suspicious);
		// _states[EnemyStates.scared] = new EnemyWalkingState(_context, this);
		// _states[EnemyStates.angry] = new EnemyWalkingState(_context, this);
	}

	public EnemyBaseState Walk(){return _states[EnemyStates.walking];}
	public EnemyBaseState Attack(){return _states[EnemyStates.attacking];}
	public EnemyBaseState Grabbed(){return _states[EnemyStates.grabbed];}
	public EnemyBaseState Flung(){return _states[EnemyStates.flung];}
	public EnemyBaseState Aware(){return _states[EnemyStates.aware];}
	public EnemyBaseState Suspicious(){return _states[EnemyStates.suspicious];}
	public EnemyBaseState Dead(){return _states[EnemyStates.dead];}
}
