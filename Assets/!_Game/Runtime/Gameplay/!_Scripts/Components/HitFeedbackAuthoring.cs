using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;

public class HitFeedbackAuthoring : MonoBehaviour
{
    [SerializeField] private FlashProfileSO flashProfileSo;
    [SerializeField] private SquashProfileSO squashProfileSo;
    [SerializeField] private PushProfileSO pushProfileSo;

    public class Baker : Baker<HitFeedbackAuthoring>
    {
        public override void Bake(HitFeedbackAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var body = authoring.GetComponentInChildren<BodyVisualAuthoring>(includeInactive: true);
            var bodyEntity = body != null
                ? GetEntity(body.gameObject, TransformUsageFlags.Dynamic)
                : Entity.Null;
            AddComponent(entity, new BodyVisualRef { Body = bodyEntity });

            if (authoring.flashProfileSo != null && bodyEntity != Entity.Null)
            {
                DependsOn(authoring.flashProfileSo);
                AddComponent(entity, new DamageFlashConfig
                {
                    WhiteColor = (Vector4)authoring.flashProfileSo.WhiteColor,
                    RedColor = (Vector4)authoring.flashProfileSo.RedColor,
                    ToWhiteDuration = authoring.flashProfileSo.ToWhiteDuration,
                    ToRedDuration = authoring.flashProfileSo.ToRedDuration,
                    ToNormalDuration = authoring.flashProfileSo.ToNormalDuration,
                });
                AddComponent(entity, new DamageFlashState());
                SetComponentEnabled<DamageFlashState>(entity, false);
            }

            if (authoring.squashProfileSo != null && bodyEntity != Entity.Null)
            {
                DependsOn(authoring.squashProfileSo);
                AddComponent(entity, new DamageSquashConfig
                {
                    Duration = authoring.squashProfileSo.Duration,
                    MaxCompress = authoring.squashProfileSo.MaxCompress,
                    MaxStretch = authoring.squashProfileSo.MaxStretch,
                });
                AddComponent(entity, new DamageSquashState());
                SetComponentEnabled<DamageSquashState>(entity, false);
            }

            if (authoring.pushProfileSo != null)
            {
                DependsOn(authoring.pushProfileSo);
                AddComponent(entity, new DamagePushConfig
                {
                    HorizontalImpulse = authoring.pushProfileSo.HorizontalImpulse,
                    UpKick = authoring.pushProfileSo.UpKick,
                    StunDuration = authoring.pushProfileSo.StunDuration,
                });
                AddComponent(entity, new DamagePushState());
                SetComponentEnabled<DamagePushState>(entity, false);
                AddComponent(entity, new Stun());
                SetComponentEnabled<Stun>(entity, false);
            }
        }
    }
}

public struct DamageFlashConfig : IComponentData
{
    public float4 WhiteColor;
    public float4 RedColor;
    public float ToWhiteDuration;
    public float ToRedDuration;
    public float ToNormalDuration;
}

public struct DamageFlashState : IComponentData, IEnableableComponent
{
    // 0 = to white, 1 = to red, 2 = to normal
    public byte Phase;
    public float Elapsed;
}

public struct DamageSquashConfig : IComponentData
{
    public float Duration;
    public float MaxCompress;
    public float MaxStretch;
}

public struct DamageSquashState : IComponentData, IEnableableComponent
{
    // Local-space pushback direction (in body's space).
    public float3 HitDirLocal;
    public float Elapsed;
}

public struct DamagePushConfig : IComponentData
{
    public float HorizontalImpulse;
    public float UpKick;
    public float StunDuration;
}

public struct DamagePushState : IComponentData, IEnableableComponent
{
    // World-space pushback direction. Set by dispatch, consumed same-tick by DamagePushSystem.
    public float3 HitDirWorld;
}
