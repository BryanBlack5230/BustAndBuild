using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeadState : EnemyBaseState
{
    public EnemyDeadState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory){}
    public override void CheckSwitchStates()
    {
        if (!_context.IsDead)
            SetSubState(_factory.Walk());
    }

    public override void EnterState()
    {
        // if (_context.Pool != null)
        // _context.Pool.Release(_context);
        // else
        // {
        // Destroy(_context);
        // }
    }

    public override void ExitState()
    {
        _context.Health.Revive();
    }

    public override void InitializeSubState(){}

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
}
