using BarkingBird.Runtime.Infrastructure.GameLoop;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    public class ThrowSettingsSetter : MonoBehaviour, IGameUpdateListener
    {
        private const int CurveSampleCount = 64;

        [Header("Throw Settings")]
        public float ThrowScale = 0.01f;
        public float ThrowThreshold = 100f;

        [Header("Velocity Power Settings")]
        [MinMaxSlider(0f, 100f, showFields: true)]
        public Vector2 MinMaxVelocity = new Vector2(5f, 75f);
        [SerializeField] private AnimationCurve _velocityPowerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [HideInInspector][SerializeField] private float _velocityMin;
        [HideInInspector][SerializeField] private float _velocityMax;

        [Header("Read Only")]
        [SerializeField] private float _lastThrowRawPower;
        [SerializeField] private float _scaledThrowForce;
        [SerializeField] private float _throwImpulse;
        [SerializeField] private float _thrownEntityMass;
        [SerializeField] private float _currentEntitySpeed;
        [SerializeField] private float _computedVelocityPower;

        [Header("Physics Settings")]
        public float Gravity = 9.8f;

        private Entity _trackedEntity = Entity.Null;
        private Entity _settingsEntity = Entity.Null;
        private EntityManager _entityManager;
        private EntityQuery _physicsStepQuery;

        private void OnValidate()
        {
            _velocityMin = MinMaxVelocity.x;
            _velocityMax = MinMaxVelocity.y;
        }

        public void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            _entityManager = world.EntityManager;
            _settingsEntity = _entityManager.CreateEntity(typeof(ThrowVelocitySettings));
            _physicsStepQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PhysicsStep>());
            SyncVelocitySettings();
            SyncGravity();
        }

        public void OnThrow(Entity entity, float rawPower)
        {
            _trackedEntity = entity;
            _lastThrowRawPower = rawPower;
            _scaledThrowForce = rawPower * ThrowScale;

            var mass = _entityManager.GetComponentData<PhysicsMass>(entity);
            _thrownEntityMass = mass.InverseMass > 0f ? 1f / mass.InverseMass : 0f;
            _throwImpulse = _scaledThrowForce * mass.InverseMass;
        }

        public void OnUpdate(float deltaTime)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            SyncVelocitySettings();
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

        private void SyncVelocitySettings()
        {
            if (_settingsEntity == Entity.Null) return;

            var samples = new FixedList512Bytes<float>();
            for (var i = 0; i < CurveSampleCount; i++)
            {
                var t = i / (float)(CurveSampleCount - 1);
                samples.Add(_velocityPowerCurve.Evaluate(t));
            }

            _entityManager.SetComponentData(_settingsEntity, new ThrowVelocitySettings
            {
                MinVelocity = MinMaxVelocity.x,
                MaxVelocity = MinMaxVelocity.y,
                CurveSamples = samples,
            });
        }

        private void SyncGravity()
        {
            if (_physicsStepQuery.IsEmpty) return;

            var entity = _physicsStepQuery.GetSingletonEntity();
            var step = _entityManager.GetComponentData<PhysicsStep>(entity);
            step.Gravity = new float3(0f, -Gravity, 0f);
            _entityManager.SetComponentData(entity, step);
        }

        private float ComputeVelocityPower(float speed)
        {
            var t = math.saturate((speed - MinMaxVelocity.x) / (MinMaxVelocity.y - MinMaxVelocity.x));
            return _velocityPowerCurve.Evaluate(t);
        }
    }
}