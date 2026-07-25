namespace BarkingBird.Runtime.Gameplay.Beacon
{
    /// <summary>
    /// World-scoped, save-backed home for the Beacon Core's free-countdown timer. It exists so the
    /// Battle-scoped <see cref="BeaconCoreController"/> and the World-scoped <c>WorldSaveService</c> can share
    /// the timer across the scope boundary (a parent scope cannot inject a child, so the controller cannot be
    /// pulled directly): the controller writes the live remaining each frame, the save service snapshots and
    /// hydrates it.
    ///
    /// Living in the World scope it also survives in-session Battle re-entry (the World scene stays loaded), so
    /// the countdown resumes without touching disk — which is what carries persistence today, since
    /// <c>DummySaveSystem</c> is in-memory.
    /// </summary>
    public sealed class BeaconCoreState
    {
        /// <summary>Seconds left until inserting the Core is free. Reset to the configured max on each drop.</summary>
        public float FreeCountdownRemaining;

        /// <summary>False on a brand-new world; the controller seeds the timer to the configured max on first run.</summary>
        public bool Initialized;
    }
}
