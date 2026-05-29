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
        var h = GizmoManager.Handler;
        if (h.showUnitState)        UnitStateGizmo();
        if (h.showAttackRange)      AttackRangeGizmo();
        if (h.showTarget)           TargetGizmo();
        if (h.showDestination)      DestinationGizmo();
        if (h.showSteeringContext)  SteeringContextGizmo();
        if (h.showSteerObstacle)    SteerObstacleGizmo();
        if (h.showFinalDestination) FinalDestinationGizmo();
    }

    private void FinalDestinationGizmo()
    {
        foreach (var (finalDestination, pathTarget) in SystemAPI.Query<RefRO<FinalDestination>, RefRO<PathTarget>>())
        {
            var arrowDirection = new float3(0f, -1f, 0f);
            Gizmos.color = Color.cyan;
            DrawArrowCone(pathTarget.ValueRO.Value, arrowDirection, 0.8f);
            
            Gizmos.color = Color.blue;
            DrawArrowCone(finalDestination.ValueRO.Value, arrowDirection, 1.5f);
        }
    }

    private void SteerObstacleGizmo()
    {
        foreach (var (transform, obstacle, context) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SteerBehavior_Obstacle>, RefRO<SteeringContext>>())
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.ValueRO.Position, context.ValueRO.AgentRadius);
            
            for (var i = 0; i < 8; i++)
            {
                var dir = SteeringConstants.GetDirection(i);
                var isFrontDirection = math.dot(dir, transform.ValueRO.Forward()) > 0.5f;
                var scanRange = isFrontDirection ? obstacle.ValueRO.SurroundRadius + obstacle.ValueRO.VisionDistance : obstacle.ValueRO.SurroundRadius;
                
                Gizmos.DrawWireSphere(transform.ValueRO.Position + dir * scanRange, context.ValueRO.AgentRadius);
            }
        }
    }

    private void SteeringContextGizmo()
    {
        foreach (var (transform, steeringContext) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SteeringContext>>())
        {
            // Best direction
            Gizmos.color = Color.green;
            var offset = new float3(0, -0.4f, 0f);
            var lineStart = transform.ValueRO.Position + offset;
            var bestDirection = steeringContext.ValueRO.BestDirection;
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
                var direction = SteeringConstants.GetDirection(interest);
                var startPoint = lineStart + direction * 0.2f;
                var endPoint = lineStart + direction * math.clamp(steeringContext.ValueRO.Interest[interest], 0.25f, 1f);
                Gizmos.DrawLine(startPoint, endPoint);
                
                debugText = SteeringConstants.DirectionsText[interest];
                Handles.Label(endPoint + direction * 0.1f, debugText, style);
            }

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
                var direction = SteeringConstants.GetDirection(danger);
                var startPoint = lineStart + direction * 0.2f;
                var endPoint = lineStart + direction * math.clamp(steeringContext.ValueRO.Danger[danger], 0.3f, 1f);
                Gizmos.DrawLine(startPoint, endPoint);
                
                debugText = SteeringConstants.DirectionsText[danger];
                Handles.Label(endPoint + direction * 0.1f, debugText, style);
            }
        }
    }

    private void DestinationGizmo()
    {
        foreach (var (transform, destination) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Destination>>())
        {
            Gizmos.color = Color.cyan;
            var stoppingDistance = math.sqrt(destination.ValueRO.StoppingDistanceSq);
            var size = math.max(0.1f, stoppingDistance);
            var offset = new float3(0, -0.5f, 0f);
            var direction = math.normalize(destination.ValueRO.Value - transform.ValueRO.Position);
            var destinationPos = destination.ValueRO.Value - direction * stoppingDistance;
            
            DrawDistance(transform.ValueRO.Position, destinationPos, Color.cyan, offset);
            Gizmos.DrawWireCube(destination.ValueRO.Value + offset, new float3(size, size, size));
        }
    }

    private void TargetGizmo()
    {
        foreach (var (worldTransform, target) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<Target>>())
        {
            if (target.ValueRO.TargetEntity == Entity.Null) continue;
            if (!SystemAPI.HasComponent<LocalToWorld>(target.ValueRO.TargetEntity)) continue;

            var targetTransform = SystemAPI.GetComponent<LocalToWorld>(target.ValueRO.TargetEntity);
            
            var dist = math.distance(worldTransform.ValueRO.Position, targetTransform.Position);
            var direction = math.normalize(targetTransform.Position - worldTransform.ValueRO.Position);
            var offset = new float3(0, 0.1f, 0);
            DrawDistance(worldTransform.ValueRO.Position, direction, dist, Color.orange, offset);
        }
    }

    private void AttackRangeGizmo()
    {
        foreach (var (transform, attackData) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<AttackData>>())
        {
            Gizmos.color = Color.indianRed;
            Gizmos.DrawWireSphere(transform.ValueRO.Position, attackData.ValueRO.AttackRange);
        }
    }

    private void UnitStateGizmo()
    {
        foreach (var (transform, actionState, emotionState) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<ActionState>, RefRO<EmotionalState>>())
        {
            var color = actionState.ValueRO.Value == ActionType.Attacking ? Color.red : Color.black; 
            var textPosition = transform.ValueRO.Position + new float3(0, 1.5f, 0);
            var debugText = $"Action: {actionState.ValueRO.Value}\nEmotion: {emotionState.ValueRO.Value}";
            
            DrawText(debugText, textPosition, color);
        }
    }

    private static void DrawDistance(float3 start, float3 destination, Color color, float3 offset = default)
    {
        var direction = math.normalize(destination - start);
        var dist = math.distance(start, destination);
        
        DrawDistance(start, direction, dist, color, offset);
    }

    private static void DrawDistance(float3 start, float3 direction, float dist, Color color, float3 offset = default)
    {
        var startPoint = start + offset;
        var endPoint = startPoint + direction * dist;
        
        var middlePoint = (startPoint + endPoint) * 0.5f;
        var size = math.max(0.1f, dist * 0.1f);
        
        Gizmos.color = color;
        Gizmos.DrawLine(startPoint, endPoint);
        DrawArrowCone(endPoint, direction, size);

        var debugText = $"{dist:F2}m";
        var textPosition = middlePoint + new float3(0, 1.5f, 0);
        var fontSize = math.max(14, (int)(14 + dist * 0.1f));
        DrawText(debugText, textPosition, color, fontSize);
    }

    private static void DrawArrowCone(float3 tip, float3 direction, float size)
    {
        var coneHeight = size * 2f;
        var coneRadius = size;
        var baseCenter = tip - direction * coneHeight;

        var perp1 = math.normalize(math.cross(direction,
            math.abs(direction.y) < 0.9f ? new float3(0, 1, 0) : new float3(1, 0, 0)));
        var perp2 = math.cross(direction, perp1);

        var segments = math.max(8, (int)(size * 2.5));
        var prev = baseCenter + perp1 * coneRadius;
        for (var i = 1; i <= segments; i++)
        {
            var angle = i * math.PI * 2f / segments;
            var point = baseCenter + (math.cos(angle) * perp1 + math.sin(angle) * perp2) * coneRadius;
            Gizmos.DrawLine(prev, point);
            Gizmos.DrawLine(tip, point);
            prev = point;
        }
    }

    private static void DrawText(string text, float3 textPosition, Color color, int fontSize = 14)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = color
            },
            fontSize = fontSize
        };

        Handles.Label(textPosition, text, style);
    }

    protected override void OnUpdate()
    {
        // Intentionally empty.
    }
#endif
    
}