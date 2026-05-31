#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BarkingBird.Runtime.Infrastructure.SceneWorkflow
{
    [CreateAssetMenu(fileName = "SceneChain", menuName = "BarkingBird/Scene Chain")]
    public sealed class SceneChain : ScriptableObject
    {
        [SerializeField] private List<SceneChainElement> _scenes = new();

        private List<string>? _sceneNamesCache;
        private List<string>? _scenePathsCache;

        public IReadOnlyList<string> SceneNames => _sceneNamesCache ??= BuildNames();
        public IReadOnlyList<string> ScenePaths => _scenePathsCache ??= BuildPaths();

        public bool ContainsScene(string sceneName)
        {
            for (var i = 0; i < _scenes.Count; i++)
                if (_scenes[i].SceneName == sceneName) return true;
            return false;
        }

        private List<string> BuildNames()
        {
            var list = new List<string>(_scenes.Count);
            for (var i = 0; i < _scenes.Count; i++) 
                list.Add(_scenes[i].SceneName);
            return list;
        }

        private List<string> BuildPaths()
        {
            var list = new List<string>(_scenes.Count);
            for (var i = 0; i < _scenes.Count; i++) 
                list.Add(_scenes[i].ScenePath);
            return list;
        }

        [Serializable]
        public sealed class SceneChainElement
        {
#if UNITY_EDITOR
            [SerializeField] private SceneAsset? _sceneAsset;

            internal void SyncFromAsset()
            {
                if (_sceneAsset == null) return;
                
                SceneName = _sceneAsset.name;
                ScenePath = AssetDatabase.GetAssetPath(_sceneAsset);
            }
#endif

            public string SceneName { get; private set; } = string.Empty;

            public string ScenePath { get; private set; } = string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sceneNamesCache = null;
            _scenePathsCache = null;
            for (var i = 0; i < _scenes.Count; i++)
                _scenes[i].SyncFromAsset();
        }
#endif
    }
}
