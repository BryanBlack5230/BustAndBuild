using System;
using System.Collections.Generic;

using BarkingBird.Runtime.Gameplay.GameWorld;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Save
{
    /// <summary>
    /// In-memory <see cref="ISaveSystem"/> placeholder. Holds slots in a dictionary for
    /// the lifetime of the app — survives world re-entry within a session, lost on
    /// restart. Wires the full hydrate/flush path so the real file-backed implementation
    /// is a drop-in replacement. NOT persisted to disk.
    /// </summary>
    public sealed class DummySaveSystem : ISaveSystem
    {
        private readonly Dictionary<string, WorldSaveData> _slots = new();

        public WorldSaveData Load(string worldId)
        {
            if (_slots.TryGetValue(worldId, out var existing))
                return existing;

            var fresh = new WorldSaveData
            {
                Version = WorldSaveData.CurrentVersion,
                WorldId = worldId,
                CreatedUtc = DateTime.UtcNow,
                LastPlayedUtc = DateTime.UtcNow,
                Currencies = null,
            };
            _slots[worldId] = fresh;
            Log.Loading.D($"DummySaveSystem created in-memory slot '{worldId}'");
            return fresh;
        }

        public void Save(WorldSaveData data)
        {
            _slots[data.WorldId] = data;
            Log.Default.W($"DummySaveSystem.Save('{data.WorldId}') — in-memory only, NOT persisted to disk");
        }
    }
}
