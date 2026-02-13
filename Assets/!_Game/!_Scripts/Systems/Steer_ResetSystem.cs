using Unity.Entities;

[UpdateInGroup(typeof(SteeringSystemGroup), OrderFirst = true)]
public partial struct Steer_ResetSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var context in SystemAPI.Query<RefRW<SteeringContext>>())
        {
            context.ValueRW.Interest = default;
            context.ValueRW.Danger = default;
        }
    }
}