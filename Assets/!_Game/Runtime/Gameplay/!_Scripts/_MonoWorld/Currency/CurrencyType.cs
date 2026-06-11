namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// All resource currencies. Order is persisted (saves index by enum value), so append
    /// new currencies at the end — never reorder or remove existing entries without a save
    /// migration.
    /// </summary>
    public enum CurrencyType
    {
        Pearls = 0,
        Food = 1,
        Wood = 2,
        Stone = 3,
        Iron = 4,
        Faith = 5,
    }
}
