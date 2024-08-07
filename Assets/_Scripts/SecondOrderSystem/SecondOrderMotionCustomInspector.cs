using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SecondOrderMotion))]
public class SecondOrderMotionCustomInspector : Editor
{
    private Keyframe[] _keyframes = new Keyframe[60];
    private AnimationCurve _curve;
    private float f, r, z = float.PositiveInfinity;

    // Update is called once per frame
    public override void OnInspectorGUI()
    {
        SecondOrderMotion t = (SecondOrderMotion)target;
        DrawDefaultInspector();

        GUILayout.Space(10);
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(128));
        float tmax = 2f;
        float dt = Time.fixedDeltaTime;
        float[] y = new float[(int)(tmax / Time.fixedDeltaTime)];
        SecondOrderDynamics dyn = new SecondOrderDynamics(t.F, t.Z, t.R, 0);
        for (int i = 1; i < y.Length; i++)
        {
            y[i] = dyn.Update(dt, 1);
        }
        float ymin = Mathf.Min(0, Mathf.Min(y));
        float ymax = Mathf.Max(1, Mathf.Max(y));

        Vector2 c2p(float t, float x)
        {
            return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t / tmax),
            Mathf.Lerp(rect.yMax, rect.yMin, (x - ymin) / (ymax - ymin)));
        }

        Handles.BeginGUI();
        Handles.DrawLine(c2p(0,0), c2p(tmax, 0));
        Handles.DrawLine(c2p(0, ymin), c2p(0, ymax));
        Handles.color = Color.green;
        Handles.DrawLine(c2p(0, 1), c2p(tmax, 1));
        for (int i = 1; i < y.Length; i++)
        {
            Handles.color = Color.cyan;
            Handles.DrawLine(c2p(Time.fixedDeltaTime * (i - 1), y[i - 1]),
            c2p(Time.fixedDeltaTime * i, y[i]));
        }
        Handles.EndGUI();
        GUI.Label(new Rect(c2p(0,0) + Vector2.left * 12, Vector2.one * 12), "0");
        GUI.Label(new Rect(c2p(0,0) + Vector2.left * 12 + Vector2.down * 6, Vector2.one * 12), "1");
        GUI.Label(new Rect(c2p(tmax, 0) - new Vector2(64, 0), new Vector2(64, 32)), tmax.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight }); 
        EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(32));
        
    }
}
