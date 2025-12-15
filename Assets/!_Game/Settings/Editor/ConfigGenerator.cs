using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Game.Configs
{
    public class ConfigGenerator
    {
        [MenuItem("Game/Generate Configs")]
        public static void Generate()
        {
            var configContainer = new ConfigContainer
            {
                General = new GeneralConfigContainer
                {
                    
                },
                Battle = new BattleConfigContainer
                {
                    CameraConfig = new CameraConfig
                    {
                        moveSpeed = 0.1f,
                        returnDuration = 1,
                        returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                        timeToHold = 1,
                        maxOutsideDistance = 15,
                        borderPushCurve = new AnimationCurve(
                            new Keyframe(0f, 0f, 3f, 0f), 
                                        new Keyframe(1f, 1f, 0f, 0f)),
                    },
                    PowerHitConfig = new PowerHitConfig
                    {
                        duration = 1,
                        force = 2,
                        sizeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                    },
                    FindTargetConfig = new FindTargetConfig
                    {
                        defaultRange = 5f,
                        defaultCheckInterval = 0.01f,
                        beaconPriority = 1f,
                        castleWallPriority = 2f,
                        unitPriority = 3f,
                    }
                }
            };
            var json = JsonConvert.SerializeObject(configContainer, Formatting.Indented);
            var path = Path.Combine(Application.dataPath, "!_Game", "Resources", RuntimeConstants.Configs.ConfigFileName + ".json");
            File.WriteAllText(path, json);
        }
    }
}