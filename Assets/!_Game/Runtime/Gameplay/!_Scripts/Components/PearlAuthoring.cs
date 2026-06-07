using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class PearlAuthoring : MonoBehaviour
{
    [SerializeField] private float pearlValue = 1f;

    public class Baker : Baker<PearlAuthoring>
    {
        public override void Bake(PearlAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Pearl { Value = authoring.pearlValue });
            AddComponent(entity, new PearlLifetime
            {
                TimeRemaining = 0f,
                NextBlinkToggleAt = 0f,
                VisibleState = 1,
            });
            AddComponent(entity, new PearlFloat
            {
                Amplitude = 0.1f,
                Period = 2.5f,
                PhaseOffset = 0f,
                RestY = 0f,
                RestTimer = 0f,
            });
            AddComponent(entity, new PhysicsGravityFactor { Value = 1f });
            AddComponent(entity, new PearlBlinking());
            AddComponent(entity, new PearlBeingCollected());
            AddComponent(entity, new PearlSettled());

            SetComponentEnabled<PearlBlinking>(entity, false);
            SetComponentEnabled<PearlBeingCollected>(entity, false);
            SetComponentEnabled<PearlSettled>(entity, false);
        }
    }
}
