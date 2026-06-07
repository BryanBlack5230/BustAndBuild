using Unity.Entities;
using UnityEngine;

public class PearlSpawnerAuthoring : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject pearlPrefab;

    [Header("Lifetime")]
    [SerializeField, Tooltip("Total seconds pearl lives before disappearing.")]
    private float lifetime = 10f;
    [SerializeField, Tooltip(("Percent of lifetime that pearl will blink for.")), Range(0f, 1f)]
    private float blinkPercent = 0.15f;
    [SerializeField, Tooltip("Toggle interval while blinking.")]
    private float blinkInterval = 0.15f;

    [Header("Pickup")]
    [SerializeField, Tooltip("World-space distance from cursor that triggers pickup.")]
    private float pickupRadius = 1.5f;

    [Header("Spawn")]
    [SerializeField, Tooltip("XZ scatter radius around the source position when spawning.")]
    private float scatter = 0.6f;
    [SerializeField, Tooltip("Y position offset added to source spawn position.")]
    private float spawnHeight = 0.5f;

    [Header("Float")]
    [SerializeField, Tooltip("Vertical bob amplitude in meters once settled.")]
    private float floatAmplitude = 0.1f;
    [SerializeField, Tooltip("Seconds per full bob cycle.")]
    private float floatPeriod = 2.5f;
    [SerializeField, Tooltip("Linear speed below which a disturbed pearl starts settling.")]
    private float restSpeedThreshold = 0.1f;
    [SerializeField, Tooltip("Seconds the pearl must stay below rest speed before re-entering float state.")]
    private float restDuration = 0.1f;

    public class Baker : Baker<PearlSpawnerAuthoring>
    {
        public override void Bake(PearlSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            var prefabEntity = Entity.Null;
            if (authoring.pearlPrefab != null)
            {
                if (authoring.pearlPrefab.GetComponent<PearlAuthoring>() == null)
                {
                    Debug.LogError(
                        $"[PearlSpawnerAuthoring] Assigned prefab '{authoring.pearlPrefab.name}' is missing a PearlAuthoring component. " +
                        "Pearl spawning is disabled until a PearlAuthoring is attached to the prefab.",
                        authoring);
                }
                else
                {
                    prefabEntity = GetEntity(authoring.pearlPrefab, TransformUsageFlags.Dynamic);
                }
            }

            AddComponent(entity, new PearlSpawnPrefab { Prefab = prefabEntity });

            var blinkStart = Mathf.Max(0f, authoring.lifetime * authoring.blinkPercent);

            AddComponent(entity, new PearlSettings
            {
                PickupRadius = authoring.pickupRadius,
                Lifetime = authoring.lifetime,
                BlinkStart = blinkStart,
                BlinkInterval = authoring.blinkInterval,
                Scatter = authoring.scatter,
                SpawnHeight = authoring.spawnHeight,
                FloatAmplitude = authoring.floatAmplitude,
                FloatPeriod = Mathf.Max(0.01f, authoring.floatPeriod),
                RestSpeedThreshold = Mathf.Max(0f, authoring.restSpeedThreshold),
                RestDuration = Mathf.Max(0f, authoring.restDuration),
            });
        }
    }
}
