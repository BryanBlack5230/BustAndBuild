using Unity.Entities;

using BarkingBird.Runtime.Gameplay.Currency;

namespace BarkingBird.Runtime.Gameplay.AI
{
    /// <summary>
    /// One row of an enemy's drop table, baked onto the enemy as a buffer. Each row is rolled
    /// independently on death: with probability <see cref="Chance"/>, spawn
    /// [<see cref="MinCount"/>, <see cref="MaxCount"/>] pickups of <see cref="Type"/>, each worth
    /// <see cref="Value"/>.
    /// </summary>
    public struct ResourceDrop : IBufferElementData
    {
        public CurrencyType Type;
        public int MinCount;
        public int MaxCount;
        public float Chance;
        public float Value;
    }
}
