using UnityEngine;

public class GameDataSetter : MonoBehaviour 
{ 
    [SerializeField] private LayerMask _unitLayerMask;

    private void Awake()
    {
        // Debug.Log((int)_unitLayerMask);
        // GameData.UnitLayerMask = _unitLayerMask;
    }
}

public static class GameData
{
    public static readonly LayerMask UnitLayerMask = 6;
    public static readonly LayerMask GroundLayerMask = 7;
    public static readonly LayerMask GrabbableLayerMask = 8;
}