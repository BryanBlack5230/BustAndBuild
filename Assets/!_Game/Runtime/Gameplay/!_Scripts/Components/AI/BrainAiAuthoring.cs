using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class BrainAiAuthoring : MonoBehaviour
    {
        public class Baker : Baker<BrainAiAuthoring>
        {
            public override void Bake(BrainAiAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BattleBrain
                {

                });
            }
        }
    }

    public enum Emotion
    {
        Normal,
        Suspicious,
        Angry,
        Scared,
    }

    public struct EmotionalState : IComponentData
    {
        public Emotion Value;
    }

    public struct BattleBrain : IComponentData
    {
        public bool CanAttack;
    }

    public enum ActionType
    {
        Stunned,
        Moving,
        Attacking,
        Evading
    }
    public struct ActionState : IComponentData
    {
        public ActionType Value;
    }

    /*
    Emotional State (Data / Modifiers):

        What it is: Pure data. It doesn't "do" anything. It just modifies weights in other systems.

        Example: Angry increases AggressionRadius in the Targeting System. Scared forces the Brain to pick the Flee behavior.

    Behavior State (The Brain):

        What it is: The decision maker. It reads Sensors (Targeting) and Emotion, then sets the Goal.

        Output: It writes to the Destination component and sets the Action State.

    Action State (The Body):

        What it is: The dumb executor.

        Example: If State is Moving, the MovementSystem pushes the LocalTransform towards Destination. If State is Attacking, the CombatSystem plays the animation and deals damage.
    */
}
