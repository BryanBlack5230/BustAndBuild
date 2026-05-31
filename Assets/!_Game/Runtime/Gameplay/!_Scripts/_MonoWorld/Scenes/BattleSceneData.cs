using Cinemachine;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BattleSceneData : MonoBehaviour
    {
        public Transform sceneBoundaryLeft, sceneBoundaryRight, sceneBoundaryTop, sceneBoundaryBottom;
        public CinemachineVirtualCamera sceneCamera;
    }
}