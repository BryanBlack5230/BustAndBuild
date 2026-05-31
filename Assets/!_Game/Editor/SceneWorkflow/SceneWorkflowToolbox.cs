#nullable enable

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.SceneWorkflow;

namespace BarkingBird.Editor
{
    internal sealed class SceneWorkflowToolbox : EditorWindow
    {
        private RunConfiguration[] _configs = Array.Empty<RunConfiguration>();
        private int? _selectedIndex;
        private bool _isSingleScene;
        private bool _isShowingPreview;
        private UnityEditor.Editor? _configEditor;
        private Vector2 _previewScroll;

        [MenuItem("BarkingBird/Scenes/Scene Workflow %#w", priority = 201)]
        internal static void ShowWindow() => GetWindow<SceneWorkflowToolbox>("Scene Workflow");

        private void OnEnable()
        {
            RefreshConfigs();
            RestorePrefs();
        }

        private void OnDisable()
        {
            if (_configEditor == null) return;
            DestroyImmediate(_configEditor);
            _configEditor = null;
        }

        private void OnFocus() => RefreshConfigs();

        private void OnGUI()
        {
            DrawConfigPicker();
            DrawModeToggle();
            EditorGUILayout.Separator();
            EditorGUILayout.Space(8f);
            DrawValidation();
            EditorGUILayout.Space(4f);
            DrawConfigPreview();
        }

        private static readonly Color SelectedColor = new Color(0.33f, 0.34f, 0.73f);
        private static readonly GUILayoutOption LocateButtonWidth = GUILayout.Width(55f);

        private void DrawConfigPicker()
        {
            EditorGUILayout.LabelField("Choose configuration:", EditorStyles.boldLabel);

            if (_configs.Length == 0)
            {
                EditorGUILayout.HelpBox("No RunConfigurations found. Play will run the current scene.", MessageType.Info);
                return;
            }

            var prevColor = GUI.backgroundColor;

            for (var i = 0; i < _configs.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = _selectedIndex == i ? SelectedColor : prevColor;
                if (GUILayout.Button(_configs[i].name))
                {
                    _selectedIndex = i;
                    GUI.backgroundColor = prevColor;
                    SavePrefs();
                    UpdateHandoff();
                    RebuildEditor();
                }

                GUI.backgroundColor = prevColor;
                if (GUILayout.Button("Locate", LocateButtonWidth))
                {
                    Selection.activeObject = _configs[i];
                    EditorGUIUtility.PingObject(_configs[i]);
                }

                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = prevColor;
        }

        private void DrawModeToggle()
        {
            EditorGUI.BeginChangeCheck();
            _isSingleScene = EditorGUILayout.Toggle(
                new GUIContent("Single Scene Mode", "Load Bootstrap + current scene only, skipping intermediate scenes."),
                _isSingleScene);
            if (!EditorGUI.EndChangeCheck()) return;

            SavePrefs();
            UpdateHandoff();
        }

        private void DrawConfigPreview()
        {
            var config = GetEffectiveConfig();
            if (config == null) return;

            EditorGUI.BeginChangeCheck();
            _isShowingPreview = EditorGUILayout.Foldout(_isShowingPreview, "Configuration Preview", true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(EditorConstants.SceneWorkflow.Toolbox.ShowPreview, _isShowingPreview);

            if (!_isShowingPreview) return;

            if (_configEditor == null || _configEditor.target != config)
                RebuildEditor();

            if (_configEditor == null) return;

            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll);
            _configEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void DrawValidation()
        {
            var config = GetEffectiveConfig();
            if (config == null)
            {
                EditorGUILayout.HelpBox(
                    $"Current scene '{EditorSceneManager.GetActiveScene().name}' is not in any RunConfiguration chain.",
                    MessageType.Warning);
                return;
            }

            var buildScenes = EditorBuildSettings.scenes;
            for (var i = 0; i < config.Chain.ScenePaths.Count; i++)
            {
                if (!IsInBuildSettings(buildScenes, config.Chain.ScenePaths[i]))
                    EditorGUILayout.HelpBox($"'{config.Chain.SceneNames[i]}' is not in Build Settings.", MessageType.Warning);
            }
        }

        private void UpdateHandoff()
        {
            var config = GetEffectiveConfig();
            if (config == null) return;

            var currentScene = EditorSceneManager.GetActiveScene().name;
            var target = _isSingleScene ? currentScene : string.Empty;
            SceneWorkflowHandoff.Set(config, _isSingleScene, target);
        }

        private void RebuildEditor()
        {
            if (_configEditor != null)
                DestroyImmediate(_configEditor);

            var config = GetEffectiveConfig();
            _configEditor = config != null ? UnityEditor.Editor.CreateEditor(config) : null;
        }

        private RunConfiguration? GetEffectiveConfig()
        {
            if (_selectedIndex.HasValue && _selectedIndex.Value < _configs.Length)
                return _configs[_selectedIndex.Value];

            return SceneWorkflowHandoff.AutoResolveConfig(EditorSceneManager.GetActiveScene().name);
        }

        private void RefreshConfigs()
        {
            var guids = AssetDatabase.FindAssets("t:RunConfiguration");
            _configs = new RunConfiguration[guids.Length];

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _configs[i] = AssetDatabase.LoadAssetAtPath<RunConfiguration>(path)!;
            }

            if (_selectedIndex.HasValue && _selectedIndex.Value >= _configs.Length)
                _selectedIndex = null;
        }

        private void RestorePrefs()
        {
            _isSingleScene = EditorPrefs.GetBool(EditorConstants.SceneWorkflow.Toolbox.SingleScene, false);
            _isShowingPreview = EditorPrefs.GetBool(EditorConstants.SceneWorkflow.Toolbox.ShowPreview, false);
            _selectedIndex = null;

            var savedPath = EditorPrefs.GetString(EditorConstants.SceneWorkflow.Toolbox.ConfigPath, string.Empty);
            if (string.IsNullOrEmpty(savedPath)) return;

            for (var i = 0; i < _configs.Length; i++)
            {
                if (AssetDatabase.GetAssetPath(_configs[i]) != savedPath) continue;
                _selectedIndex = i;
                return;
            }
        }

        private void SavePrefs()
        {
            var path = _selectedIndex.HasValue && _selectedIndex.Value < _configs.Length
                ? AssetDatabase.GetAssetPath(_configs[_selectedIndex.Value])
                : string.Empty;
            EditorPrefs.SetString(EditorConstants.SceneWorkflow.Toolbox.ConfigPath, path);
            EditorPrefs.SetBool(EditorConstants.SceneWorkflow.Toolbox.SingleScene, _isSingleScene);
        }

        private static bool IsInBuildSettings(EditorBuildSettingsScene[] buildScenes, string path)
        {
            for (var i = 0; i < buildScenes.Length; i++)
                if (buildScenes[i].path == path) return true;
            return false;
        }
    }
}
