using GameEngine.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FindTargetSystem))]
[UpdateBefore(typeof(UnitMoverSystem))]
public partial struct BattleBrainSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var brainJob = new BrainDecisionJob
        {
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            UnableToActLookup = SystemAPI.GetComponentLookup<UnableToAct>(true),
            CooldownLookup = SystemAPI.GetComponentLookup<AttackCooldownExpirationTimestamp>(true),
        };
        brainJob.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct BrainDecisionJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly] public ComponentLookup<UnableToAct> UnableToActLookup;
    [ReadOnly] public ComponentLookup<AttackCooldownExpirationTimestamp> CooldownLookup;

    private void Execute(
        Entity entity,
        ref BattleBrain brain,
        ref ActionState action,
        ref Destination destination,
        ref UnitMover unitMover,
        in Target target,
        in EmotionalState emotion,
        in AttackData attackData,
        in LocalTransform transform,
        in BattleUnitBase unitBase)
    {
        if (UnableToActLookup.IsComponentEnabled(entity))
        {
            action.Value = ActionType.Stunned;
            brain.CanAttack = false;
            return;
        }
        
        if (emotion.Value == Emotion.Scared)
        {
            action.Value = ActionType.Moving;
            // unitMover.moveSpeed = config.moveSpeed * config.scaredMoveMultiplier; // this should be handled in emotion system at switch time
            destination.Value = unitBase.position;
            destination.StoppingDistanceSq = 0f;
            brain.CanAttack = false;
        }

        if (target.TargetEntity == Entity.Null || !TransformLookup.HasComponent(target.TargetEntity))
        {
            action.Value = ActionType.Moving; 
            destination.Value = unitBase.position;
            destination.StoppingDistanceSq = 0f;
            brain.CanAttack = false;
            return;
        }
        
        var myPos = transform.Position;
        var targetPos = TransformLookup[target.TargetEntity].Position;
        var distToTargetSq = math.distancesq(myPos, targetPos);
        var attackRangeSq = attackData.AttackRange * attackData.AttackRange;

        action.Value = CooldownLookup.IsComponentEnabled(entity) ? ActionType.Evading : ActionType.Attacking;

        if (action.Value == ActionType.Attacking)
        {
            if (distToTargetSq <= attackRangeSq)
            {
                action.Value = ActionType.Attacking;
                destination.Value = myPos; // Stop moving
                destination.StoppingDistanceSq = attackRangeSq;
                brain.CanAttack = true;
            }
            else
            {
                action.Value = ActionType.Moving;
                destination.Value = targetPos;
                destination.StoppingDistanceSq = 1f;
                brain.CanAttack = false;
            }
        }
        else
        {
            action.Value = ActionType.Moving;
            destination.Value = targetPos; // need new logic here for running around the target
            destination.StoppingDistanceSq = 1f;
            brain.CanAttack = false;
        }
    }
}