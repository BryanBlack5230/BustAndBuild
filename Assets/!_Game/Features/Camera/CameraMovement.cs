using System;
using Cinemachine;
using Game.Configs;
using Game.Core.Events;
using Game.Feature.Input;

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

        public CameraMovement(MousePositionProvider mouse, BattleSceneData battleSceneData, ConfigContainer configContainer)
        {
            _mouse = mouse;
            var config = configContainer.Battle.CameraConfig;
            _input = new CameraInputHandler(config.timeToHold);

            var rangeX = new BorderRange(battleSceneData.sceneBoundaryLeft.position.x, battleSceneData.sceneBoundaryRight.position.x);
            var rangeY = new BorderRange(battleSceneData.sceneBoundaryBottom.position.y, battleSceneData.sceneBoundaryTop.position.y);

            _transposer = battleSceneData.sceneCamera.GetCinemachineComponent<CinemachineTransposer>();
            _drag = new CameraDragHandler(_transposer, config);
            _border = new CameraBorderHandler(_transposer, rangeX, rangeY, config);
        }

        public void Initialize()
        {
            Register();
        }

        private void Register()
        {
            EventManager.Input.GroundGrabbed += _input.HandleGrabEvent;
            EventManager.Input.Release += _input.HandleRelease;
            
            _input.DragStarted += OnDragStarted;
            _input.DragEnded += OnDragEnded;
        }

        private void Unregister()
        {
            EventManager.Input.GroundGrabbed -= _input.HandleGrabEvent;
            EventManager.Input.Release -= _input.HandleRelease;
            
            _input.DragStarted -= OnDragStarted;
            _input.DragEnded -= OnDragEnded;
            
            _input.Dispose();
            _border.Dispose();
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
            Unregister();
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}