using Unity.Burst;
using Unity.Entities;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Infrastructure.GameLoop;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateBefore(typeof(TargetSearchSystem))]
[BurstCompile]
public partial struct BattleDirectorCleanupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleCoordinator>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var directorEntity = SystemAPI.GetSingletonEntity<BattleCoordinator>();
        var isDirty = false;

        isDirty |= CleanupBuffer<EnemyUnitReference>(ref state, directorEntity);
        isDirty |= CleanupBuffer<AllyUnitReference>(ref state, directorEntity);

        if (!isDirty) return;
    
        var director = SystemAPI.GetComponent<BattleCoordinator>(directorEntity);
        director.ForceGlobalReevaluation = true;
        state.EntityManager.SetComponentData(directorEntity, director);
    }

    private bool CleanupBuffer<T>(ref SystemState state, Entity director) where T : unmanaged, IBufferElementData
    {
        var buffer = state.EntityManager.GetBuffer<T>(director);
        var entities = buffer.Reinterpret<Entity>();
        var changed = false;

        for (int i = entities.Length - 1; i >= 0; i--)
        {
            if (state.EntityManager.Exists(entities[i])) continue;
        
            // Debug.Log($"Cleanup buffer of type {typeof(T).Name}, entity {i} doest not exist anymore");
            buffer.RemoveAt(i);
            changed = true;
        }
    
        // Debug.Log($"Cleanup buffer of type {typeof(T).Name} is dirty: {changed}");
        return changed;
    }
}