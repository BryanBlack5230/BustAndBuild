using System;
using Cinemachine;
using Unity.Entities;

using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Camera
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

    public class BattleCameraMovement : IGamePauseListener, IGameResumeListener, IGameUpdateListener, IWorldInitializable, IDisposable
    {
        private readonly CinemachineTransposer _transposer;
        private readonly CameraInputHandler _input;
        private readonly CameraDragHandler _drag;
        private readonly CameraBorderHandler _border;

        private readonly MousePositionProvider _mouse;
        
        private bool _isDragging;

        public BattleCameraMovement(MousePositionProvider mouse, BattleSceneData battleSceneData, CameraConfigSO config)
        {
            _mouse = mouse;
            _input = new CameraInputHandler(config.TimeToHold);

            _transposer = battleSceneData.sceneCamera.GetCinemachineComponent<CinemachineTransposer>();
            var center = _transposer.FollowTargetPosition;
            var rangeX = new BorderRange(battleSceneData.sceneBoundaryLeft.position.x - center.x, battleSceneData.sceneBoundaryRight.position.x - center.x);
            var rangeY = new BorderRange(battleSceneData.sceneBoundaryBottom.position.y - center.y, battleSceneData.sceneBoundaryTop.position.y - center.y);

            _drag = new CameraDragHandler(_transposer, config);
            _border = new CameraBorderHandler(_transposer, rangeX, rangeY, config);
        }

        // em unused: this class touches no ECS — it implements IWorldInitializable only to share the flow's
        // single post-construction init hook, deferring its EventBus wiring out of the Reflex ctor.
        public void Initialize(EntityManager em) => Register();
        public void OnResume() => Register();
        public void OnPause() => Unregister();
        public void Dispose() => Unregister();

        private void Register()
        {
            EventBus.Subscribe<GroundGrabbedEvent>(_input.HandleGrabEvent);
            EventBus.Subscribe<ReleaseEvent>(_input.HandleRelease);

            _input.DragStarted += OnDragStarted;
            _input.DragEnded += OnDragEnded;
        }

        private void Unregister()
        {
            EventBus.Unsubscribe<GroundGrabbedEvent>(_input.HandleGrabEvent);
            EventBus.Unsubscribe<ReleaseEvent>(_input.HandleRelease);

            _input.DragStarted -= OnDragStarted;
            _input.DragEnded -= OnDragEnded;

            _input.Dispose();
            _border.Dispose();
        }

        private void OnDragEnded()
        {
            if (!_isDragging) return;
            
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
    }
}