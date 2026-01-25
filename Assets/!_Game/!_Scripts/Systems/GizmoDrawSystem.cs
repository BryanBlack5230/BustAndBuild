using GameEngine.AI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public partial class GizmoDrawSystem : SystemBase
{
    
#if UNITY_EDITOR
    protected override void OnStartRunning()
    {
        // Connect system to handler.
        GizmoManager.OnDrawGizmos(DrawGizmos);
    }

    private void DrawGizmos()
    {
        foreach (var (transform, actionState, emotionState) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<ActionState>, RefRO<EmotionalState>>())
        {
            var color = actionState.ValueRO.Value == ActionType.Attacking ? Color.red : Color.black; 
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = color
                },
                fontSize = 14
            };

            var textPosition = transform.ValueRO.Position + new float3(0, 1.5f, 0);
            var debugText = $"Action: {actionState.ValueRO.Value}\nEmotion: {emotionState.ValueRO.Value}";
            Handles.Label(textPosition, debugText, style);
        }
        
        foreach (var (transform, attackData) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<AttackData>>())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.ValueRO.Position, attackData.ValueRO.AttackRange);
        }

        foreach (var (transform, target) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Target>>())
        {
            if (target.ValueRO.TargetEntity == Entity.Null) continue;
            var targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.TargetEntity);
            
            var distsq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);
            var dist = math.distance(transform.ValueRO.Position, targetTransform.Position);
            var direction = math.normalize(targetTransform.Position - transform.ValueRO.Position);
            var offset = new float3(0, 0.1f, 0);
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(transform.ValueRO.Position + offset, transform.ValueRO.Position  + offset + direction * dist);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.ValueRO.Position, transform.ValueRO.Position + direction * distsq);
        }

        foreach (var (transform, destination) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Destination>>())
        {
            Gizmos.color = Color.cyan;
            var radius = math.max(0.1f, destination.ValueRO.StoppingDistanceSq);
            var offset = new float3(0, -0.5f, 0f);
            Gizmos.DrawWireSphere(destination.ValueRO.Value + offset, radius);
            Gizmos.DrawLine(transform.ValueRO.Position + offset, destination.ValueRO.Value + offset - destination.ValueRO.StoppingDistanceSq);
        }
     
        // More ForEach queries as needed ...
    }

    protected override void OnUpdate()
    {
        // Intentionally empty.
    }
#endif
    
}