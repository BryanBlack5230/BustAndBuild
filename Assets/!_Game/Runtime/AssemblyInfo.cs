using System.Runtime.CompilerServices;

// The broadphase collectors (TargetScoringCollector, ObstacleDangerCollector) are internal but are the
// behavioural heart of the targeting/steering refactor, so the test assembly needs to fold fabricated
// DistanceHits through them directly. See Tests/AI/*CollectorTests.cs.
[assembly: InternalsVisibleTo("Tests")]
