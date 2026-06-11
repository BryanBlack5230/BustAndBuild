using BarkingBird.Runtime.Gameplay.GameWorld;

namespace BarkingBird.Runtime.Infrastructure.Save
{
    /// <summary>
    /// Persistence seam for per-world save slots. Currently fulfilled by
    /// <see cref="DummySaveSystem"/> (in-memory, no disk I/O). The real implementation
    /// (atomic JSON write to persistentDataPath/saves/world_&lt;id&gt;.json, slot
    /// enumeration, version migration) will drop in behind this interface unchanged.
    /// </summary>
    public interface ISaveSystem
    {
        /// <summary>Returns the existing slot for <paramref name="worldId"/>, or a fresh record if none exists.</summary>
        WorldSaveData Load(string worldId);

        /// <summary>Persists the given slot.</summary>
        void Save(WorldSaveData data);
    }
}
