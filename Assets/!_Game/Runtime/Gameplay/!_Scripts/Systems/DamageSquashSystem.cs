using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateAfter(typeof(HitFeedbackDispatchSystem))]
public partial struct DamageSquashSystem : ISystem
{
    private ComponentLookup<PostTransformMatrix> _ptmLookup;

    public void OnCreate(ref SystemState state)
    {
        _ptmLookup = state.GetComponentLookup<PostTransformMatrix>();

        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<DamageSquashConfig>(),
            ComponentType.ReadOnly<DamageSquashState>(),
            ComponentType.ReadOnly<BodyVisualRef>()));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _ptmLookup.Update(ref state);

        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (config, stateRW, enabled, bodyRef) in SystemAPI.Query<RefRO<DamageSquashConfig>, RefRW<DamageSquashState>, EnabledRefRW<DamageSquashState>, RefRO<BodyVisualRef>>())
        {
            var body = bodyRef.ValueRO.Body;
            if (body == Entity.Null || !_ptmLookup.HasComponent(body)) continue;

            stateRW.ValueRW.Elapsed += dt;

            var elapsed = stateRW.ValueRO.Elapsed;
            var duration = config.ValueRO.Duration;

            if (elapsed >= duration)
            {
                _ptmLookup[body] = new PostTransformMatrix { Value = float4x4.identity };
                enabled.ValueRW = false;
                continue;
            }

            var t = math.saturate(elapsed / duration);
            var bell = math.sin(math.PI * t);

            var n = stateRW.ValueRO.HitDirLocal;
            var nLen = math.length(n);
            if (nLen < 1e-4f) { _ptmLookup[body] = new PostTransformMatrix { Value = float4x4.identity }; continue; }
            n /= nLen;

            var sAxis = 1f - config.ValueRO.MaxCompress * bell;
            var sPerp = 1f + config.ValueRO.MaxStretch * bell;

            // Anisotropic scale: stretch perpendicular by sPerp, compress along n by sAxis.
            // M = sPerp * I + (sAxis - sPerp) * (n n^T)
            var nx = n.x; var ny = n.y; var nz = n.z;
            var k = sAxis - sPerp;
            var m = new float3x3(
                sPerp + k * nx * nx, k * nx * ny,        k * nx * nz,
                k * ny * nx,        sPerp + k * ny * ny, k * ny * nz,
                k * nz * nx,        k * nz * ny,         sPerp + k * nz * nz
            );

            _ptmLookup[body] = new PostTransformMatrix { Value = new float4x4(m, float3.zero) };
        }
    }
}
