using GameEngine.AI;
using GameManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateAfter(typeof(TargetSearchSystem))]
[UpdateBefore(typeof(UnitMoverSystem))]
public partial struct BattleBrainSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleCoordinator>();
        state.RequireForUpdate<FactionBases>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var bases = SystemAPI.GetSingletonRW<FactionBases>().ValueRO;
        if (!bases.IsInitialized) return;
        
        var brainJob = new BrainDecisionJob
        {
            Bases = bases,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            UnableToActLookup = SystemAPI.GetComponentLookup<UnableToAct>(true),
            CooldownLookup = SystemAPI.GetComponentLookup<AttackCooldownExpirationTimestamp>(true),
        };
        state.Dependency = brainJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct BrainDecisionJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly] public ComponentLookup<UnableToAct> UnableToActLookup;
    [ReadOnly] public ComponentLookup<AttackCooldownExpirationTimestamp> CooldownLookup;
    [ReadOnly] public FactionBases Bases;

    private void Execute(
        Entity entity,
        ref BattleBrain brain,
        ref ActionState action,
        ref Destination destination,
        EnabledRefRW<SteeringEnabled> steerEnabled,
        in Target target,
        in EmotionalState emotion,
        in AttackData attackData,
        in LocalTransform transform,
        in Unit unit)
    {
        if (UnableToActLookup.IsComponentEnabled(entity))
        {
            action.Value = ActionType.Stunned;
            brain.CanAttack = false;
            steerEnabled.ValueRW = false;
            return;
        }
        
        var myPos = transform.Position;
        steerEnabled.ValueRW = true;
        
        if (emotion.Value == Emotion.Scared)
        {
            var baseBounds = unit.faction == Faction.Ally ? Bases.AllyBaseBounds : Bases.EnemyBaseBounds;
            
            action.Value = ActionType.Moving;
            // unitMover.moveSpeed = config.moveSpeed * config.scaredMoveMultiplier; // this should be handled in emotion system at switch time
            destination.Value = baseBounds.ClosestPoint(myPos);
            destination.StoppingDistanceSq = 0f;
            brain.CanAttack = false;
            return;
        }

        if (target.TargetEntity == Entity.Null || !TransformLookup.HasComponent(target.TargetEntity))
        {
            var baseBounds = unit.faction == Faction.Ally ? Bases.AllyBaseBounds : Bases.EnemyBaseBounds;
            
            action.Value = ActionType.Moving; 
            destination.Value = baseBounds.ClosestPoint(myPos);
            destination.StoppingDistanceSq = 0f;
            brain.CanAttack = false;
            return;
        }
        
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
                steerEnabled.ValueRW = false;
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
            action.Value = ActionType.Evading;
            var dirAway = math.normalize(myPos - targetPos);
            destination.Value = myPos + (dirAway * 3.0f);
            destination.StoppingDistanceSq = 0.5f;
            brain.CanAttack = false;
        }
    }
}