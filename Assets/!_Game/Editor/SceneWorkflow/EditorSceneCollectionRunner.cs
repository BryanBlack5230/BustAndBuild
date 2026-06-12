#nullable enable

using System;
using BarkingBird.Runtime.Infrastructure.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BarkingBird.Editor
{
    [InitializeOnLoad]
    internal static class SceneWorkflowPlayModeHandler
    {
        private static bool _restartingPlay;

        static SceneWorkflowPlayModeHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    HandleExitingEditMode();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    HandleEnteredEditMode();
                    break;
            }
        }

        private static void HandleExitingEditMode()
        {
            if (_restartingPlay)
            {
                _restartingPlay = false;
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();

            if (!SceneWorkflowHandoff.TryGet(out var config, out _, out _))
            {
                config = SceneWorkflowHandoff.AutoResolveConfig(activeScene.name);
                if (config != null)
                    SceneWorkflowHandoff.Set(config, isSingleScene: false, singleSceneTarget: string.Empty);
            }

            if (config == null || config.Chain.SceneNames.Count == 0) return;

            var firstSceneName = config.Chain.SceneNames[0];
            if (activeScene.name == firstSceneName) return;

            SaveSceneSetup();

            EditorApplication.isPlaying = false;
            _restartingPlay = true;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(config.Chain.ScenePaths[0], OpenSceneMode.Single);

            EditorApplication.delayCall += RestartPlay;

            Log.Editor.D($"Switched to '{firstSceneName}' — restarting play.");
        }

        private static void HandleEnteredEditMode()
        {
            if (_restartingPlay) return;
            RestoreSceneSetup();
        }

        private static void RestartPlay()
        {
            EditorApplication.isPlaying = true;
        }

        private static void SaveSceneSetup()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            SessionState.SetString(EditorConstants.SceneWorkflow.SavedScenesKey, JsonUtility.ToJson(new SceneSetupData(setup)));
        }

        private static void RestoreSceneSetup()
        {
            var json = SessionState.GetString(EditorConstants.SceneWorkflow.SavedScenesKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            SessionState.EraseString(EditorConstants.SceneWorkflow.SavedScenesKey);

            var data = JsonUtility.FromJson<SceneSetupData>(json);
            if (data?.Paths == null || data.Paths.Length == 0) return;

            var setups = new SceneSetup[data.Paths.Length];
            for (var i = 0; i < data.Paths.Length; i++)
            {
                setups[i] = new SceneSetup
                {
                    path = data.Paths[i],
                    isLoaded = true,
                    isActive = data.Paths[i] == data.ActivePath
                };
            }

            EditorSceneManager.RestoreSceneManagerSetup(setups);
        }

        [Serializable]
        private sealed class SceneSetupData
        {
            public string[] Paths = Array.Empty<string>();
            public string ActivePath = string.Empty;

            public SceneSetupData() { }

            public SceneSetupData(SceneSetup[] setup)
            {
                Paths = new string[setup.Length];
                for (var i = 0; i < setup.Length; i++)
                {
                    Paths[i] = setup[i].path;
                    if (setup[i].isActive)
                        ActivePath = setup[i].path;
                }
            }
        }
    }
}
