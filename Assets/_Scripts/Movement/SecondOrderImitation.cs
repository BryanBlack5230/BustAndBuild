using UnityEngine;
using DG.Tweening;

public class SecondOrderImitation : MonoBehaviour
{
    [SerializeField] Transform[] bones;
    [SerializeField] Transform anchorPoint;
    [SerializeField] float damping = 20f;
    [SerializeField, Range(0f, 1f)] float bounce = 0.8f;
    [SerializeField] float maxDistortion = 1.5f, minDistortion = 0.5f;
    private Vector2[] _distanceToAnchor, _oldPositions;
    private Vector2 _anchorPointPos;

    void Start()
    {
        _oldPositions = new Vector2[bones.Length];

        if (_distanceToAnchor == null || _distanceToAnchor.Length == 0)
            UpdateBoneToAnchorDistance();
    }

    public void UpdateBoneToAnchorDistance()
    {
        _distanceToAnchor = new Vector2[bones.Length];
        _anchorPointPos = anchorPoint.position;

        for (int i = 0; i < bones.Length; i++)
        {
            _distanceToAnchor[i] = (Vector2)bones[i].position - _anchorPointPos;
        }
    }

    void FixedUpdate()
    {
        _anchorPointPos = anchorPoint.position;

        for (int i = 0; i < bones.Length; i++)
        {
            PutConstraints(ref bones[i], i);
            MoveBone(ref bones[i], i);
        }
    }

    private void PutConstraints(ref Transform bone, int index)
    {
        Vector2 toBone = (Vector2)bone.position - _anchorPointPos;
        Vector2 boneDirection = _distanceToAnchor[index].normalized;

        // Determine quadrant based on bone direction
        int quadrant;
        if (boneDirection.x >= 0 && boneDirection.y >= 0)
            quadrant = 0; // First quadrant (top-right)
        else if (boneDirection.x < 0 && boneDirection.y >= 0)
            quadrant = 1; // Second quadrant (top-left)
        else if (boneDirection.x < 0 && boneDirection.y < 0)
            quadrant = 2; // Third quadrant (bottom-left)
        else
            quadrant = 3; // Fourth quadrant (bottom-right)

        // Define sector bounds based on quadrant
        float minAngle = quadrant * 90 - 45;
        float maxAngle = (quadrant + 1) * 90 - 45;

        // Clamp bone position to stay within sector bounds
        float distance = toBone.magnitude;
        float minDistance = minDistortion * _distanceToAnchor[index].magnitude;
        float maxDistance = maxDistortion * _distanceToAnchor[index].magnitude;

        // Clamp distance within min and max distances
        float clampedDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Calculate angle of the bone relative to the anchor point
        float angle = Vector2.SignedAngle(Vector2.up, toBone.normalized);

        // Clamp angle within sector bounds
        float clampedAngle = Mathf.Clamp(angle, minAngle, maxAngle);

        // Adjust bone position based on clamped distance and clamped angle
        Vector2 constrainedPosition = Quaternion.Euler(0, 0, clampedAngle) * Vector2.up * clampedDistance;
        bone.position = _anchorPointPos + constrainedPosition;
    }

    private void MoveBone(ref Transform bone, int index)
    {
        Vector2 oldPos = bone.position;

        bone.position = Vector2.Lerp(bone.position, _distanceToAnchor[index] + _anchorPointPos, (1 - bounce) * damping * Time.fixedDeltaTime);
        bone.position = (1 + bounce) * (Vector2)bone.position - bounce * _oldPositions[index];

        _oldPositions[index] = oldPos;
    }

    void OnDrawGizmos()
    {
        if (_distanceToAnchor == null || _distanceToAnchor.Length == 0)
            UpdateBoneToAnchorDistance();

        for (int i = 0; i < bones.Length; i++)
        {
            float distanceToCenter = _distanceToAnchor[i].magnitude;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)bones[i].position, distanceToCenter * minDistortion);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)bones[i].position, distanceToCenter * maxDistortion);
        }
    }

    public void MoveToRandomPosition()
    {
        Vector3 lowerLeftCorner = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 upperRightCorner = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        var minX = lowerLeftCorner.x;
        var maxX = upperRightCorner.x;
        var minY = lowerLeftCorner.y;
        var maxY = upperRightCorner.y;

        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        var targetPosition = new Vector3(randomX, randomY, transform.position.z);

        anchorPoint.position = targetPosition;
        Debug.Break();
    }

    public void ApplyCollision(Vector2 collisionPoint, Vector2 force)
    {
        Transform[] closestBones = FindClosestBonesToCollision(collisionPoint);

        // Calculate weights based on distances
        float totalDistance = Vector2.Distance(collisionPoint, closestBones[0].position) + Vector2.Distance(collisionPoint, closestBones[1].position);
        float weight1 = 1 - (Vector2.Distance(collisionPoint, closestBones[0].position) / totalDistance);
        float weight2 = 1 - (Vector2.Distance(collisionPoint, closestBones[1].position) / totalDistance);

        // Calculate displacement vectors for the two closest bones
        Vector3 displacement1 = force.magnitude * weight1 * ((Vector2)closestBones[0].position - collisionPoint).normalized;
        Vector3 displacement2 = force.magnitude * weight2 * ((Vector2)closestBones[1].position - collisionPoint).normalized;

        // Adjust bone positions based on collision
        closestBones[0].position += displacement1;
        closestBones[1].position += displacement2;
    }

    private Transform[] FindClosestBonesToCollision(Vector2 collisionPoint)
    {
        Transform closestBone1 = null;
        Transform closestBone2 = null;
        float minDistance1 = Mathf.Infinity;
        float minDistance2 = Mathf.Infinity;

        for (int i = 0; i < bones.Length; i++)
        {
            float distance = Vector2.Distance(collisionPoint, bones[i].position);
            if (distance < minDistance1)
            {
                minDistance2 = minDistance1;
                closestBone2 = closestBone1;

                minDistance1 = distance;
                closestBone1 = bones[i];
            }
            else if (distance < minDistance2)
            {
                minDistance2 = distance;
                closestBone2 = bones[i];
            }
        }

        return new Transform[] { closestBone1, closestBone2 };
    }

    public void ResetBonePositions()
    {
        if (_distanceToAnchor == null || _distanceToAnchor.Length == 0)
            UpdateBoneToAnchorDistance();

        _anchorPointPos = anchorPoint.position;

        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].position = _distanceToAnchor[i] + _anchorPointPos;
            _oldPositions[i] = bones[i].position;
        }
    }
}
