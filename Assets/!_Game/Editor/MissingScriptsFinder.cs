#nullable enable
using System.Collections.Generic;
using BarkingBird.Runtime.Infrastructure.Utilities;
using UnityEditor;
using UnityEngine;

namespace BarkingBird.Editor
{
    /// <summary>
    /// Finds and removes missing-script references on the current selection
    /// (children included). Handles both scene objects and prefab assets.
    /// Adapted from UGFW (MIT, © 2026 MAK).
    /// </summary>
    public static class MissingScriptsFinder
    {
        [MenuItem("BarkingBird/Missing Scripts/Find In Selection")]
        public static void FindMissing()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Log.Editor.D("Missing Scripts: nothing selected — select scene objects or prefabs first.");
                return;
            }

            var brokenObjects = new HashSet<GameObject>();
            foreach (GameObject selected in Selection.gameObjects)
            {
                foreach (Transform t in selected.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                        brokenObjects.Add(t.gameObject);
                }
            }

            var selection = new GameObject[brokenObjects.Count];
            brokenObjects.CopyTo(selection);
            Selection.objects = selection;

            Log.Editor.D($"Missing Scripts: found {selection.Length} object(s) with missing scripts — they are now selected.");
        }

        [MenuItem("BarkingBird/Missing Scripts/Remove From Selection")]
        public static void RemoveMissingFromSelection()
        {
            var totalRemoved = 0;
            var objectsTouched = 0;

            foreach (GameObject selected in Selection.gameObjects)
            {
                if (EditorUtility.IsPersistent(selected))
                    RemoveFromPrefabAsset(selected, ref totalRemoved, ref objectsTouched);
                else
                    RemoveFromSceneObject(selected, ref totalRemoved, ref objectsTouched);
            }

            Log.Editor.D($"Missing Scripts: removed {totalRemoved} missing script slot(s) from {objectsTouched} object(s).");
        }

        private static void RemoveFromSceneObject(GameObject selected, ref int totalRemoved, ref int objectsTouched)
        {
            foreach (Transform t in selected.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0)
                    continue;

                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed <= 0) continue;
                totalRemoved += removed;
                objectsTouched++;
                EditorUtility.SetDirty(go);
            }
        }

        // Prefab assets cannot be edited in place — removals silently fail to
        // persist to the .prefab file, so the asset is opened as prefab contents
        // and saved back. Not undoable (prefab contents live outside the scene).
        private static void RemoveFromPrefabAsset(GameObject prefabAsset, ref int totalRemoved, ref int objectsTouched)
        {
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var changed = false;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0)
                        continue;

                    var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed > 0)
                    {
                        totalRemoved += removed;
                        objectsTouched++;
                        changed = true;
                    }
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
