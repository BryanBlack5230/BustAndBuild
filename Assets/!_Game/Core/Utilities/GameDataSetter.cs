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
}