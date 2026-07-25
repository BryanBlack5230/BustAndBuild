using System;

namespace BarkingBird.Runtime.Gameplay.GameWorld
{
    /// <summary>
    /// Serializable snapshot of a single world/slot. One file per world once real
    /// persistence lands. <see cref="Version"/> is included from day one so future
    /// schema changes can migrate old saves. Currencies are stored as a raw int[] indexed
    /// by CurrencyType — this DTO stays enum-agnostic so the save model doesn't reference
    /// the gameplay enum; the Wallet owns the enum↔index mapping.
    /// </summary>
    [Serializable]
    public sealed class WorldSaveData
    {
        // v2 added the Beacon Core free-countdown fields. Old (v1) saves deserialize them as their
        // defaults (Initialized=false), which the controller reads as "fresh world" — a graceful no-migration upgrade.
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public string WorldId;
        public DateTime CreatedUtc;
        public DateTime LastPlayedUtc;
        public int[] Currencies;

        public float BeaconCoreFreeCountdown;
        public bool BeaconCoreInitialized;

        // Future fields: day number, built structures, wall HP, etc.
    }
}
