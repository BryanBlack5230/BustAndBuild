using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace GameEngine.AI
{
    partial struct UnitMoverSystem : ISystem
    {
        private float _reachedTargetDistanceSq;
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _reachedTargetDistanceSq = 2f;
            
            RunInThreads(ref state);
            // RunOnMainThread(ref state);
        }

        private void RunInThreads(ref SystemState state)
        {
            var unitMoverJob = new UnitMoverJob
            {
                reachedTargetDistanceSq = _reachedTargetDistanceSq,
            };
            // unitMoverJob.Run(); // runs on main thread, use for testing
            unitMoverJob.ScheduleParallel();
            
        }

        private void RunOnMainThread(ref SystemState state)
        {
            foreach (var (localTransform, unitMover, physicsVelocity)
                     in SystemAPI.Query<
                         RefRW<LocalTransform>,
                         RefRO<UnitMover>,
                         RefRW<PhysicsVelocity>>())
            {
                var moveDirection = unitMover.ValueRO.targetPosition - localTransform.ValueRO.Position;
                if (math.lengthsq(moveDirection) < _reachedTargetDistanceSq)
                {
                    physicsVelocity.ValueRW.Linear = float3.zero;
                    physicsVelocity.ValueRW.Angular = float3.zero;
                    return;
                }
                
                moveDirection = math.normalize(moveDirection); // set transform to anything but 0 0 0, or you'll get NaN
                
                physicsVelocity.ValueRW.Linear = moveDirection * unitMover.ValueRO.moveSpeed;
                physicsVelocity.ValueRW.Angular = float3.zero;
            }
        }
    }
    
    [BurstCompile]
    public partial struct UnitMoverJob : IJobEntity
    {
        public float reachedTargetDistanceSq;
        public void Execute(ref LocalTransform localTransform, in UnitMover unitMover, ref PhysicsVelocity physicsVelocity)
        {
            var moveDirection = unitMover.targetPosition - localTransform.Position;
            if (math.lengthsq(moveDirection) < reachedTargetDistanceSq)
            {
                physicsVelocity.Linear = float3.zero;
                physicsVelocity.Angular = float3.zero;
                return;
            }
            moveDirection = math.normalize(moveDirection); // set transform to anything but 0 0 0, or you'll get NaN
                
            physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
            physicsVelocity.Angular = float3.zero;
        }
    }
}