using System;
using UnityEditor;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Settings
{
#if UNITY_EDITOR
    public class GizmoSystemHandler : MonoBehaviour
    {
        [Header("Gizmo Toggles")]
        [SerializeField] public bool showUnitState = true;
        [SerializeField] public bool showAttackRange = true;
        [SerializeField] public bool showTarget = true;
        [SerializeField] public bool showDestination = true;
        [SerializeField] public bool showSteeringContext = true;
        [SerializeField] public bool showSteerObstacle = true;
        [SerializeField] public bool showFinalDestination = true;

        public Action DrawGizmos;
        public Action DrawGizmosSelected;

        private void OnDrawGizmos()
        {
            if (EditorApplication.isPlaying)
            {
                DrawGizmos?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (EditorApplication.isPlaying)
            {
                DrawGizmosSelected?.Invoke();
            }
        }
    }
    
    public static class GizmoManager
    {
        public static void OnDrawGizmos(Action action)
        {
            Handler.DrawGizmos += action;
        }

        public static GizmoSystemHandler Handler => _handler != null ? _handler : (_handler = findOrCreateHandler());
        private static GizmoSystemHandler _handler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            _handler = null;
        }

        private static GizmoSystemHandler findOrCreateHandler()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<GizmoSystemHandler>();
            if (existing != null) return existing;

            var go = new GameObject("Gizmo Handler") { hideFlags = HideFlags.DontSave };
            return go.AddComponent<GizmoSystemHandler>();
        }

    }
#endif
}
