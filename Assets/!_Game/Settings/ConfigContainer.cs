using System;
using Cysharp.Threading.Tasks;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using Newtonsoft.Json;
using Unity.Entities;
using UnityEngine;

namespace Game.Configs
{
    public sealed class ConfigContainer : ILoadUnit, IDisposable
    {
        public GeneralConfigContainer General;
        public BattleConfigContainer Battle;
        
        public BlobAssetReference<FindTargetConfigBlob> FindTargetConfigBlob { get; private set; }


        public UniTask Load()
        {
            var asset = AssetService.R.Load<TextAsset>(RuntimeConstants.Configs.ConfigFileName);
            JsonConvert.PopulateObject(asset.text, this);

            CreateBlobAssets();
            
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            if (FindTargetConfigBlob.IsCreated)
                FindTargetConfigBlob.Dispose();
        }

        private void CreateBlobAssets()
        {
            if (Battle?.FindTargetConfig == null)
            {
                Log.Boot.E("FindTargetConfig is null! Check your JSON file");
            }
            else
            {
                FindTargetConfigBlob = BlobConfigConverter.CreateBlob<FindTargetConfigBlob>(Battle.FindTargetConfig);
            }
            
        }
    }

    [Serializable]
    public class BattleConfigContainer
    {
        public CameraConfig CameraConfig;
        public PowerHitConfig PowerHitConfig;
        public FindTargetConfig FindTargetConfig;
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
    public class FindTargetConfig
    {
        public float defaultRange;
        public float defaultCheckInterval;
        public float castleWallPriority;
        public float beaconPriority;
        public float unitPriority;
    }

    public struct FindTargetConfigBlob
    {
        public float defaultRange;
        public float defaultCheckInterval;
        public float castleWallPriority;
        public float beaconPriority;
        public float unitPriority;
    }
    
    [Serializable]
    public class GeneralConfigContainer
    {
        
    }

}