using System;

using UnityEngine;

using BarkingBird.Runtime.Gameplay.Currency;

namespace BarkingBird.Runtime.Gameplay.AI
{
    /// <summary>
    /// Inspector-authored row of an enemy's drop table. Baked into a <see cref="ResourceDrop"/>
    /// buffer element. Kept as a standalone serializable struct so it can be reused by the
    /// per-unit-type config SO when the config hub lands.
    /// </summary>
    [Serializable]
    public struct DropTableEntry
    {
        [Tooltip("Resource granted when one of these pickups is collected.")]
        public CurrencyType type;
        [Tooltip("Minimum number of pickups spawned when this row hits.")]
        public int minCount;
        [Tooltip("Maximum number of pickups spawned when this row hits (inclusive).")]
        public int maxCount;
        [Range(0f, 1f), Tooltip("Independent chance this row drops at all. 1 = always (e.g. pearls).")]
        public float chance;
        [Tooltip("Wallet amount granted per pickup.")]
        public float value;
    }
}
