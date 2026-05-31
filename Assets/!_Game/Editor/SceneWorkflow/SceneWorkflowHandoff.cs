#nullable enable

using UnityEditor;

using BarkingBird.Runtime.Infrastructure.SceneWorkflow;

namespace BarkingBird.Editor
{
    internal static class SceneWorkflowHandoff
    {
        internal static void Set(RunConfiguration config, bool isSingleScene, string singleSceneTarget)
        {
            SessionState.SetString(EditorConstants.SceneWorkflow.RunConfigPathKey, AssetDatabase.GetAssetPath(config));
            SessionState.SetBool(EditorConstants.SceneWorkflow.IsSingleSceneKey, isSingleScene);
            SessionState.SetString(EditorConstants.SceneWorkflow.SingleSceneTargetKey, singleSceneTarget);
        }

        internal static bool TryGet(out RunConfiguration? config, out bool isSingleScene, out string singleSceneTarget)
        {
            config = null;
            isSingleScene = false;
            singleSceneTarget = string.Empty;

            var path = SessionState.GetString(EditorConstants.SceneWorkflow.RunConfigPathKey, string.Empty);
            if (string.IsNullOrEmpty(path)) return false;

            config = AssetDatabase.LoadAssetAtPath<RunConfiguration>(path);
            if (config == null) return false;

            isSingleScene = SessionState.GetBool(EditorConstants.SceneWorkflow.IsSingleSceneKey, false);
            singleSceneTarget = SessionState.GetString(EditorConstants.SceneWorkflow.SingleSceneTargetKey, string.Empty);
            return true;
        }

        internal static void Clear()
        {
            SessionState.EraseString(EditorConstants.SceneWorkflow.RunConfigPathKey);
            SessionState.EraseBool(EditorConstants.SceneWorkflow.IsSingleSceneKey);
            SessionState.EraseString(EditorConstants.SceneWorkflow.SingleSceneTargetKey);
        }

        internal static RunConfiguration? AutoResolveConfig(string sceneName)
        {
            var guids = AssetDatabase.FindAssets("t:RunConfiguration");
            RunConfiguration? firstMatch = null;

            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var candidate = AssetDatabase.LoadAssetAtPath<RunConfiguration>(assetPath);
                if (candidate == null || !candidate.Chain.ContainsScene(sceneName)) continue;
                if (candidate.IsDefault) return candidate;
                firstMatch ??= candidate;
            }

            return firstMatch;
        }
    }
}
