using UnityEngine;
using Cinemachine;
using Game.Core.Events;
using Game.Feature.Input;
using Reflex.Attributes;

namespace Game.Feature.Camera
{
    public struct BorderRange
    {
        public readonly float Min;
        public readonly float Max;
        
        public BorderRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public bool IsOutside(float v) => v < Min || v > Max;

        public float DistanceOutside(float v)
        {
            if (v < Min) return Min - v;
            if (v > Max) return v - Max;
            return 0f;
        }
    }

    public class CameraMovement : MonoBehaviour
    {
        private CinemachineTransposer _transposer;
        private CameraInputHandler _input;
        private CameraDragHandler _drag;
        private CameraBorderHandler _border;

        private MousePositionProvider _mouse;
        
        private bool _isDragging;

        [Inject]
        private void Construct(MousePositionProvider mouse, SceneData sceneData)
        {
            _mouse = mouse;
            _input = new CameraInputHandler(sceneData.cameraSettings.timeToHold);

            var rangeX = new BorderRange(sceneData.sceneBoundaryLeft.position.x, sceneData.sceneBoundaryRight.position.x);
            var rangeY = new BorderRange(sceneData.sceneBoundaryBottom.position.y, sceneData.sceneBoundaryTop.position.y);

            _transposer = sceneData.sceneCamera.GetCinemachineComponent<CinemachineTransposer>();
            _drag = new CameraDragHandler(_transposer, sceneData.cameraSettings);
            _border = new CameraBorderHandler(_transposer, rangeX, rangeY, sceneData.cameraSettings);
        }

        private void Register()
        {
            EventManager.Input.GroundGrabbed += _input.HandleGrabEvent;
            EventManager.Input.Release += _input.HandleRelease;
            
            _input.DragStarted += OnDragStarted;
            _input.DragEnded += OnDragEnded;
        }

        private void Release()
        {
            EventManager.Input.GroundGrabbed -= _input.HandleGrabEvent;
            EventManager.Input.Release -= _input.HandleRelease;
            
            _input.DragStarted -= OnDragStarted;
            _input.DragEnded -= OnDragEnded;
        }

        private void OnDragEnded()
        {
            _isDragging = false;
            if (_border.IsOutsideBounds(_transposer.m_FollowOffset))
                _border.StartReturn();
        }

        private void OnDragStarted()
        {
            _isDragging = true;
            _border.CancelReturn();
            _drag.StartDrag(_mouse.mousePosition);
        }

        private void Update()
        {
            if (!_isDragging) return;

            var rawMovement = _drag.GetRawMovement(_mouse.mousePosition);
            var movement = _border.ApplyResistanceIfNeeded(rawMovement);
            
            _drag.ApplyMovement(movement);
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Release();

            _input.Dispose();
            _border.Dispose();
        }
    }
}