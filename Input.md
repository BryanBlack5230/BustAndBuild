Read ReleaseCoordinator, OverlapResolver and GrabbedEntityMover, TunnelTeleporter. Fix bugs in them:
1. ResolveAsync was made UniTask<bool> (success vs timeout), let ReleaseCoordinator track the in-flight task per entity, so that we can call Cancel in dispose, if something happened.
2. Code duplication across the three components.
AabbsOverlapXY is reimplemented in OverlapResolver and TunnelTeleporter. The "OverlapAabb → loop hits → filter dead/self → AABB-XY check" pattern appears four times across the two files (CheckOverlap, DisplaceStep, Teleport, IsStillOverlapping). Make a small static helper that takes a delegate or returns a NativeList of valid RigidBodys
3. Allocation in ClampToViewportAndGround.
The foreach (var offset in new[] { ... }) allocates a 4-element array per call. Release-path so not hot, but static readonly it or unroll the four iterations.