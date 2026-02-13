using GameManagement;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace GameEngine.AI
{
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    partial struct UnitMoverSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var unitMoverJob = new UnitMoverJob { DeltaTime = SystemAPI.Time.DeltaTime };
            unitMoverJob.ScheduleParallel();
        }
    }
    
    [BurstCompile]
    [WithDisabled(typeof(UnableToAct))]
    public partial struct UnitMoverJob : IJobEntity
    {
        public float DeltaTime;
        public void Execute(ref LocalTransform localTransform, in ActionState action, in UnitMover unitMover, in Destination destination, ref PhysicsVelocity physicsVelocity)
        {
            if (action.Value == ActionType.Attacking || action.Value == ActionType.Stunned) return;
            
            var moveDirection = destination.Value - localTransform.Position;
            if (math.lengthsq(moveDirection) <= destination.StoppingDistanceSq)
            {
                physicsVelocity.Linear = float3.zero;
                physicsVelocity.Angular = float3.zero;
                return;
            }
            moveDirection = math.normalize(moveDirection); // set transform to anything but 0 0 0, or you'll get NaN
                
            physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
            physicsVelocity.Angular = float3.zero;
            
            var targetRotation = quaternion.LookRotationSafe(
                new float3(moveDirection.x, 0f, moveDirection.z),
                math.up()
            );
            
            localTransform.Rotation = math.slerp(
                localTransform.Rotation, 
                targetRotation, 
                DeltaTime * unitMover.turnSpeed
            );
        }
    }
}