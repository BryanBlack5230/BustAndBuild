using Reflex.Attributes;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    /// <summary>
    /// Runtime diagnostics for throws, plus the live apply of configured gravity to the physics world.
    /// Throw tuning lives on the ConfigHub (<see cref="ThrowConfigSO"/>); the <c>ThrowVelocitySettings</c>
    /// singleton is baked by <c>BlobContainer</c>. Gravity is applied here every frame (rather than baked
    /// once) because <c>PhysicsStep</c> is authored in the battle subscene and only appears once that scene
    /// loads — a bootstrap-only bake would miss it.
    /// </summary>
    public class ThrowDebugTracker : MonoBehaviour, IGameUpdateListener
    {
        [Header("Read Only")]
        [SerializeField] private float _lastThrowRawPower;
        [SerializeField] private float _scaledThrowForce;
        [SerializeField] private float _throwImpulse;
        [SerializeField] private float _thrownEntityMass;
        [SerializeField] private float _currentEntitySpeed;
        [SerializeField] private float _computedVelocityPower;

        private ThrowConfigSO _config;

        private Entity _trackedEntity = Entity.Null;
        private EntityManager _entityManager;
        private EntityQuery _physicsStepQuery;
        private bool _initialized;

        [Inject]
        private void Construct(ThrowConfigSO config) => _config = config;

        public void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;
            _physicsStepQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PhysicsStep>());
            _initialized = true;
        }

        public void OnThrow(Entity entity, float rawPower)
        {
            if (!_initialized) Initialize();

            _trackedEntity = entity;
            _lastThrowRawPower = rawPower;
            _scaledThrowForce = rawPower * (_config != null ? _config.ThrowScale : 0f);

            var mass = _entityManager.GetComponentData<PhysicsMass>(entity);
            _thrownEntityMass = mass.InverseMass > 0f ? 1f / mass.InverseMass : 0f;
            _throwImpulse = _scaledThrowForce * mass.InverseMass;
        }

        public void OnUpdate(float deltaTime)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            if (!_initialized) Initialize();

            SyncGravity();

            if (_trackedEntity == Entity.Null) return;

            if (!_entityManager.Exists(_trackedEntity)
                || !_entityManager.HasComponent<InAir>(_trackedEntity)
                || !_entityManager.IsComponentEnabled<InAir>(_trackedEntity))
            {
                _currentEntitySpeed = 0f;
                _computedVelocityPower = 0f;
                _trackedEntity = Entity.Null;
                return;
            }

            var vel = _entityManager.GetComponentData<PhysicsVelocity>(_trackedEntity);
            _currentEntitySpeed = math.length(vel.Linear);
            _computedVelocityPower = ComputeVelocityPower(_currentEntitySpeed);
        }

        // PhysicsStep is owned by the battle subscene, so only write when the singleton exists.
        private void SyncGravity()
        {
            if (_config == null || _physicsStepQuery.IsEmpty) return;

            var entity = _physicsStepQuery.GetSingletonEntity();
            var step = _entityManager.GetComponentData<PhysicsStep>(entity);
            step.Gravity = new float3(0f, -_config.Gravity, 0f);
            _entityManager.SetComponentData(entity, step);
        }

        private float ComputeVelocityPower(float speed)
        {
            if (_config == null) return 0f;

            var min = _config.MinMaxVelocity.x;
            var max = _config.MinMaxVelocity.y;
            var t = math.saturate((speed - min) / (max - min));
            return _config.VelocityPowerCurve != null ? _config.VelocityPowerCurve.Evaluate(t) : t;
        }
    }
}
