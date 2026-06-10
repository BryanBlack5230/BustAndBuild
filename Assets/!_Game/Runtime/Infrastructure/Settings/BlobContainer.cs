using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using BarkingBird.Runtime.Gameplay.AI;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    public sealed class BlobContainer : IDisposable
    {
        private readonly ConfigContainer _container;
        private readonly PrototypeConfigSetter _prototypeConfig;

        private BlobAssetReference<TargetProfilesBlob> _profilesBlob;

        public BlobContainer(ConfigContainer container, PrototypeConfigSetter prototypeConfig)
        {
            _container = container;
            _prototypeConfig = prototypeConfig;
        }

        public void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            if (_profilesBlob.IsCreated) _profilesBlob.Dispose();
            _profilesBlob = CreateProfilesBlob();

            var configEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(configEntity, new TargetProfiles
            {
                Blob = _profilesBlob
            });
            entityManager.SetName(configEntity, "Global_Target_Profiles");
        }

        public void Dispose()
        {
            if (_profilesBlob.IsCreated) _profilesBlob.Dispose();
        }
        
        private BlobAssetReference<TargetProfilesBlob> CreateProfilesBlob()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TargetProfilesBlob>();

            // var enemiesSource = _container.Battle.EnemyProfiles;
            var enemiesSource = _prototypeConfig.EnemyProfiles;
            var enemiesArray = builder.Allocate(ref root.EnemyProfiles, enemiesSource.Count);
            for (int i = 0; i < enemiesSource.Count; i++)
            {
                enemiesArray[i] = ConvertToStruct(enemiesSource[i]);
            }

            // var alliesSource = _container.Battle.AllyProfiles;
            var alliesSource = _prototypeConfig.AllyProfiles;
            var alliesArray = builder.Allocate(ref root.AllyProfiles, alliesSource.Count);
            for (int i = 0; i < alliesSource.Count; i++)
            {
                alliesArray[i] = ConvertToStruct(alliesSource[i]);
            }

            return builder.CreateBlobAssetReference<TargetProfilesBlob>(Allocator.Persistent);
        }
    
        private TargetProfileBlob ConvertToStruct(TargetProfile source)
        {
            return new TargetProfileBlob
            {
                DetectionRadiusSq = source.DetectionRadiusSq * source.DetectionRadiusSq,
                ViewAngleCos = math.cos(math.radians(source.ViewAngleCos * 0.5f)),
                CheckInterval = source.CheckInterval,
                WeightEnemy = source.WeightEnemy,
                WeightAlly = source.WeightAlly,
                WeightWall = source.WeightWall,
                WeightBeacon = source.WeightBeacon,
                DistanceWeight = source.DistanceWeight,
                LowHealthBonus = source.LowHealthBonus,
                AggroBonus = source.AggroBonus,
                LineOfSightBonus = source.LineOfSightBonus
            };
        }
    }
    
    public struct TargetProfilesBlob
    {
        // Arrays indexed by the Enum value
        public BlobArray<TargetProfileBlob> EnemyProfiles;
        public BlobArray<TargetProfileBlob> AllyProfiles;
    }
}