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
            foreach (var (unitMover, target) in SystemAPI.Query<RefRW<UnitMover>, RefRO<Target>>().WithDisabled<Grabbed>())
            {
                if (target.ValueRO.targetEntity == Entity.Null) continue;
                
                if (state.EntityManager.HasComponent<LocalTransform>(target.ValueRO.targetEntity))
                {
                    var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(target.ValueRO.targetEntity);
                    unitMover.ValueRW.targetPosition = targetTransform.Position;
                }
            }
        }
    }
}