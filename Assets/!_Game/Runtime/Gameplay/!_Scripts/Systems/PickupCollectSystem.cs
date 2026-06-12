using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial class PickupCollectSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PickupSettings>();
        RequireForUpdate<CursorWorldPosition>();
        RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    protected override void OnUpdate()
    {
        var cursor = SystemAPI.GetSingleton<CursorWorldPosition>();
        if (!cursor.IsValid) return;

        var settings = SystemAPI.GetSingleton<PickupSettings>();
        var radiusSq = settings.PickupRadius * settings.PickupRadius;
        var rayOrigin = cursor.RayOrigin;
        var rayDir = cursor.RayDirection;
        var invDirY = 1f / rayDir.y;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        foreach (var (pickup, transform, beingCollected, entity) in SystemAPI
            .Query<RefRO<Pickup>, RefRO<LocalTransform>, EnabledRefRW<PickupBeingCollected>>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
            .WithEntityAccess())
        {
            if (beingCollected.ValueRO) continue;

            var pos = transform.ValueRO.Position;
            var t = (pos.y - rayOrigin.y) * invDirY;
            var cursorAtPickupY = rayOrigin + rayDir * t;
            var distSq = math.distancesq(new float2(pos.x, pos.z), new float2(cursorAtPickupY.x, cursorAtPickupY.z));
            if (distSq > radiusSq) continue;

            beingCollected.ValueRW = true;
            EventBus.Raise(new PickupCollectedEvent(pickup.ValueRO.Type, pos, pickup.ValueRO.Value));
            ecb.DestroyEntity(entity);
        }
    }
}
