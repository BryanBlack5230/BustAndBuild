using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace GameEngine.AI
{
    partial struct SetMoveTargetPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (unitMover, target) in SystemAPI.Query<RefRW<UnitMover>, RefRO<Target>>().WithDisabled<UnableToAct>())
            {
                continue;
                if (target.ValueRO.TargetEntity == Entity.Null) continue;
                
                if (state.EntityManager.HasComponent<LocalTransform>(target.ValueRO.TargetEntity))
                {
                    var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(target.ValueRO.TargetEntity);
                    // unitMover.ValueRW.targetPosition = targetTransform.Position;
                }
            }
        }
    }
}