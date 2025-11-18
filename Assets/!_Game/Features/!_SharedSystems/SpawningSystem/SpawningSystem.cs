using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public partial struct SpawningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var em  = state.EntityManager;
        var dt = SystemAPI.Time.DeltaTime;

        // --- SpawnByPoint ---
        foreach (var (spawnSettings, point) in SystemAPI.Query<RefRW<SpawnSettings>, RefRO<SpawnByPoint>>())
        {
            // Debug.Log("Huh, point");
            // you can convert it to a method and it will work, but Burst will crap errors from time to time because it wants it to be inlined
            spawnSettings.ValueRW.timer -= dt;
            if (spawnSettings.ValueRO.timer > 0f) continue;
            spawnSettings.ValueRW.timer = spawnSettings.ValueRO.spawnInterval;
            
            var prefab = spawnSettings.ValueRO.prefab;
            var spawnPos = point.ValueRO.position;
            Spawn(prefab, spawnPos);
        }

        // --- SpawnByArea ---
        foreach (var (spawnSettings, area) in SystemAPI.Query<RefRW<SpawnSettings>, RefRO<SpawnByArea>>())
        {
            // Debug.Log("Huh, area");
            spawnSettings.ValueRW.timer -= dt;
            if (spawnSettings.ValueRO.timer > 0f) continue;
            spawnSettings.ValueRW.timer = spawnSettings.ValueRO.spawnInterval;

            var rand = Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime * 10007);
            var prefab = spawnSettings.ValueRO.prefab;
            var spawnPos = PhysicsUtility.GetRandomPointInsideCollider(em, area.ValueRO.areaCollider,  ref rand);
            Spawn(prefab, spawnPos);
        }

        // --- SpawnByRadius ---
        foreach (var (spawnSettings, radius) in SystemAPI.Query<RefRW<SpawnSettings>, RefRO<SpawnByRadius>>())
        {
            // Debug.Log("Huh, radius");
            spawnSettings.ValueRW.timer -= dt;
            if (spawnSettings.ValueRO.timer > 0f) continue;
            spawnSettings.ValueRW.timer = spawnSettings.ValueRO.spawnInterval;

            var rand = Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime * 10007);
            var prefab = spawnSettings.ValueRO.prefab;
            var spawnPos = radius.ValueRO.center + rand.NextFloat3Direction() * rand.NextFloat(0f, radius.ValueRO.radius);
            Spawn(prefab, spawnPos);
        }

        // --- SpawnAttached (spawn relative to another entity's LocalTransform) ---
        foreach (var (spawnSettings, attached) in SystemAPI.Query<RefRW<SpawnSettings>, RefRO<SpawnAttached>>())
        {
            // Debug.Log("Huh, attached");
            spawnSettings.ValueRW.timer -= dt;
            if (spawnSettings.ValueRO.timer > 0f) continue;
            spawnSettings.ValueRW.timer = spawnSettings.ValueRO.spawnInterval;

            var spawnPos = attached.ValueRO.offset;
            var prefab = spawnSettings.ValueRO.prefab;
            if (em.Exists(attached.ValueRO.targetEntity) && em.HasComponent<LocalTransform>(attached.ValueRO.targetEntity))
            {
                var t = em.GetComponentData<LocalTransform>(attached.ValueRO.targetEntity);
                spawnPos = t.Position + attached.ValueRO.offset;
            }
            
            Spawn(prefab, spawnPos);
        }

        ecb.Playback(em);
        ecb.Dispose();
        
        // ─────────────────────────────────────────────────────────
        // LOCAL BURST-INLINEABLE METHODS
        // ─────────────────────────────────────────────────────────
        void Spawn(Entity prefab, float3 pos)
        {
            var e = ecb.Instantiate(prefab);
            ecb.SetComponent(e, LocalTransform.FromPosition(pos));
        }
        // ─────────────────────────────────────────────────────────
    }
}
