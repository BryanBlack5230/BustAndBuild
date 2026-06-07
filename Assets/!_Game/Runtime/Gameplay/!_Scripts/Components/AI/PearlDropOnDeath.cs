using Unity.Entities;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public struct PearlDropOnDeath : IComponentData
    {
        public int MinCount;
        public int MaxCount;
        public float ValuePerPearl;
    }
}
