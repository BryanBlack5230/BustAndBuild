using System;
using Cysharp.Threading.Tasks;
using GameEngine.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Configs
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
    
    [Serializable]
    public class GeneralConfigContainer
    {
        
    }

}