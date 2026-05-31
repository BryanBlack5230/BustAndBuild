using UnityEditor;

using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Editor
{
    public class ToolBox
    {
        [MenuItem("BarkingBird/Scenes/Bootstrap &1", priority = 202)]
        public static void OpenBootstrapScene() =>
            EditorSceneUtils.OpenSceneByBuildIndex(RuntimeConstants.Scenes.Bootstrap);

        [MenuItem("BarkingBird/Scenes/Loading &2", priority = 203)]
        public static void OpenLoadingScene() =>
            EditorSceneUtils.OpenSceneByBuildIndex(RuntimeConstants.Scenes.Loading);

        [MenuItem("BarkingBird/Scenes/World &3", priority = 204)]
        public static void OpenWorldScene() =>
            EditorSceneUtils.OpenSceneByBuildIndex(RuntimeConstants.Scenes.World);

        [MenuItem("BarkingBird/Scenes/Battle &4", priority = 205)]
        public static void OpenBattleScene() =>
            EditorSceneUtils.OpenSceneByBuildIndex(RuntimeConstants.Scenes.Battle);

        [MenuItem("BarkingBird/Scenes/City &5", priority = 206)]
        public static void OpenCityScene() =>
            EditorSceneUtils.OpenSceneByBuildIndex(RuntimeConstants.Scenes.City);
    }
}