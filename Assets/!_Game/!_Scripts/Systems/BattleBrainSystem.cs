using GameEngine.AI;
using GameEngine.Utils.Logging;
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
            LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
            UnableToActLookup = SystemAPI.GetComponentLookup<UnableToAct>(true),
            CooldownLookup = SystemAPI.GetComponentLookup<AttackCooldownExpirationTimestamp>(true),
        };
        state.Dependency = brainJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithPresent(typeof(SteeringEnabled))]
public partial struct BrainDecisionJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    [ReadOnly] public ComponentLookup<UnableToAct> UnableToActLookup;
    [ReadOnly] public ComponentLookup<AttackCooldownExpirationTimestamp> CooldownLookup;
    [ReadOnly] public FactionBases Bases;

    
    private void Execute(
        Entity entity,
        ref BattleBrain brain,
        ref ActionState action,
        ref Destination destination,
        ref FinalDestination finalDestination,
        EnabledRefRW<SteeringEnabled> steerEnabled,
        in Target target,
        in EmotionalState emotion,
        in AttackData attackData,
        in LocalToWorld worldTransform,
        in Unit unit)
    {
        if (UnableToActLookup.IsComponentEnabled(entity))
        {
            action.Value = ActionType.Stunned;
            brain.CanAttack = false;
            steerEnabled.ValueRW = false;
            return;
        }
        
        var myWorldPos = worldTransform.Position;
        steerEnabled.ValueRW = true;
        
        if (emotion.Value == Emotion.Scared)
        {
            var baseBounds = unit.faction == Faction.Ally ? Bases.AllyBaseBounds : Bases.EnemyBaseBounds;
            
            action.Value = ActionType.Moving;
            // unitMover.moveSpeed = config.moveSpeed * config.scaredMoveMultiplier; // this should be handled in emotion system at switch time
            finalDestination.Value = baseBounds.ClosestPoint(myWorldPos);
            brain.CanAttack = false;
            return;
        }

        if (target.TargetEntity == Entity.Null || !LocalToWorldLookup.HasComponent(target.TargetEntity))
        {
            var baseBounds = unit.faction == Faction.Ally ? Bases.AllyBaseBounds : Bases.EnemyBaseBounds;
            
            action.Value = ActionType.Moving; 
            finalDestination.Value = baseBounds.ClosestPoint(myWorldPos);
            brain.CanAttack = false;
            return;
        }
        
        var targetWorldPos = LocalToWorldLookup[target.TargetEntity].Position;
        
        var distToTargetSq = math.distancesq(myWorldPos, targetWorldPos);
        var attackRangeSq = attackData.AttackRange * attackData.AttackRange;

        action.Value = CooldownLookup.IsComponentEnabled(entity) ? ActionType.Evading : ActionType.Attacking;
        
        if (action.Value == ActionType.Attacking)
        {
            if (distToTargetSq <= attackRangeSq)
            {
                action.Value = ActionType.Attacking;
                finalDestination.Value = myWorldPos; // Stop moving
                steerEnabled.ValueRW = false;
                brain.CanAttack = true;
            }
            else
            {
                action.Value = ActionType.Moving;
                finalDestination.Value = targetWorldPos;
                brain.CanAttack = false;
            }
        }
        else
        {
            if (distToTargetSq <= attackRangeSq * 32) //TODO this is a quick-fix, need a proper evasion system
            {
                action.Value = ActionType.Evading;
                var dirAway = math.normalize(myWorldPos - targetWorldPos);
                finalDestination.Value = myWorldPos + (dirAway * 3.0f);
                brain.CanAttack = false;
            }
            else
            {
                action.Value = ActionType.Moving;
                finalDestination.Value = targetWorldPos;
                brain.CanAttack = false;
            }
            
        }
    }
}