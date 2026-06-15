using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;

/// <summary>
/// One profile asset per <see cref="AllyType"/>. The baker places <see cref="Targeting"/>/<see cref="Combat"/>
/// at the runtime blob slot matching <see cref="Type"/> (live, Rebake-able) and reads <see cref="Stats"/>
/// at subscene-bake time into the prefab (edit-time re-bake).
/// </summary>
[CreateAssetMenu(fileName = "AllyProfile", menuName = "Game/Units/Ally Profile")]
public sealed class AllyUnitProfile : ScriptableObject, IUnitProfile
{
    [Tooltip("Which ally type this profile configures. Must be unique within the hub's ally list.")]
    public AllyType Type;
    public TargetingProfile Targeting;
    public CombatProfile Combat;
    public UnitStats Stats;

    int IUnitProfile.TypeValue => (int)Type;
    string IUnitProfile.TypeLabel => Type.ToString();
    TargetingProfile IUnitProfile.Targeting => Targeting;
    CombatProfile IUnitProfile.Combat => Combat;
    UnitStats IUnitProfile.Stats => Stats;
}
