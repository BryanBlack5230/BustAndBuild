using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BodyVisualAuthoring : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public class Baker : Baker<BodyVisualAuthoring>
    {
        public override void Bake(BodyVisualAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var renderer = authoring.GetComponent<Renderer>();
            var color = renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasColor(BaseColor)
                ? (float4)(Vector4)renderer.sharedMaterial.GetColor(BaseColor)
                : new float4(1f, 1f, 1f, 1f);

            AddComponent(entity, new BodyVisualTag());
            AddComponent(entity, new BodyOriginalColor { Value = color });
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = color });
            AddComponent(entity, new PostTransformMatrix { Value = float4x4.identity });
        }
    }
}

public struct BodyVisualTag : IComponentData {}

public struct BodyOriginalColor : IComponentData
{
    public float4 Value;
}

public struct BodyVisualRef : IComponentData
{
    public Entity Body;
}
