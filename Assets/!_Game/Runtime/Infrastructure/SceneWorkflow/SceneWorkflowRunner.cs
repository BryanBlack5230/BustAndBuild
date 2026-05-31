#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.SceneWorkflow
{
    /// <summary>
    /// Runs a scene workflow from a RunConfiguration.
    /// In the editor, only activates when the SceneWorkflow tool explicitly sets a config.
    /// In builds, activates when a RunConfiguration has IsBuildConfig set.
    /// </summary>
    public sealed class SceneWorkflowRunner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_EDITOR
            var configPath = UnityEditor.SessionState.GetString(SceneWorkflowConstants.RunConfigPathKey, string.Empty);
            if (string.IsNullOrEmpty(configPath)) return;

            var runConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<RunConfiguration>(configPath);
            if (runConfig == null) return;

            var isSingleScene = UnityEditor.SessionState.GetBool(SceneWorkflowConstants.IsSingleSceneKey, false);
            var singleSceneTarget = UnityEditor.SessionState.GetString(SceneWorkflowConstants.SingleSceneTargetKey, string.Empty);

            Spawn(runConfig, isSingleScene, singleSceneTarget);
#else
        var configs = AssetService.R.LoadAll<RunConfiguration>(RuntimeConstants.SceneWorkflow.RunConfigurationsPath);
        for (var i = 0; i < configs.Length; i++)
        {
            if (!configs[i].IsBuildConfig) continue;
            Spawn(configs[i], false, string.Empty);
            return;
        }
#endif
        }

        private static void Spawn(RunConfiguration config, bool isSingleScene, string singleSceneTarget)
        {
            var go = new GameObject("[SceneWorkflowRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneWorkflowRunner>()
                .RunAsync(config, isSingleScene, singleSceneTarget)
                .Forget();
        }

        private async UniTask RunAsync(RunConfiguration runConfig, bool isSingleScene, string singleSceneTarget)
        {
            var scenes = BuildEffectiveSceneList(runConfig.Chain, isSingleScene, singleSceneTarget);

            for (var i = 0; i < scenes.Count; i++)
            {
                await WaitForSceneInit(scenes[i]);

                if (i < scenes.Count - 1)
                    SceneManager.LoadSceneAsync(scenes[i + 1], LoadSceneMode.Additive);
            }

            var overrides = runConfig.Overrides;
            for (var i = 0; i < overrides.Count; i++)
            {
                try
                {
                    await overrides[i].Apply();
                }
                catch (Exception e)
                {
                    Log.Boot.W($"Override '{overrides[i].GetType().Name}' skipped: {e.Message}");
                }
            }

            Destroy(gameObject);
        }

        private static List<string> BuildEffectiveSceneList(SceneChain chain, bool isSingleScene, string singleSceneTarget)
        {
            if (!isSingleScene || chain.SceneNames.Count == 0)
                return new List<string>(chain.SceneNames);

            var bootstrap = chain.SceneNames[0];
            if (string.IsNullOrEmpty(singleSceneTarget) || singleSceneTarget == bootstrap)
                return new List<string> { bootstrap };

            return new List<string> { bootstrap, singleSceneTarget };
        }

        private static async UniTask WaitForSceneInit(string sceneName)
        {
            Scene scene;
            while (!(scene = SceneManager.GetSceneByName(sceneName)).isLoaded)
                await UniTask.Yield();

            var flow = FindSceneFlow(scene);
            if (flow != null)
                await flow.WaitForInit();
        }

        private static ISceneFlow? FindSceneFlow(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var flow = roots[i].GetComponentInChildren<ISceneFlow>(true);
                if (flow != null) return flow;
            }
            return null;
        }
    }
}