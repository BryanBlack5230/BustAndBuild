using Unity.Entities;
using UnityEngine;

public class BounceDamageAuthoring : MonoBehaviour
{
    public float baseDamage = 5f;
    public float bounceMultiplier = 2f;
    public float bounceElasticity = 0.8f;

    public class Baker : Baker<BounceDamageAuthoring>
    {
        public override void Bake(BounceDamageAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BounceDamage
            {
                BaseDamage = authoring.baseDamage,
                BounceCount = 0,
                BounceDamageMultiplier = authoring.bounceMultiplier,
                BounceElasticity = authoring.bounceElasticity
            });
        }
    }
}

public struct BounceDamage : IComponentData
{
    public int BounceCount;
    public float BounceElasticity;
    public float BaseDamage;
    public float BounceDamageMultiplier;
}