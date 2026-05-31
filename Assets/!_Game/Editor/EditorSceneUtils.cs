using UnityEditor;
using UnityEditor.SceneManagement;

namespace BarkingBird.Editor
{
    public static class EditorSceneUtils
    {
        public static void OpenSceneByBuildIndex(int buildIndex)
        {
            var scenes = EditorBuildSettings.scenes;
            
            if (buildIndex < 0 || buildIndex >= scenes.Length) 
                throw new System.IndexOutOfRangeException();

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scenes[buildIndex].path);
        }
    }
}