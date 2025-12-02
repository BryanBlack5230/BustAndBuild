using Cinemachine;
using Game.Feature.Camera;
using UnityEngine;

public class SceneData : MonoBehaviour
{
    public Transform sceneBoundaryLeft, sceneBoundaryRight, sceneBoundaryTop, sceneBoundaryBottom;
    public CinemachineVirtualCamera sceneCamera;
    public CameraSettings cameraSettings;
}