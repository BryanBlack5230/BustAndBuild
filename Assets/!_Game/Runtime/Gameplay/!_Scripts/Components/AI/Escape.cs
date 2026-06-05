using Unity.Entities;
using Unity.Mathematics;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public struct Escaped : IComponentData, IEnableableComponent { }

    public struct HasLeftBase : IComponentData, IEnableableComponent
    {
        public float DwellTimer;
        public float3 LastOutsidePosition;
    }
}
