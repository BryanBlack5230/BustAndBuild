using UnityEngine.SceneManagement;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    public static class RuntimeConstants
    {
        public static class Scenes
        {
            private const string ScenesPath = "!_Game/Runtime/Gameplay/Scenes/";

            public static readonly int Bootstrap = SceneUtility.GetBuildIndexByScenePath(ScenesPath + "0.Bootstrap");
            public static readonly int Loading   = SceneUtility.GetBuildIndexByScenePath(ScenesPath + "1.Loading");
            public static readonly int World     = SceneUtility.GetBuildIndexByScenePath(ScenesPath + "2.World");
            public static readonly int Battle    = SceneUtility.GetBuildIndexByScenePath(ScenesPath + "3.BattleGroundScene");
            public static readonly int City      = SceneUtility.GetBuildIndexByScenePath(ScenesPath + "4.City");
        }
        public static class PhysicLayers
        {
            public const string Unit = "Unit";
            public const string Grabbable = "Grabbable";
            public const string Ground = "Ground";
            public const string Obstacle = "Obstacle";
        }
        
        public static class Configs
        {
            public const string ConfigFileName = "Settings/Config";

            // Assets-relative path to the Resources root, used by editor-side ConfigGenerator when writing Config.json.
            public const string AssetsResourcesFolder = "!_Game/Runtime/Gameplay/Resources";
        }

        public static class Cursors
        {
            public const string Open = "OpenHandCursor";
            public const string ObjectHold = "HoldingObjectCursor";
            public const string GroundHold = "HoldingGroundCursor";

            public const string Dummy = "DummyPrefab";

            public const string RootPath = "Cursors/";
            public const string SpritesPath = RootPath + "Sprites/";
            public const string TexturesPath = RootPath + "Textures/";
            public const string DummyPrefabPath = RootPath + Dummy;

            public static readonly string[] All = {Open, ObjectHold, GroundHold};
        }

        public static class Daylight
        {
            public const int MinutesInDay = 1440;
        }

        public static class SceneWorkflow
        {
            public const string RunConfigurationsPath = "Settings/SceneRunConfigurations";
        }
    }
}