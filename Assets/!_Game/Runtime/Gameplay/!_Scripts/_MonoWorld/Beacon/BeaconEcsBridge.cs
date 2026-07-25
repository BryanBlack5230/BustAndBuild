#nullable enable

using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Beacon
{
    /// <summary>
    /// The ECS side of the Beacon Core loop (ADR-0006), mirroring <c>DaylightEcsBridge</c>. Two directions:
    /// <list type="bullet">
    ///   <item><b>Day-end trigger</b> — polls the Beacon's <c>IsInvulnerable</c> flag each frame and, on the rising
    ///     edge, sends <c>ForceFinishDayCommand</c>. A Mono poll (not an ECS-raised event) so it pauses with the
    ///     game: the Day must not end during a (soft-)pause, and <c>OnUpdate</c> only ticks while running.</item>
    ///   <item><b>Day-end reset</b> — on <c>DayEndedEvent</c> re-drops the one persistent Core at the socket (reset
    ///     position, velocity, mass, grab flags) and heals the Beacon to full (Health=Max, clear invuln + pending
    ///     damage). Both day-end causes (invuln tripwire and timer expiry) converge here.</item>
    /// </list>
    /// Also captures the Core's <see cref="PristineMass"/> once while it rests, so the drop can restore an
    /// un-frozen mass. EntityManager writes happen on the main thread from the event/update callbacks — safe.
    /// </summary>
    public sealed class BeaconEcsBridge : IGameUpdateListener, IWorldInitializable, IDisposable
    {
        private readonly CommandDispatcher _dispatcher;
        private readonly BattleSceneData _battleSceneData;
        private EntityManager _entityManager;
        private EntityQuery _beaconQuery;
        private EntityQuery _coreQuery;
        private IDisposable? _dayEndedToken;

        private bool _wasInvulnerable;

        public BeaconEcsBridge(CommandDispatcher dispatcher, BattleSceneData battleSceneData)
        {
            _dispatcher = dispatcher;
            _battleSceneData = battleSceneData;
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;

            _beaconQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<BeaconTag>());
            // Include disabled entities: the Core is hidden (Disabled tag) during the Day and must still be
            // findable so DayEnded can re-enable it.
            _coreQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<BeaconCore>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });

            // Subscribe only after the queries exist: OnDayEnded -> DropCore/HealBeacon read em + both queries.
            _dayEndedToken = EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        public void OnUpdate(float deltaTime)
        {
            CapturePristineMassOnce();
            PollBeaconInvulnerability();
        }

        private void PollBeaconInvulnerability()
        {
            var nowInvulnerable = _beaconQuery.TryGetSingletonEntity<BeaconTag>(out var beacon)
                                  && _entityManager.HasComponent<IsInvulnerable>(beacon)
                                  && _entityManager.IsComponentEnabled<IsInvulnerable>(beacon);

            if (nowInvulnerable && !_wasInvulnerable)
                _dispatcher.Send(new ForceFinishDayCommand());

            _wasInvulnerable = nowInvulnerable;
        }

        private void CapturePristineMassOnce()
        {
            if (!_coreQuery.TryGetSingletonEntity<BeaconCore>(out var core)) return;
            if (!_entityManager.HasComponent<PristineMass>(core)) return;

            var pristine = _entityManager.GetComponentData<PristineMass>(core);
            if (pristine.Captured) return;
            if (!_entityManager.HasComponent<PhysicsMass>(core)) return;

            var mass = _entityManager.GetComponentData<PhysicsMass>(core);
            if (mass.InverseMass <= 0f) return; // frozen by a grab — not the pristine value, wait for a resting frame

            pristine.Value = mass;
            pristine.Captured = true;
            _entityManager.SetComponentData(core, pristine);
        }

        private void OnDayEnded(in DayEndedEvent _)
        {
            DropCore();
            HealBeacon();
            _wasInvulnerable = false;
        }

        private void DropCore()
        {
            if (!_coreQuery.TryGetSingletonEntity<BeaconCore>(out var core))
            {
                Log.Battle.W("[BeaconEcsBridge] DayEnded but no Beacon Core entity found to drop.");
                return;
            }

            _entityManager.SetEnabled(core, true); // un-hide the Core (removes the Disabled tag)

            var socket = _battleSceneData != null ? _battleSceneData.beaconCoreSocket : null;
            if (socket != null)
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(core);
                transform.Position = socket.position;
                _entityManager.SetComponentData(core, transform);
            }

            // PhysicsVelocity/PhysicsMass come from the user-authored dynamic body, not this baker — guard them.
            if (_entityManager.HasComponent<PhysicsVelocity>(core))
                _entityManager.SetComponentData(core, new PhysicsVelocity());

            // Restore the un-frozen mass: a grab freezes InverseMass=0 and the insert can skip the normal
            // release-time restore, so without this the dropped Core comes back immovable.
            if (_entityManager.HasComponent<PristineMass>(core) && _entityManager.HasComponent<PhysicsMass>(core))
            {
                var pristine = _entityManager.GetComponentData<PristineMass>(core);
                if (pristine.Captured)
                    _entityManager.SetComponentData(core, pristine.Value);
            }

            _entityManager.SetComponentEnabled<Grabbed>(core, false);
            _entityManager.SetComponentEnabled<InAir>(core, false);
        }

        private void HealBeacon()
        {
            if (!_beaconQuery.TryGetSingletonEntity<BeaconTag>(out var beacon)) return;

            var health = _entityManager.GetComponentData<Health>(beacon);
            health.Value = health.Max;
            _entityManager.SetComponentData(beacon, health);

            if (_entityManager.HasBuffer<DamageBufferElement>(beacon))
                _entityManager.GetBuffer<DamageBufferElement>(beacon).Clear();

            if (_entityManager.HasComponent<IsInvulnerable>(beacon))
                _entityManager.SetComponentEnabled<IsInvulnerable>(beacon, false);
        }

        public void Dispose()
        {
            _dayEndedToken?.Dispose();
            if (_beaconQuery != default) _beaconQuery.Dispose();
            if (_coreQuery != default) _coreQuery.Dispose();
        }
    }
}
