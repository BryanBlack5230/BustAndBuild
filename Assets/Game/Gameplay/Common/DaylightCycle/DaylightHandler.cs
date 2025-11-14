using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DaylightHandler : MonoBehaviour
{
    [SerializeField] private Gradient _sunLight;
    [SerializeField] private Light2D _globalLight2D;
    [SerializeField] private Light _globalLight3D;

    public void SetDaylightTo(float dayPercent)
    {
        var sunLight = _sunLight.Evaluate(dayPercent);
        _globalLight2D.color = sunLight;
        _globalLight3D.color = sunLight;
    }
}