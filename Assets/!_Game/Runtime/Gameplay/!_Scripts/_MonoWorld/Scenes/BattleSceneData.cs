using Cinemachine;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BattleSceneData : MonoBehaviour
    {
        public Transform sceneBoundaryLeft, sceneBoundaryRight, sceneBoundaryTop, sceneBoundaryBottom;
        public CinemachineVirtualCamera sceneCamera;

        [Header("Beacon")]
        [Tooltip("Beacon Core socket: insert target + day-end drop point + (future) hint anchor. " +
                 "Wire to a Transform at the beacon. Leave unassigned to disable Core insertion in this scene.")]
        public Transform beaconCoreSocket;
    }
}