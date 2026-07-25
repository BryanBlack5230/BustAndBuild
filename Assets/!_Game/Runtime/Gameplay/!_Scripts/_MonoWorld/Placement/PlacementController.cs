#nullable enable

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Placement
{
    /// <summary>
    /// Mono-side implementation of the placement settle-check (ADR-0007). Each frame it scans every
    /// <c>Claimable</c> entity that is <c>InAir</c>-enabled and, for each registered <see cref="AssignablePlace"/>,
    /// claims the entity when it is moving slower than the place's velocity threshold and sits inside its radius.
    ///
    /// Detection is debounced <b>per settle</b>: an entity fires a place's handler once, then not again until it
    /// leaves every place's window (lands / is re-grabbed / drifts out), so a can't-afford Core does not re-run
    /// its handler every frame it lingers near the socket. Handlers run after the scan because they make
    /// structural changes (hiding the entity).
    ///
    /// Single-Core scale for now; the stable contract (<c>Claimable</c> + <see cref="AssignablePlace"/>) lifts to
    /// an ECS detector when unit-assignment brings volume.
    /// </summary>
    public sealed class PlacementController : IGameUpdateListener, IWorldInitializable, IDisposable
    {
        private EntityManager _entityManager;
        private EntityQuery _claimableQuery;

        private readonly List<AssignablePlace> _places = new();
        private readonly HashSet<Entity> _attempted = new();
        private readonly HashSet<Entity> _seenThisFrame = new();
        private readonly List<(AssignablePlace place, Entity entity)> _pendingClaims = new();
        private readonly Predicate<Entity> _notSeenThisFrame;

        public PlacementController()
        {
            _notSeenThisFrame = entity => !_seenThisFrame.Contains(entity);
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _claimableQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Claimable>(),
                ComponentType.ReadOnly<InAir>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        public void Register(AssignablePlace place)
        {
            if (!_places.Contains(place)) _places.Add(place);
        }

        public void Unregister(AssignablePlace place) => _places.Remove(place);

        public void OnUpdate(float deltaTime)
        {
            if (_places.Count == 0) return;

            using var entities = _claimableQuery.ToEntityArray(Allocator.Temp);
            _seenThisFrame.Clear();
            _pendingClaims.Clear();

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var position = _entityManager.GetComponentData<LocalTransform>(entity).Position;
                var speed = math.length(_entityManager.GetComponentData<PhysicsVelocity>(entity).Linear);

                for (var p = 0; p < _places.Count; p++)
                {
                    var place = _places[p];
                    if (!IsSettledInto(position, speed, place.Position, place.Radius, place.VelThreshold)) continue;

                    _seenThisFrame.Add(entity);
                    if (_attempted.Add(entity))
                        _pendingClaims.Add((place, entity));
                    break; // an entity settles into at most one place per frame
                }
            }

            // Reset the debounce for anything that left every window (it survives only if seen this frame).
            _attempted.RemoveWhere(_notSeenThisFrame);

            // Handlers make structural changes (hiding the claimed entity) — run them after the read scan.
            for (var i = 0; i < _pendingClaims.Count; i++)
                _pendingClaims[i].place.OnClaimed(_pendingClaims[i].entity);
        }

        /// <summary>
        /// Pure settle predicate (the velocity + radius half of the claim; the InAir half is the query): an entity
        /// at <paramref name="entityPosition"/> moving at <paramref name="entitySpeed"/> settles into a place when
        /// it is no faster than the velocity threshold and within the radius. Testable without a live world.
        /// </summary>
        public static bool IsSettledInto(float3 entityPosition, float entitySpeed, float3 placePosition, float radius, float velThreshold)
            => entitySpeed <= velThreshold && math.distancesq(entityPosition, placePosition) <= radius * radius;

        public void Dispose()
        {
            if (_claimableQuery != default) _claimableQuery.Dispose();
        }
    }
}
