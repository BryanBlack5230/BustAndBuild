using Unity.Burst;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateBefore(typeof(AbleToActEvaluationSystem))]
    public partial struct StunSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (stun, stunEnabled) in
                     SystemAPI.Query<RefRW<Stun>, EnabledRefRW<Stun>>())
            {
                stun.ValueRW.Remaining -= dt;
                if (stun.ValueRO.Remaining <= 0f)
                    stunEnabled.ValueRW = false;
            }
        }
    }
}
