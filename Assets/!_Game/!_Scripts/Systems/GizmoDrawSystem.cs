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
            Gizmos.color = Color.indianRed;
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

        foreach (var (transform, steeringContext, destination) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SteeringContext>, RefRO<Destination>>())
        {
            // Best direction
            Gizmos.color = Color.green;
            var offset = new float3(0, -0.4f, 0f);
            var lineStart = transform.ValueRO.Position + offset;
            var bestDirection = math.normalize(destination.ValueRO.Value - transform.ValueRO.Position);
            Gizmos.DrawLine(lineStart + bestDirection * 0.5f, lineStart + bestDirection * 1.5f);
            
            //Interests
            Gizmos.color = Color.olive;
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = Color.olive
                },
                fontSize = 14
            };
            lineStart += new float3(0, 0.1f, 0);
            var debugText = "";
            for (int interest = 0; interest < 8; interest++)
            {
                var direction = SteeringConstants.Directions[interest];
                var startPoint = lineStart + direction * 0.2f;
                var endPoint = lineStart + direction * math.clamp(steeringContext.ValueRO.Interest[interest], 0.25f, 1f);
                Gizmos.DrawLine(startPoint, endPoint);
                
                debugText = SteeringConstants.DirectionsText[interest];
                Handles.Label(endPoint + direction * 0.1f, debugText, style);
            }

            var temp = 0;
            //Dangers
            Gizmos.color = Color.red;
            style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = Color.red
                },
                fontSize = 14
            };
            lineStart += new float3(0, 0.1f, 0);
            for (int danger = 0; danger < 8; danger++)
            {
                var direction = SteeringConstants.Directions[danger];
                var startPoint = lineStart + direction * 0.2f;
                var endPoint = lineStart + direction * math.clamp(steeringContext.ValueRO.Danger[danger], 0.3f, 1f);
                Gizmos.DrawLine(startPoint, endPoint);
                
                debugText = SteeringConstants.DirectionsText[danger];
                Handles.Label(endPoint + direction * 0.1f, debugText, style);
            }
        }

        foreach (var (transform, obstacle, context) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SteerBehavior_Obstacle>, RefRO<SteeringContext>>())
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.ValueRO.Position, context.ValueRO.AgentRadius);
            
            for (int i = 0; i < 8; i++)
            {
                var dir = SteeringConstants.Directions[i];
                var isFrontDirection = math.dot(dir, transform.ValueRO.Forward()) > 0.5f;
                var scanRange = isFrontDirection ? obstacle.ValueRO.SurroundRadius + obstacle.ValueRO.VisionDistance : obstacle.ValueRO.SurroundRadius;
                
                Gizmos.DrawWireSphere(transform.ValueRO.Position + dir * scanRange, context.ValueRO.AgentRadius);
            }
        }
     
        // More ForEach queries as needed ...
    }

    private static void DrawLine(float3 lineStart, float3 direction, float minLenght, float maxLenght, float startOffset = 0)
    {
        
    }

    protected override void OnUpdate()
    {
        // Intentionally empty.
    }
#endif
    
}