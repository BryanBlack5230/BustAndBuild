using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct WallSectionInitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (wallSection, entity) in
                 SystemAPI.Query<RefRO<WallSection>>()
                     .WithNone<WallCleanupTag>()
                     .WithEntityAccess())
        {
            ecb.AddComponent(entity, new WallCleanupTag { CastleEntity = wallSection.ValueRO.CastleEntity });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
