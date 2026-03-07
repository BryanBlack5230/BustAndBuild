using GameEngine.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SteeringSystemGroup))]
public partial struct Steer_SeekSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SteeringContext>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (context, pathTarget, seekConfig, transform) in SystemAPI.Query<RefRW<SteeringContext>, RefRO<PathTarget>, RefRO<SteerBehavior_Seek>, RefRO<LocalTransform>>())
        {
            var dirToGoal = pathTarget.ValueRO.Value - transform.ValueRO.Position;
            var distSq = math.lengthsq(dirToGoal);

            if (distSq < 0.01f) continue;

            var normalizedDir = math.normalize(dirToGoal);

            for (var i = 0; i < 8; i++)
            {
                var alignment = math.dot(normalizedDir, SteeringConstants.Directions[i]);
                var value = (alignment + 1.0f) * 0.5f;
                context.ValueRW.Interest[i] += value * seekConfig.ValueRO.Weight;
            }
        }
    }
    //
    // [BurstCompile]
    // public partial struct SeekJob : IJobEntity
    // {
    //     [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    //
    //     private void Execute(
    //         ref SteeringContext context, 
    //         in Target target, 
    //         in SteerBehavior_Seek config, 
    //         in LocalTransform myTransform)
    //     {
    //         if (target.TargetEntity == Entity.Null) return;
    //         if (!TransformLookup.HasComponent(target.TargetEntity)) return;
    //
    //         var targetPos = TransformLookup[target.TargetEntity].Position;
    //         var myPos = myTransform.Position;
    //         
    //         var vectorToTarget = targetPos - myPos;
    //         var distSq = math.lengthsq(vectorToTarget);
    //
    //         if (distSq < context.AgentRadius) return;
    //
    //         var dirToTarget = math.normalize(vectorToTarget);
    //
    //         for (var i = 0; i < 8; i++)
    //         {
    //             var fixedDir = SteeringConstants.Directions[i];
    //             
    //             var alignment = math.dot(dirToTarget, fixedDir);
    //             if (alignment > 0)
    //             {
    //                 context.Interest[i] += alignment * config.Weight;
    //             }
    //         }
    //     }
    // }
}