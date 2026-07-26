#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class ReleaseCoordinator : IDisposable, IWorldInitializable
    {
        private readonly OverlapResolver _overlapResolver;
        private readonly OverlapEjector _overlapEjector;
        private EntityManager _entityManager;
        private readonly Dictionary<Entity, CancellationTokenSource> _inflight = new();

        public ReleaseCoordinator(OverlapResolver overlapResolver, OverlapEjector overlapEjector)
        {
            _overlapResolver = overlapResolver;
            _overlapEjector = overlapEjector;
        }

        public void Initialize(EntityManager em) => _entityManager = em;

        public void Dispose()
        {
            foreach (var cts in _inflight.Values)
                cts.Cancel();
            _inflight.Clear();
        }

        public void HandleRelease(Entity entity, float3 impulse, bool isFastSpeed, PhysicsMass originalMass)
        {
            if (!isFastSpeed)
            {
                if (_overlapResolver.CheckOverlap(entity))
                {
                    Log.Battle.D($"Entity {entity} overlapping on release, displacing before launch");
                    StartResolve(entity, impulse, originalMass);
                    return;
                }
            }
            else if (_overlapResolver.CheckOverlap(entity))
            {
                var throwDir = math.lengthsq(impulse) > math.EPSILON
                    ? math.normalize(new float3(impulse.x, impulse.y, 0f))
                    : math.up();
                var destinationClear = _overlapEjector.Teleport(entity, throwDir);
                Log.Battle.D($"Entity {entity} fast-thrown through collider");

                if (!destinationClear)
                {
                    Log.Battle.D($"Entity {entity} teleport destination overlapping, falling back to displace");
                    StartResolve(entity, impulse, originalMass);
                    return;
                }
            }

            _overlapResolver.ClampToViewportAndGround(entity);
            RestorePhysicsWithImpulse(entity, impulse, originalMass);
        }

        private void StartResolve(Entity entity, float3 impulse, PhysicsMass originalMass)
        {
            if (_inflight.TryGetValue(entity, out var existing))
                existing.Cancel();
            var cts = new CancellationTokenSource();
            _inflight[entity] = cts;
            ResolveAndCleanup(entity, impulse, originalMass, cts).Forget();
        }

        private async UniTaskVoid ResolveAndCleanup(Entity entity, float3 impulse, PhysicsMass originalMass, CancellationTokenSource cts)
        {
            try
            {
                await _overlapResolver.ResolveAsync(entity, impulse, originalMass, RestorePhysicsWithImpulse, cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_inflight.TryGetValue(entity, out var stored) && ReferenceEquals(stored, cts))
                    _inflight.Remove(entity);
                cts.Dispose();
            }
        }

        private void RestorePhysicsWithImpulse(Entity entity, float3 impulse, PhysicsMass originalMass)
        {
            var velocity = impulse * originalMass.InverseMass;
            _entityManager.SetComponentData(entity, new PhysicsVelocity { Angular = 0f, Linear = velocity });
            _entityManager.SetComponentData(entity, originalMass);
        }
    }
}
