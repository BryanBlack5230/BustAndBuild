using GameManagement;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct CastleBreachSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var castleLookup = SystemAPI.GetComponentLookup<Castle>(false);

        foreach (var (wallCleanup, entity) in
                 SystemAPI.Query<RefRO<WallCleanupTag>>()
                     .WithNone<WallSection>()
                     .WithEntityAccess())
        {
            var castleEnt = wallCleanup.ValueRO.CastleEntity;

            if (castleLookup.HasComponent(castleEnt))
            {
                var castle = castleLookup[castleEnt];
                castle.hasBeenBreached = true;
                ecb.SetComponent(castleEnt, castle);
            }

            ecb.RemoveComponent<WallCleanupTag>(entity);
        }
    }
}