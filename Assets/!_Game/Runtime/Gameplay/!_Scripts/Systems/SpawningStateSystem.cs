using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawningStateSystem : SystemBase
{
    private EntityQuery _query;
    private bool _desiredState;

    protected override void OnCreate()
    {
        _query = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadWrite<SpawnEnemies>() },
            Options = EntityQueryOptions.IgnoreComponentEnabledState
        });
        Enabled = false;
    }

    public void SetDesiredState(bool isDay)
    {
        _desiredState = isDay;
        Enabled = true;
    }

    protected override void OnUpdate()
    {
        if (_query.IsEmpty) return;
        EntityManager.SetComponentEnabled<SpawnEnemies>(_query, _desiredState);
        Enabled = false;
    }
}
