#nullable enable
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Input
{
    internal static class BoundaryConstraints
    {
        private static CollisionFilter? _groundFilter;
        private static CollisionFilter GroundFilter => _groundFilter ??= new CollisionFilter
        {
            BelongsTo    = ~0u,
            CollidesWith = (uint)(1 << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground)),
            GroupIndex   = 0
        };

        public static float GetGroundY(float3 position, in PhysicsWorldSingleton world)
        {
            var ray = new RaycastInput
            {
                Start  = new float3(position.x, position.y + 200f, position.z),
                End    = new float3(position.x, position.y - 200f, position.z),
                Filter = GroundFilter
            };
            return world.CastRay(ray, out var hit) ? hit.Position.y : 0f;
        }

        public static float3 ClampToViewport(float3 position, quaternion rotation, float2 halfExtents, UnityEngine.Camera camera, bool clampBottom)
        {
            var hw = halfExtents.x;
            var hh = halfExtents.y;

            var vp0 = camera.WorldToViewportPoint(position + math.rotate(rotation, new float3(-hw, -hh, 0f)));
            var vp1 = camera.WorldToViewportPoint(position + math.rotate(rotation, new float3(+hw, -hh, 0f)));
            var vp2 = camera.WorldToViewportPoint(position + math.rotate(rotation, new float3(-hw, +hh, 0f)));
            var vp3 = camera.WorldToViewportPoint(position + math.rotate(rotation, new float3(+hw, +hh, 0f)));

            var minVpX = Mathf.Min(Mathf.Min(vp0.x, vp1.x), Mathf.Min(vp2.x, vp3.x));
            var maxVpX = Mathf.Max(Mathf.Max(vp0.x, vp1.x), Mathf.Max(vp2.x, vp3.x));
            var maxVpY = Mathf.Max(Mathf.Max(vp0.y, vp1.y), Mathf.Max(vp2.y, vp3.y));

            const float margin = 0.01f;
            float shiftX = 0f, shiftY = 0f;

            if (minVpX < margin)           shiftX = margin - minVpX;
            else if (maxVpX > 1f - margin) shiftX = (1f - margin) - maxVpX;

            if (clampBottom)
            {
                var minVpY = Mathf.Min(Mathf.Min(vp0.y, vp1.y), Mathf.Min(vp2.y, vp3.y));
                if (minVpY < margin)           shiftY = margin - minVpY;
                else if (maxVpY > 1f - margin) shiftY = (1f - margin) - maxVpY;
            }
            else
            {
                if (maxVpY > 1f - margin) shiftY = (1f - margin) - maxVpY;
            }

            if (shiftX == 0f && shiftY == 0f) return position;

            var centerVp = camera.WorldToViewportPoint(position);
            var newWorld = camera.ViewportToWorldPoint(new Vector3(centerVp.x + shiftX, centerVp.y + shiftY, centerVp.z));
            return new float3(newWorld.x, newWorld.y, position.z);
        }
    }
}
