#nullable enable
using System;
using UnityEngine;

namespace Game.Settings
{
    [CreateAssetMenu(fileName = "TrajectoryPredictorSettings", menuName = "Game/Trajectory Predictor Settings")]
    public sealed class TrajectoryPredictorSettings : ScriptableObject
    {
        [Header("Simulation")]
        [SerializeField] private int _simSteps = 100;
        [SerializeField] private float _simDt = 0.04f;
        [SerializeField] private float _lookAheadTime = 0.02f;
        [SerializeField] private float _lingerDuration = 0.5f;

        [Header("Impact Circle")]
        [SerializeField] private int _circleSegments = 10;
        [SerializeField] private float _circleRadius = 0.5f;

        [Header("Visuals")]
        [SerializeField] private LineRenderer? _trajectoryLinePrefab;
        [SerializeField] private LineRenderer? _impactCirclePrefab;

        public int SimSteps => _simSteps;
        public float SimDt => _simDt;
        public float LookAheadTime => _lookAheadTime;
        public float LingerDuration => _lingerDuration;
        public int CircleSegments => _circleSegments;
        public float CircleRadius => _circleRadius;

        public LineRenderer TrajectoryLinePrefab => _trajectoryLinePrefab
            ?? throw new InvalidOperationException($"{nameof(TrajectoryLinePrefab)} is not assigned.");
        public LineRenderer ImpactCirclePrefab => _impactCirclePrefab
            ?? throw new InvalidOperationException($"{nameof(ImpactCirclePrefab)} is not assigned.");
    }
}
