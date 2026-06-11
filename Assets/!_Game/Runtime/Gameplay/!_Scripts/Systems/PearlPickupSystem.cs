using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial class PearlPickupSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PearlSettings>();
        RequireForUpdate<CursorWorldPosition>();
        RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    protected override void OnUpdate()
    {
        var cursor = SystemAPI.GetSingleton<CursorWorldPosition>();
        if (!cursor.IsValid) return;

        var settings = SystemAPI.GetSingleton<PearlSettings>();
        var radiusSq = settings.PickupRadius * settings.PickupRadius;
        var rayOrigin = cursor.RayOrigin;
        var rayDir = cursor.RayDirection;
        var invDirY = 1f / rayDir.y;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        foreach (var (pearl, transform, beingCollected, entity) in SystemAPI
            .Query<RefRO<Pearl>, RefRO<LocalTransform>, EnabledRefRW<PearlBeingCollected>>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
            .WithEntityAccess())
        {
            if (beingCollected.ValueRO) continue;

            var pos = transform.ValueRO.Position;
            var t = (pos.y - rayOrigin.y) * invDirY;
            var cursorAtPearlY = rayOrigin + rayDir * t;
            var distSq = math.distancesq(new float2(pos.x, pos.z), new float2(cursorAtPearlY.x, cursorAtPearlY.z));
            if (distSq > radiusSq) continue;

            beingCollected.ValueRW = true;
            EventBus.Raise(new PearlPickedUpEvent(pos, pearl.ValueRO.Value));
            ecb.DestroyEntity(entity);
        }
    }
}
