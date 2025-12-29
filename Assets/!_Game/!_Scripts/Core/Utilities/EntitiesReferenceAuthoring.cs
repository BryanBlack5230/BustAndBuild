using UnityEngine;
using Unity.Entities;

public class EntitiesReferenceAuthoring : MonoBehaviour
{
    public GameObject enemyPrefab;
    public class Baker : Baker<EntitiesReferenceAuthoring>
    {
        public override void Bake(EntitiesReferenceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new EntitiesReference
						{
						    enemyPrefabEntity = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
						});
        }
    }
}

public struct EntitiesReference : IComponentData
{
    public Entity enemyPrefabEntity;
}