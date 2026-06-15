using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateAfter(typeof(BattleBrainSystem))]
    [UpdateBefore(typeof(UnitMoverSystem))]
    public partial class SteeringSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SteeringSystemGroup), OrderLast = true)]
    public partial struct Steer_ResolveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SteeringContext>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.TryGetSingleton<SteeringConfig>(out var steeringConfig) ? steeringConfig : SteeringConfig.Default;

            var job = new ResolveJob
            {
                DangerMultiplier = config.DangerMultiplier,
                LookAheadDistance = config.LookAheadDistance,
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ResolveJob : IJobEntity
        {
            public float DangerMultiplier;
            public float LookAheadDistance;

            private void Execute(
                ref Destination destination, 
                ref SteeringContext context, 
                in LocalTransform transform,
                EnabledRefRO<SteeringEnabled> steerEnabled)
            {
                if (!steerEnabled.ValueRO) return;
            
                Span<float> scores = stackalloc float[8]; 
            
                var bestScore = 0.001f;
                var bestIndex = -1;
                var finalDir = float3.zero;

                for (var i = 0; i < 8; i++)
                {
                    var interest = context.Interest[i];
                    var danger = context.Danger[i];

                    var score = interest - (danger * DangerMultiplier);
                
                    score = math.max(0f, score);
                    scores[i] = score;
                    finalDir += score * SteeringConstants.GetDirection(i);
                
                    // Log.Battle.D($"{SteeringConstants.DirectionsText[i]}: interest {interest}, danger {danger}, score {score}");

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }
            
                // Log.Battle.D($"Best with score {SteeringConstants.DirectionsText[bestIndex]}: {bestScore}");

                if (bestIndex == -1)
                {
                    // destination.StoppingDistanceSq = 0.1f;
                    destination.Value = transform.Position;
                    context.BestDirection = float3.zero;
                }
                else
                {
                    finalDir = math.lengthsq(finalDir) > 0.001f ? math.normalize(finalDir) : SteeringConstants.GetDirection(bestIndex);

                    // destination.StoppingDistanceSq = context.AgentRadius + 0.5f;
                    destination.Value = transform.Position + (finalDir * LookAheadDistance);
                    context.BestDirection = finalDir;
                }
            }
        }
    }
}