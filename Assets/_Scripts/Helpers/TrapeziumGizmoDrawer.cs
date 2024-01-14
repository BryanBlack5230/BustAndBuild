using UnityEngine;
using UnityEditor;

public class TrapeziumGizmoDrawer : MonoBehaviour
{
	[SerializeField, Range(0, 90)] private float observerAngle = 30f;
	[SerializeField, Range(0, 1)] private float screenPercent = 0.3f;
	[SerializeField, Range(0, 1)] private float colorBrightness = 0.6f;
	[SerializeField] private bool printModifier = false;
	[SerializeField] private Transform pointer;

	private void Start() 
	{
		CoreHelper.SetAngleAndGroundSize(observerAngle, screenPercent);
		CoreHelper.RecalcScreenSize();
	}

	private void OnDrawGizmos()
	{
		DrawTrapeziumGizmo();

		if (pointer != null && printModifier)
		{
			print("calculations " + CoreHelper.GetDepthModifier(pointer.position.y) + "  bottom coord: " + pointer.position.y);
			string pointPos = "(" + pointer.position.x.ToString("0.00") + "," + pointer.position.y.ToString("0.00") + ")";
			DrawString(pointPos, pointer.position);
		}
	}

	private void DrawTrapeziumGizmo()
	{
		float x = transform.position.x;
		float y = transform.position.y;
		float z = transform.position.z;

		float angleRad = observerAngle * Mathf.Deg2Rad;
		float width = CoreHelper.groundTopY - CoreHelper.groundBottomY;
		CoreHelper.SetAngleAndGroundSize(observerAngle, screenPercent);

		Vector3 point1 = new Vector3(x - CoreHelper.screenLength / 2 - width * Mathf.Tan(angleRad), y, z);
		Vector3 point2 = new Vector3(x + CoreHelper.screenLength / 2 + width * Mathf.Tan(angleRad), y, z);
		Vector3 point3 = new Vector3(x + CoreHelper.screenLength / 2, y + width, z);
		Vector3 point4 = new Vector3(x - CoreHelper.screenLength / 2, y + width, z);

		Gizmos.color = Color.red;
		Gizmos.DrawLine(point1, point2);
		Gizmos.color = Color.red * colorBrightness;
		Gizmos.DrawLine(point2, point3);
		Gizmos.color = Color.green * colorBrightness;
		Gizmos.DrawLine(point3, point4);
		Gizmos.color = Color.green;
		Gizmos.DrawLine(point4, point1);

		Gizmos.color = Color.white * colorBrightness * 0.5f;
		Gizmos.DrawLine(point1, point3);
		Gizmos.DrawLine(point2, point4);

		Gizmos.color = Color.white;
		Gizmos.DrawLine(CoreHelper.GetWallPos(CoreHelper.groundTopY), CoreHelper.GetWallPos(CoreHelper.groundBottomY));
	}

	public static void DrawString(string text, Vector3 worldPos, Color? textColor = null, Color? backColor = null)
	{
		Handles.BeginGUI();
		var restoreTextColor = GUI.color;
		var restoreBackColor = GUI.backgroundColor;

		GUI.color = textColor ?? Color.black;
		GUI.backgroundColor = backColor ?? Color.clear;

		var view = SceneView.currentDrawingSceneView;
		if (view != null && view.camera != null)
		{
			Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
			if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
			{
				GUI.color = restoreTextColor;
				Handles.EndGUI();
				return;
			}
			Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
			var r = new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y);
			GUI.Box(r, text, EditorStyles.numberField);
			GUI.Label(r, text);
			GUI.color = restoreTextColor;
			GUI.backgroundColor = restoreBackColor;
		}
		Handles.EndGUI();
	}
}
