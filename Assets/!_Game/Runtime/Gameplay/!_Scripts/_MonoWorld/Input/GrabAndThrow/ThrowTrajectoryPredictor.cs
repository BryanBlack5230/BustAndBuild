#nullable enable
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

using Material = UnityEngine.Material;
using Object = UnityEngine.Object;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class ThrowTrajectoryPredictor : IGameUpdateListener, IWorldInitializable, IDisposable
    {
        private const float DefaultBounceElasticity = 0.8f;

        private readonly CursorMovementCalculations _cursorMovements;
        private readonly ThrowConfigSO _throwConfig;
        private readonly TrajectoryPredictorSettings _settings;
        private EntityManager _entityManager;
        private EntityQuery _cameraFrustumQuery;
        private EntityQuery _battleCenterQuery;
        private EntityQuery _physicsWorldQuery;

        private readonly LineRenderer _trajectoryLine;
        private readonly LineRenderer _impactCircle;
        private readonly Material _trajectoryMat;
        private readonly Material _impactMat;
        private readonly Vector3[] _linePoints;
        private readonly Vector3[] _circlePoints;

        private Entity _trackedEntity;
        private PhysicsMass _trackedMass;
        private float _bounceElasticity;
        private float _entityHalfHeight;
        private float _entityHalfWidth;
        private float _lingerTimer;

        public ThrowTrajectoryPredictor(
            CursorMovementCalculations cursorMovements,
            ThrowConfigSO throwConfig,
            TrajectoryPredictorSettings settings)
        {
            _cursorMovements = cursorMovements;
            _throwConfig = throwConfig;
            _settings = settings;

            _linePoints = new Vector3[settings.SimSteps + 1];
            _circlePoints = new Vector3[settings.CircleSegments];

            _trajectoryLine = Object.Instantiate(settings.TrajectoryLinePrefab);
            _trajectoryLine.enabled = false;
            _trajectoryMat = _trajectoryLine.material;

            _impactCircle = Object.Instantiate(settings.ImpactCirclePrefab);
            _impactCircle.enabled = false;
            _impactMat = _impactCircle.material;
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _cameraFrustumQuery = _entityManager.CreateEntityQuery(typeof(CameraFrustumData));
            _battleCenterQuery = _entityManager.CreateEntityQuery(typeof(BattleScreenCenter));
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        }

        public void Dispose()
        {
            _cameraFrustumQuery.Dispose();
            _battleCenterQuery.Dispose();
            _physicsWorldQuery.Dispose();
            if (_trajectoryLine) Object.Destroy(_trajectoryLine.gameObject);
            if (_impactCircle) Object.Destroy(_impactCircle.gameObject);
            if (_trajectoryMat) Object.Destroy(_trajectoryMat);
            if (_impactMat) Object.Destroy(_impactMat);
        }

        public void StartTracking(Entity entity, PhysicsMass originalMass)
        {
            _lingerTimer = 0f;
            SetMaterialAlpha(_trajectoryMat, 1f);
            SetMaterialAlpha(_impactMat, 1f);
            _trackedEntity = entity;
            _trackedMass = originalMass;
            _bounceElasticity = _entityManager.HasComponent<BounceDamage>(entity)
                ? _entityManager.GetComponentData<BounceDamage>(entity).BounceElasticity
                : DefaultBounceElasticity;
            var halfExtents = PhysicsUtility.GetEntityHalfExtentsXY(entity, _entityManager);
            _entityHalfHeight = halfExtents.y;
            _entityHalfWidth  = halfExtents.x;
            _trajectoryLine.enabled = true;
            _impactCircle.enabled = false;
        }

        public void StopTracking()
        {
            _trackedEntity = Entity.Null;
            if (_trajectoryLine.enabled)
                _lingerTimer = _settings.LingerDuration;
            else
                HideVisuals();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_trackedEntity == Entity.Null)
            {
                TickLinger(deltaTime);
                return;
            }

            if (!_entityManager.Exists(_trackedEntity))
            {
                HideVisuals();
                return;
            }

            if (!TryGetSimulationInputs(out var startPos, out var groundY, out var camData, out var battleCenter))
                return;

            Simulate(startPos, ComputeThrowVelocity(), groundY, in camData, in battleCenter);
        }

        private void TickLinger(float deltaTime)
        {
            if (_lingerTimer <= 0f) return;
            _lingerTimer -= deltaTime;
            var alpha = math.saturate(_lingerTimer / _settings.LingerDuration);
            SetMaterialAlpha(_trajectoryMat, alpha);
            SetMaterialAlpha(_impactMat, alpha);
            if (_lingerTimer <= 0f) HideVisuals();
        }

        private bool TryGetSimulationInputs(out float3 startPos, out float groundY,
            out CameraFrustumData camData, out BattleScreenCenter battleCenter)
        {
            startPos = _entityManager.GetComponentData<LocalTransform>(_trackedEntity).Position;
            groundY = _physicsWorldQuery.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorld)
                ? BoundaryConstraints.GetGroundY(startPos, in physicsWorld)
                : 0f;

            if (!_cameraFrustumQuery.TryGetSingleton<CameraFrustumData>(out camData) || !camData.IsLive)
            {
                battleCenter = default;
                return false;
            }

            return _battleCenterQuery.TryGetSingleton<BattleScreenCenter>(out battleCenter);
        }

        private float3 ComputeThrowVelocity()
        {
            // Project cursor velocity forward using acceleration so the line anticipates fast flicks
            var cursorVel = _cursorMovements.Velocity + _cursorMovements.Acceleration * _settings.LookAheadTime;
            var rawPower  = cursorVel.magnitude;
            var impulse   = new float3(cursorVel.normalized.x, cursorVel.normalized.y, 0f)
                            * (rawPower * _throwConfig.ThrowScale);
            return impulse * _trackedMass.InverseMass;
        }

        private void HideVisuals()
        {
            _trajectoryLine.enabled = false;
            _impactCircle.enabled = false;
        }

        private void Simulate(float3 startPos, float3 startVel, float groundY,
            in CameraFrustumData camData, in BattleScreenCenter battleCenter)
        {
            var groundFloor = groundY + _entityHalfHeight;

            var pos     = startPos;
            var vel     = startVel;
            var gravity = _throwConfig.Gravity;

            var worldToCam = camData.WorldToCameraMatrix;
            var camToWorld = math.inverse(worldToCam);
            var camUp      = camToWorld.c1.xyz;
            var tanHalfFov = math.tan(math.radians(camData.Fov * 0.5f));

            // Project entity half-dimensions onto camera axes to match ScreenBounceSystem corner checks.
            // camRight is ~world-X for a camera with only X-tilt; camUp.y ≈ cos(tilt).
            var entityHalfWidth  = _entityHalfWidth;                       // camera-space half-width
            var entityHalfHeight = _entityHalfHeight * math.abs(camUp.y); // camera-space half-height (compressed by tilt)

            var pointCount = 0;
            _linePoints[pointCount++] = new Vector3(pos.x, pos.y, pos.z);

            var impactFound = false;

            for (var i = 0; i < _settings.SimSteps; i++)
            {
                var prevPos = pos;
                vel.y -= gravity * _settings.SimDt;
                pos   += vel * _settings.SimDt;

                // Ground contact — stop here, circle drawn at actual ground surface (not entity centre)
                if (pos.y < groundFloor)
                {
                    var t = (prevPos.y - groundFloor) / (prevPos.y - pos.y);
                    var impactX = math.lerp(prevPos.x, pos.x, t);
                    var impactZ = math.lerp(prevPos.z, pos.z, t);
                    _linePoints[pointCount++] = new Vector3(impactX, groundFloor, impactZ);
                    DrawImpactCircle(new float3(impactX, groundY, impactZ), in camData);
                    impactFound = true;
                    break;
                }

                // Screen edge bounce — entity EDGES vs frustum boundary, matching ScreenBounceSystem
                SimulateBounce(ref pos, ref vel, worldToCam, camToWorld, tanHalfFov,
                    entityHalfWidth, entityHalfHeight, in camData, in battleCenter);

                _linePoints[pointCount++] = new Vector3(pos.x, pos.y, pos.z);
            }

            if (!impactFound) _impactCircle.enabled = false;

            _trajectoryLine.positionCount = pointCount;
            _trajectoryLine.SetPositions(_linePoints);
        }

        private void SimulateBounce(
            ref float3 pos, ref float3 vel,
            float4x4 worldToCam, float4x4 camToWorld,
            float tanHalfFov, float entityHalfWidth, float entityHalfHeight,
            in CameraFrustumData camData, in BattleScreenCenter battleCenter)
        {
            var camPos = math.transform(worldToCam, pos);
            var depth  = -camPos.z;
            if (depth <= 0f) return;

            var camRight = camToWorld.c0.xyz;
            var camUp    = camToWorld.c1.xyz;

            var halfHeight = depth * tanHalfFov;
            var halfWidth  = halfHeight * camData.Aspect + battleCenter.HalfWidthOffset;
            var topLimit   = halfHeight + battleCenter.HalfHeightOffset;
            var bounced    = false;
            var camSnap    = camPos;

            if (camPos.x - entityHalfWidth < -halfWidth && math.dot(vel, camRight) < 0f)
            {
                vel       = math.reflect(vel, camRight);
                camSnap.x = -halfWidth + entityHalfWidth;
                bounced   = true;
            }
            else if (camPos.x + entityHalfWidth > halfWidth && math.dot(vel, -camRight) < 0f)
            {
                vel       = math.reflect(vel, -camRight);
                camSnap.x = halfWidth - entityHalfWidth;
                bounced   = true;
            }

            if (camPos.y + entityHalfHeight > topLimit && math.dot(vel, -camUp) < 0f)
            {
                vel       = math.reflect(vel, -camUp);
                camSnap.y = topLimit - entityHalfHeight;
                bounced   = true;
            }

            if (bounced)
            {
                vel *= _bounceElasticity;
                pos  = math.transform(camToWorld, camSnap);
            }
        }

        private static void SetMaterialAlpha(Material target, float alpha)
        {
            var color = target.color;
            color.a = alpha;
            target.color = color;
        }

        private void DrawImpactCircle(float3 center, in CameraFrustumData camData)
        {
            // Build two axes that lie on the ground plane (XZ), derived from the camera
            // so the ellipse aligns with however the camera is oriented.
            var camToWorld    = math.inverse(camData.WorldToCameraMatrix);
            var camRight      = math.normalize(camToWorld.c0.xyz);
            var groundForward = math.normalize(math.cross(camRight, math.up()));

            var segments = _settings.CircleSegments;
            var radius   = _settings.CircleRadius;
            for (var i = 0; i < segments; i++)
            {
                var angle  = i / (float)segments * math.PI2;
                var offset = camRight * (math.cos(angle) * radius)
                           + groundForward * (math.sin(angle) * radius);
                _circlePoints[i] = new Vector3(center.x + offset.x, center.y + offset.y, center.z + offset.z);
            }
            _impactCircle.positionCount = segments;
            _impactCircle.SetPositions(_circlePoints);
            _impactCircle.enabled = true;
        }
    }
}
