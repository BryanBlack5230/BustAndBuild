using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    public sealed class ConfigContainer : ILoadUnit
    {
        public GeneralConfigContainer General;
        public BattleConfigContainer Battle;

        public UniTask Load()
        {
            var asset = AssetService.R.Load<TextAsset>(RuntimeConstants.Configs.ConfigFileName);
            JsonConvert.PopulateObject(asset.text, this);

            return UniTask.CompletedTask;
        }
    }

    [Serializable]
    public class BattleConfigContainer
    {
        public CameraConfig CameraConfig;
        public PowerHitConfig PowerHitConfig;
        public List<TargetProfile> AllyProfiles;
        public List<TargetProfile> EnemyProfiles;
    }

    [Serializable]
    public class PowerHitConfig
    {
        public float duration;
        public float force;
        public AnimationCurve sizeCurve;
    }

    [Serializable]
    public class CameraConfig
    {
        public float moveSpeed;
        public float returnDuration;
        public AnimationCurve returnCurve;
        public float timeToHold;
        public float maxOutsideDistance;
        public AnimationCurve borderPushCurve; 
    }
    
    [BlobConfig]
    [Serializable]
    public class TargetProfile
    {
        public float DetectionRadiusSq;
        public float ViewAngleCos;
        public float CheckInterval;

        public float WeightEnemy;
        public float WeightAlly;
        public float WeightWall;
        public float WeightBeacon;

        public float DistanceWeight;
        public float LowHealthBonus;
        public float AggroBonus;
        public float LineOfSightBonus;
    }

    public struct TargetProfileBlob
    {
        // General Settings
        public float DetectionRadiusSq;
        public float ViewAngleCos;
        public float CheckInterval;

        // Weights (Positive = Desire, Negative = Avoid/Ignore)
        // A standard Soldier might have: Enemy=100, Ally=-1, Beacon=50
        // A Healer might have: Enemy=-10, Ally=100, Beacon=0
        public float WeightEnemy;
        public float WeightAlly;
        public float WeightWall;
        public float WeightBeacon;

        // Modifiers
        public float DistanceWeight;       // Usually negative (prefer closer)
        public float LowHealthBonus;       // Prefer weak targets (or injured friends if healer)
        public float AggroBonus;           // Priority if they are attacking ME
        public float LineOfSightBonus;     // Prefer visible targets
    }
    
    [Serializable]
    public class GeneralConfigContainer
    {
        
    }

}