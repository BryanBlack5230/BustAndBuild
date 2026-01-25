using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class GizmoSystemHandler : MonoBehaviour
{
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

    private static GizmoSystemHandler Handler => _handler != null ? _handler : (_handler = createHandler());
    private static GizmoSystemHandler _handler;

    private static GizmoSystemHandler createHandler()
    {
        var go = new GameObject("Gizmo Handler") { hideFlags = HideFlags.DontSave };

        return go.AddComponent<GizmoSystemHandler>();
    }

}
#endif