using Unity.Burst;
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
        foreach (var (context, pathTarget, seekConfig, worldTransform) in SystemAPI.Query<RefRW<SteeringContext>, RefRO<PathTarget>, RefRO<SteerBehavior_Seek>, RefRO<LocalToWorld>>())
        {
            var dirToGoal = pathTarget.ValueRO.Value - worldTransform.ValueRO.Position;
            var distSq = math.lengthsq(dirToGoal);

            if (distSq < 0.01f) continue;

            var normalizedDir = math.normalize(dirToGoal);

            for (var i = 0; i < 8; i++)
            {
                var alignment = math.dot(normalizedDir, SteeringConstants.GetDirection(i));
                var value = (alignment + 1.0f) * 0.5f;
                context.ValueRW.Interest[i] += value * seekConfig.ValueRO.Weight;
            }
        }
    }
}