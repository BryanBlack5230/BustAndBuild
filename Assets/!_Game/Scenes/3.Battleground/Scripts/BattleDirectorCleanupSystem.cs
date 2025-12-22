using GameManagement;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateBefore(typeof(FindTargetSystem))]
[BurstCompile]
public partial struct BattleDirectorCleanupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleDirector>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Debug.Log("Cleanup OnUpdate");
        var directorEntity = SystemAPI.GetSingletonEntity<BattleDirector>();
        var isDirty = false;

        isDirty |= CleanupBuffer<EnemyUnitReference>(ref state, directorEntity);
        isDirty |= CleanupBuffer<AllyUnitReference>(ref state, directorEntity);

        if (!isDirty) return;
        
        var director = SystemAPI.GetComponent<BattleDirector>(directorEntity);
        director.IsDirty = true;
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