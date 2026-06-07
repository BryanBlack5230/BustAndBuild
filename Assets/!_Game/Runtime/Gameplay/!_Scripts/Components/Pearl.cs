using Unity.Entities;

public struct Pearl : IComponentData
{
    public float Value;
}

public struct PearlLifetime : IComponentData
{
    public float TimeRemaining;
    public float NextBlinkToggleAt;
    public byte VisibleState;
}

public struct PearlBlinking : IComponentData, IEnableableComponent { }

public struct PearlBeingCollected : IComponentData, IEnableableComponent { }

public struct PearlSettled : IComponentData, IEnableableComponent { }

public struct PearlFloat : IComponentData
{
    public float Amplitude;
    public float Period;
    public float PhaseOffset;
    public float RestY;
    public float RestTimer;
}

public struct PearlSpawnPrefab : IComponentData
{
    public Entity Prefab;
}

public struct PearlSettings : IComponentData
{
    public float PickupRadius;
    public float Lifetime;
    public float BlinkStart;
    public float BlinkInterval;
    public float Scatter;
    public float SpawnHeight;
    public float FloatAmplitude;
    public float FloatPeriod;
    public float RestSpeedThreshold;
    public float RestDuration;
}
