using System.Collections.Generic;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;

/// <summary>
/// One profile asset per <see cref="EnemyType"/>. The baker places <see cref="Targeting"/>/<see cref="Combat"/>
/// at the runtime blob slot matching <see cref="Type"/> (live, Rebake-able) and reads <see cref="Stats"/>/
/// <see cref="Drops"/> at subscene-bake time into the prefab (edit-time re-bake).
/// </summary>
[CreateAssetMenu(fileName = "EnemyProfile", menuName = "Game/Units/Enemy Profile")]
public sealed class EnemyUnitProfile : ScriptableObject, IUnitProfile
{
    [Tooltip("Which enemy type this profile configures. Must be unique within the hub's enemy list.")]
    public EnemyType Type;
    public TargetingProfile Targeting;
    public CombatProfile Combat;
    public UnitStats Stats;

    [Tooltip("Resources this enemy type drops on death. Each row is rolled independently.")]
    public List<DropTableEntry> Drops = new();

    int IUnitProfile.TypeValue => (int)Type;
    string IUnitProfile.TypeLabel => Type.ToString();
    TargetingProfile IUnitProfile.Targeting => Targeting;
    CombatProfile IUnitProfile.Combat => Combat;
    UnitStats IUnitProfile.Stats => Stats;
}
