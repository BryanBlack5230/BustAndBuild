using UnityEngine.SceneManagement;

namespace Game.Configs
{
    public static class RuntimeConstants
    {
        public static class Scenes
        {
            public static readonly int Bootstrap = SceneUtility.GetBuildIndexByScenePath("0.Bootstrap");
            public static readonly int Loading = SceneUtility.GetBuildIndexByScenePath("1.Loading");
            public static readonly int World = SceneUtility.GetBuildIndexByScenePath("2.World");
            // public static readonly int Battle = SceneUtility.GetBuildIndexByScenePath("3.BattleGround");
            public static readonly int Battle = SceneUtility.GetBuildIndexByScenePath("3.BattleGroundScene");
            public static readonly int City = SceneUtility.GetBuildIndexByScenePath("4.City");
        }
        public static class PhysicLayers
        {
            public const string Unit = "Unit";
            public const string Grabbable = "Grabbable";
            public const string Ground = "Ground";
        }
        
        public static class Configs
        {
            public const string ConfigFileName = "Config";
        }

        public static class Cursors
        {
            public const string Open = "OpenHandCursor";
            public const string ObjectHold = "HoldingObjectCursor";
            public const string GroundHold = "HoldingGroundCursor";

            public const string Dummy = "DummyPrefab";
            
            public static readonly string[] All = {Open, ObjectHold, GroundHold};
        }
    }
}