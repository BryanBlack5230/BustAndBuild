using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using BarkingBird.Runtime.Infrastructure.GameLoop;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateAfter(typeof(HitFeedbackDispatchSystem))]
public partial struct DamageFlashSystem : ISystem
{
    private ComponentLookup<URPMaterialPropertyBaseColor> _colorLookup;
    private ComponentLookup<BodyOriginalColor> _originalLookup;

    public void OnCreate(ref SystemState state)
    {
        _colorLookup = state.GetComponentLookup<URPMaterialPropertyBaseColor>();
        _originalLookup = state.GetComponentLookup<BodyOriginalColor>(true);

        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<DamageFlashConfig>(),
            ComponentType.ReadOnly<DamageFlashState>(),
            ComponentType.ReadOnly<BodyVisualRef>()));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _colorLookup.Update(ref state);
        _originalLookup.Update(ref state);

        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (config, stateRW, enabled, bodyRef) in SystemAPI
                     .Query<RefRO<DamageFlashConfig>, RefRW<DamageFlashState>, EnabledRefRW<DamageFlashState>, RefRO<BodyVisualRef>>())
        {
            var body = bodyRef.ValueRO.Body;
            if (body == Entity.Null || !_colorLookup.HasComponent(body)) continue;

            var original = _originalLookup.HasComponent(body)
                ? _originalLookup[body].Value
                : new float4(1f, 1f, 1f, 1f);

            stateRW.ValueRW.Elapsed += dt;

            var phase = stateRW.ValueRO.Phase;
            var elapsed = stateRW.ValueRO.Elapsed;

            float4 from, to;
            float duration;

            switch (phase)
            {
                case 0:
                    from = original;
                    to = config.ValueRO.WhiteColor;
                    duration = config.ValueRO.ToWhiteDuration;
                    break;
                case 1:
                    from = config.ValueRO.WhiteColor;
                    to = config.ValueRO.RedColor;
                    duration = config.ValueRO.ToRedDuration;
                    break;
                default:
                    from = config.ValueRO.RedColor;
                    to = original;
                    duration = config.ValueRO.ToNormalDuration;
                    break;
            }

            if (elapsed >= duration)
            {
                if (phase < 2)
                {
                    stateRW.ValueRW.Phase = (byte)(phase + 1);
                    stateRW.ValueRW.Elapsed = 0f;
                    _colorLookup[body] = new URPMaterialPropertyBaseColor { Value = to };
                    continue;
                }

                _colorLookup[body] = new URPMaterialPropertyBaseColor { Value = original };
                enabled.ValueRW = false;
                continue;
            }

            var t = math.saturate(elapsed / duration);
            _colorLookup[body] = new URPMaterialPropertyBaseColor { Value = math.lerp(from, to, t) };
        }
    }
}
