using Unity.Burst;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateAfter(typeof(SpawningSystem))]
    [BurstCompile]
    public partial struct BattleUnitRegistrationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleCoordinator>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var directorEntity = SystemAPI.GetSingletonEntity<BattleCoordinator>();

            var enemies = state.EntityManager.GetBuffer<EnemyUnitReference>(directorEntity);
            var allies = state.EntityManager.GetBuffer<AllyUnitReference>(directorEntity);
            // Debug.Log($"UnitRegistrationSystem OnUpdate: enemies [{enemies.Length}] allies [{allies.Length}]");

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var listChanged = false;

            foreach (var (unit, entity) in SystemAPI.Query<RefRO<Unit>>()
                         .WithNone<UnitRegisteredTag>()
                         .WithEntityAccess())
            {
                // Debug.Log($"Working on entity {entity}, faction {unit.ValueRO.faction}");
                if (unit.ValueRO.faction == Faction.Enemy)
                {
                    enemies.Add(new EnemyUnitReference { Value = entity });
                    listChanged = true;
                }
                else if (unit.ValueRO.faction == Faction.Ally)
                {
                    allies.Add(new AllyUnitReference { Value = entity });
                    listChanged = true;
                }
                ecb.AddComponent<UnitRegisteredTag>(entity);
            }

            // Debug.Log($"UnitRegistrationSystem OnUpdate: listChanged [{listChanged}], enemies [{enemies.Length}], allies [{allies.Length}]");
            if (!listChanged || enemies.Length == 0 || allies.Length == 0) return;
        
            var director = SystemAPI.GetComponent<BattleCoordinator>(directorEntity);
            director.ForceGlobalReevaluation = true;
            state.EntityManager.SetComponentData(directorEntity, director);
        }
    }
}