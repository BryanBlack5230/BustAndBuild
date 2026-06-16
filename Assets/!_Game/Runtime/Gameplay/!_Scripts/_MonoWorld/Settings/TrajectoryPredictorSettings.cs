#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [CreateAssetMenu(fileName = "TrajectoryPredictorSettings", menuName = "Game/Trajectory Predictor Settings")]
    public sealed class TrajectoryPredictorSettings : ScriptableObject
    {
        [Title("Simulation")]
        [Tooltip("Number of integration steps simulated for the predicted arc.")]
        [SerializeField, Min(1)] private int _simSteps = 100;
        [Tooltip("Timestep per simulation step.")]
        [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _simDt = 0.04f;
        [Tooltip("How far ahead of the throw origin the arc starts sampling.")]
        [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _lookAheadTime = 0.02f;
        [Tooltip("How long the arc lingers (fading) after release.")]
        [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _lingerDuration = 0.5f;

        [Title("Impact Circle")]
        [Tooltip("Number of line segments forming the impact circle.")]
        [SerializeField, Min(3)] private int _circleSegments = 10;
        [SerializeField, Min(0f), SuffixLabel("m", Overlay = true)] private float _circleRadius = 0.5f;

        [Title("Visuals")]
        [Required, SerializeField] private LineRenderer? _trajectoryLinePrefab;
        [Required, SerializeField] private LineRenderer? _impactCirclePrefab;

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
