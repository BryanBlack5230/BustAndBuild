using Unity.Entities;
using UnityEngine;


public class AttackAuthoring : MonoBehaviour
{
    public float damage;
    public float cooldown;
    public float range;
    public class Baker : Baker<AttackAuthoring>
    {
        public override void Bake(AttackAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AttackData
            {
                Damage = authoring.damage,
                CooldownTime = authoring.cooldown,
                AttackRange = authoring.range,
            });
        }
    }
}

public struct AttackData : IComponentData
{
    public float Damage;
    public float CooldownTime;
    public float AttackRange;
}

public struct AttackCooldownExpirationTimestamp : IComponentData, IEnableableComponent
{
    public double Value;
}