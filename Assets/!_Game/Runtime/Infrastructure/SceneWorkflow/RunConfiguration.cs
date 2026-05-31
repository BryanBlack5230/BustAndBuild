#nullable enable

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.SceneWorkflow
{
    [CreateAssetMenu(fileName = "RunConfiguration", menuName = "BarkingBird/Run Configuration")]
    public sealed class RunConfiguration : ScriptableObject
    {
        [HorizontalGroup("Flags")]
        [ShowIf("_isDefault")]
        [GUIColor(0.5f, 1f, 1f)]
        [ReadOnly]
        [SerializeField] private bool _isDefault;

        [HorizontalGroup("Flags")]
        [ShowIf("_isBuildConfig")]
        [GUIColor(1f, 0.7f, 0f)]
        [ReadOnly]
        [SerializeField] private bool _isBuildConfig;

        [Space(height:20)]
        [AssetList]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed), SerializeField] private SceneChain _chain = null!;

        [Space(height:20)]
        [SerializeReference] private List<StateOverride> _overrides = new();

        public SceneChain Chain => _chain != null ? _chain : throw new System.InvalidOperationException($"RunConfiguration '{name}' has no SceneChain assigned.");
        public bool IsDefault => _isDefault;
        public bool IsBuildConfig => _isBuildConfig;
        public IReadOnlyList<StateOverride> Overrides => _overrides;

#if UNITY_EDITOR
        [PropertyOrder(-1)]
        [Button("Set Default")]
        private void SetDefault()
        {
            _isDefault = true;
            UnityEditor.EditorUtility.SetDirty(this);

            var guids = UnityEditor.AssetDatabase.FindAssets("t:RunConfiguration");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var other = UnityEditor.AssetDatabase.LoadAssetAtPath<RunConfiguration>(path);
                if (other == null || other == this || other._chain != _chain) continue;
                other._isDefault = false;
                UnityEditor.EditorUtility.SetDirty(other);
            }

            UnityEditor.AssetDatabase.SaveAssets();
        }

        [PropertyOrder(-1)]
        [Button("Set Build Config")]
        private void SetBuildConfig()
        {
            _isBuildConfig = true;
            UnityEditor.EditorUtility.SetDirty(this);

            var guids = UnityEditor.AssetDatabase.FindAssets("t:RunConfiguration");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var other = UnityEditor.AssetDatabase.LoadAssetAtPath<RunConfiguration>(path);
                if (other == null || other == this) continue;
                other._isBuildConfig = false;
                UnityEditor.EditorUtility.SetDirty(other);
            }

            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}