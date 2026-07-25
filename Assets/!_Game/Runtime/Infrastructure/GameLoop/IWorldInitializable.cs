#nullable enable

using Unity.Entities;

namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    /// <summary>
    /// Implemented by Mono-side services/views that need the ECS <see cref="EntityManager"/> after the scene's
    /// world content is ready. The scene <c>*Flow</c> resolves every participant (registered under the
    /// <see cref="IWorldInitializable"/> contract) and calls <see cref="Initialize"/> in its <c>Start()</c> —
    /// deferring world access out of the Reflex constructor so the world stays out of the container and tests can
    /// pass a throwaway world instead of mutating the process-global default (see ADR / di-architecture learnings).
    ///
    /// The param is the <see cref="EntityManager"/> rather than the <c>World</c> because that is all consumers
    /// use; <c>em.World</c> recovers the World if ever needed.
    /// </summary>
    public interface IWorldInitializable
    {
        void Initialize(EntityManager em);
    }
}
