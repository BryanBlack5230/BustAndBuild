using System;
using Cinemachine;
using Game.Core.Events;
using Game.Feature.Input;
using UnityEngine;

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

    public class CameraMovement : IGamePauseListener, IGameResumeListener, IGameUpdateListener, IDisposable
    {
        private readonly CinemachineTransposer _transposer;
        private readonly CameraInputHandler _input;
        private readonly CameraDragHandler _drag;
        private readonly CameraBorderHandler _border;

        private readonly MousePositionProvider _mouse;
        
        private bool _isDragging;

        public CameraMovement(MousePositionProvider mouse, SceneData sceneData)
        {
            _mouse = mouse;
            _input = new CameraInputHandler(sceneData.cameraSettings.timeToHold);

            var rangeX = new BorderRange(sceneData.sceneBoundaryLeft.position.x, sceneData.sceneBoundaryRight.position.x);
            var rangeY = new BorderRange(sceneData.sceneBoundaryBottom.position.y, sceneData.sceneBoundaryTop.position.y);

            _transposer = sceneData.sceneCamera.GetCinemachineComponent<CinemachineTransposer>();
            _drag = new CameraDragHandler(_transposer, sceneData.cameraSettings);
            _border = new CameraBorderHandler(_transposer, rangeX, rangeY, sceneData.cameraSettings);
            
            Register();
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

        public void OnUpdate(float deltaTime)
        {
            if (!_isDragging) return;

            var rawMovement = _drag.GetRawMovement(_mouse.mousePosition);
            var movement = _border.ApplyResistanceIfNeeded(rawMovement);
            
            _drag.ApplyMovement(movement);
        }

        public void OnResume()
        {
            Register();
        }

        public void OnPause()
        {
            Release();

            _input.Dispose();
            _border.Dispose();
        }

        public void Dispose()
        {
            Release();

            _input.Dispose();
            _border.Dispose();
        }
    }
}