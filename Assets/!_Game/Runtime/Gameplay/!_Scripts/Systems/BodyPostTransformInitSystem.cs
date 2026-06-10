using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BodyPostTransformInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate(state.GetEntityQuery(
            new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<BodyVisualTag>() },
                None = new[] { ComponentType.ReadOnly<PostTransformMatrix>() },
            }));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<BodyVisualTag>>()
                     .WithNone<PostTransformMatrix>()
                     .WithEntityAccess())
        {
            ecb.AddComponent(entity, new PostTransformMatrix { Value = float4x4.identity });
        }
    }
}
