using Unity.Entities;
using UnityEngine;

public class WallChildAuthoring : MonoBehaviour
{
    public GameObject ParentWall;
    public class Baker : Baker<WallChildAuthoring>
    {
        public override void Bake(WallChildAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var parent = GetEntity(authoring.ParentWall, TransformUsageFlags.None);
            AddComponent(entity, new WallReference{ParentWallEntity = parent});
        }
    }
}

public struct WallReference : IComponentData {
    public Entity ParentWallEntity;
}