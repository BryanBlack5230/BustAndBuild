#nullable enable

using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(SceneChain))]
    internal sealed class SceneChainEditor : UnityEditor.Editor
    {
        private static readonly GUILayoutOption OpenAdditiveButtonWidth = GUILayout.Width(105f);
        private static readonly GUILayoutOption OpenSingleButtonWidth = GUILayout.Width(85f);

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(6f);
            
            if (GUILayout.Button("Open Chain"))
                OpenChain();

            EditorGUILayout.Space(6f);

            serializedObject.Update();

            var scenesProperty = serializedObject.FindProperty("_scenes");
            scenesProperty.isExpanded = EditorGUILayout.Foldout(scenesProperty.isExpanded, "Scenes", true);

            if (scenesProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawSceneList(scenesProperty);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSceneList(SerializedProperty scenesProperty)
        {
            for (var i = 0; i < scenesProperty.arraySize; i++)
            {
                var element = scenesProperty.GetArrayElementAtIndex(i);
                var sceneAssetProp = element.FindPropertyRelative("_sceneAsset");
                var sceneAsset = sceneAssetProp?.objectReferenceValue as SceneAsset;
                var scenePath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty;

                EditorGUILayout.BeginHorizontal();

                if (sceneAssetProp != null)
                    EditorGUILayout.PropertyField(sceneAssetProp, new GUIContent($"Element {i}"));
                else
                    EditorGUILayout.LabelField($"Element {i}", "(unavailable)");

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(scenePath));
                if (GUILayout.Button("Open additively", OpenAdditiveButtonWidth))
                    OpenScene(scenePath, OpenSceneMode.Additive);
                if (GUILayout.Button("Open single", OpenSingleButtonWidth))
                    OpenScene(scenePath, OpenSceneMode.Single);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
        }

        private void OpenChain()
        {
            var scenesProperty = serializedObject.FindProperty("_scenes");
            if (scenesProperty.arraySize == 0) return;

            var chainPaths = new string[scenesProperty.arraySize];
            var chainCount = 0;

            for (var i = 0; i < scenesProperty.arraySize; i++)
            {
                var element = scenesProperty.GetArrayElementAtIndex(i);
                var sceneAsset = element.FindPropertyRelative("_sceneAsset")?.objectReferenceValue as SceneAsset;
                if (sceneAsset == null) continue;

                var path = AssetDatabase.GetAssetPath(sceneAsset);
                if (!string.IsNullOrEmpty(path))
                    chainPaths[chainCount++] = path;
            }

            if (chainCount == 0) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            for (var i = 0; i < chainCount; i++)
            {
                if (!EditorSceneManager.GetSceneByPath(chainPaths[i]).IsValid())
                    EditorSceneManager.OpenScene(chainPaths[i], OpenSceneMode.Additive);
            }

            for (var i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!IsInChain(scene.path, chainPaths, chainCount))
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool IsInChain(string path, string[] chainPaths, int chainCount)
        {
            for (var i = 0; i < chainCount; i++)
                if (chainPaths[i] == path) return true;
            return false;
        }

        private static void OpenScene(string scenePath, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Additive && EditorSceneManager.GetSceneByPath(scenePath).IsValid()) return;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scenePath, mode);
        }
    }
}
