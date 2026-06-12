using Unity.Entities;
using Unity.Physics;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Currency;

public class PickupAuthoring : MonoBehaviour
{
    [SerializeField, Tooltip("Which resource this pickup grants when collected. Spawning overrides this from the enemy's drop table, but it's the default for directly-placed pickups.")]
    private CurrencyType resourceType = CurrencyType.Pearls;
    [SerializeField, Tooltip("Wallet amount granted per pickup. Spawning overrides this from the drop table row.")]
    private float value = 1f;

    public class Baker : Baker<PickupAuthoring>
    {
        public override void Bake(PickupAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Pickup { Type = authoring.resourceType, Value = authoring.value });
            AddComponent(entity, new PickupLifetime
            {
                TimeRemaining = 0f,
                NextBlinkToggleAt = 0f,
                VisibleState = 1,
                BaseScale = 1f,
            });
            AddComponent(entity, new PickupFloat
            {
                Amplitude = 0.1f,
                Period = 2.5f,
                PhaseOffset = 0f,
                RestY = 0f,
                RestTimer = 0f,
            });
            AddComponent(entity, new PhysicsGravityFactor { Value = 1f });
            AddComponent(entity, new PickupBlinking());
            AddComponent(entity, new PickupBeingCollected());
            AddComponent(entity, new PickupSettled());

            SetComponentEnabled<PickupBlinking>(entity, false);
            SetComponentEnabled<PickupBeingCollected>(entity, false);
            SetComponentEnabled<PickupSettled>(entity, false);
        }
    }
}

public struct Pickup : IComponentData
{
    public CurrencyType Type;
    public float Value;
}

public struct PickupLifetime : IComponentData
{
    public float TimeRemaining;
    public float NextBlinkToggleAt;
    public byte VisibleState;
    public float BaseScale;
}

public struct PickupBlinking : IComponentData, IEnableableComponent { }

public struct PickupBeingCollected : IComponentData, IEnableableComponent { }

public struct PickupSettled : IComponentData, IEnableableComponent { }

public struct PickupFloat : IComponentData
{
    public float Amplitude;
    public float Period;
    public float PhaseOffset;
    public float RestY;
    public float RestTimer;
}
