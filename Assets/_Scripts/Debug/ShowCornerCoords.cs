using TMPro;
using UnityEngine;

public class ShowCornerCoords : MonoBehaviour
{
	void Start()
	{
		Vector3 bottomLeft = CoreHelper.camera.ViewportToWorldPoint(Vector3.zero);
		Vector3 topRight = CoreHelper.camera.ViewportToWorldPoint(Vector3.one);

		transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = VectorToString(bottomLeft.x, topRight.y);
		transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = VectorToString(topRight.x, topRight.y);
		transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = VectorToString(bottomLeft.x, bottomLeft.y);
		transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = VectorToString(topRight.x, bottomLeft.y);
	}

	private string VectorToString(float x, float y)
	{
		return $"[x:{x.ToString("0.00")},y:{y.ToString("0.00")}]";
	}
}
