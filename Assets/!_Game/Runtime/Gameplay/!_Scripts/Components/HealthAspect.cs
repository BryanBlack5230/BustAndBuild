using Unity.Entities;
using Unity.Mathematics;

public readonly partial struct HealthAspect : IAspect
{
    public readonly Entity Self;

    private readonly RefRW<Health> _health;
    private readonly DynamicBuffer<DamageBufferElement> _damageBuffer;
    private readonly EnabledRefRW<IsDead> _isDead;

    [Optional] private readonly EnabledRefRW<IsInvulnerable> _isInvulnerable;

    public float Value             => _health.ValueRO.Value;
    public float Max               => _health.ValueRO.Max;
    public bool  IsDead            => _isDead.ValueRO;
    public bool  IsInvulnerable    => _isInvulnerable.IsValid && _isInvulnerable.ValueRO;
    public bool  HasPendingDamage  => _damageBuffer.Length > 0;

    public void DrainBufferedDamage()
    {
        if (_isDead.ValueRO) return;
        if (_damageBuffer.IsEmpty) return;

        var total = 0f;
        var array = _damageBuffer.AsNativeArray();
        for (var i = 0; i < array.Length; i++) total += array[i].Value;

        var projected = _health.ValueRO.Value - total;
        float next;

        if (_isInvulnerable.IsValid && projected < 1f)
        {
            next = 1f;
            if (!_isInvulnerable.ValueRO) _isInvulnerable.ValueRW = true;
        }
        else
        {
            next = math.max(projected, 0f);
        }

        _health.ValueRW.Value = next;
        _damageBuffer.Clear();

        if (next <= 0f) _isDead.ValueRW = true;
    }
}
