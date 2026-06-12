using BarkingBird.Runtime.Infrastructure;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct HitFeedbackDispatchSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<HitFeedbackBufferElement>()));
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (buffer, transform, entity) in SystemAPI
                     .Query<DynamicBuffer<HitFeedbackBufferElement>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            if (buffer.Length == 0) continue;

            var dirSum = float3.zero;
            for (var i = 0; i < buffer.Length; i++) dirSum += buffer[i].HitDirection;
            var dirWorld = math.normalizesafe(dirSum, new float3(0f, 0f, 1f));
            // Log.Battle.D(entity, $"{entity} has hits in buffer, hit direction [{dirWorld}]");

            buffer.Clear();

            if (SystemAPI.HasComponent<DamageFlashConfig>(entity))
            {
                SystemAPI.SetComponent(entity, new DamageFlashState { Phase = 0, Elapsed = 0f });
                SystemAPI.SetComponentEnabled<DamageFlashState>(entity, true);
                // Log.Battle.D(entity, $"{entity} has flash config, enable flash");
            }

            if (SystemAPI.HasComponent<DamageSquashConfig>(entity))
            {
                var inv = math.inverse(transform.ValueRO.Rotation);
                var dirLocal = math.normalizesafe(math.mul(inv, dirWorld), new float3(0f, 0f, 1f));
                SystemAPI.SetComponent(entity, new DamageSquashState
                {
                    HitDirLocal = dirLocal,
                    Elapsed = 0f,
                });
                SystemAPI.SetComponentEnabled<DamageSquashState>(entity, true);
                // Log.Battle.D(entity, $"{entity} has squash config, enable squash");
            }

            if (SystemAPI.HasComponent<DamagePushConfig>(entity))
            {
                SystemAPI.SetComponent(entity, new DamagePushState { HitDirWorld = dirWorld });
                SystemAPI.SetComponentEnabled<DamagePushState>(entity, true);
                // Log.Battle.D(entity, $"{entity} has push config, enable push");
            }
        }
    }
}
