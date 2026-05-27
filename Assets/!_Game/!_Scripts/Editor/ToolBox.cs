using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Editor
{
    public class ToolBox
    {
        [MenuItem("BarkingBird/Scenes/Bootstrap &1", priority = 202)]
        public static void OpenBootstrapScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/!_Game/Scenes/0.Bootstrap.unity");
        }

        [MenuItem("BarkingBird/Scenes/Loading &2", priority = 203)]
        public static void OpenLoadingScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/!_Game/Scenes/1.Loading.unity");
        }
        
        [MenuItem("BarkingBird/Scenes/World &3", priority = 204)]
        public static void OpenWorldScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/!_Game/Scenes/2.World.unity");
        }
        
        [MenuItem("BarkingBird/Scenes/Battle &4", priority = 205)]
        public static void OpenBattleScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/!_Game/Scenes/3.BattleGroundScene.unity");
        }
        
        [MenuItem("BarkingBird/Scenes/City &5", priority = 206)]
        public static void OpenCityScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/!_Game/Scenes/4.City.unity");
        }
    }
}