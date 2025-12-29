using UnityEngine;
using Unity.Entities;

public class BeaconAuthoring : MonoBehaviour
{
    public class Baker : Baker<BeaconAuthoring>
    {
        public override void Bake(BeaconAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new BeaconTag
						{
						
						});
        }
    }
}

public struct BeaconTag : IComponentData
{

}