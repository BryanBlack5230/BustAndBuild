using Cinemachine;
using UnityEngine;

namespace Game.Feature.Camera
{
    public class CameraDragHandler
    {
        private readonly CinemachineTransposer _transposer;
        private readonly CameraSettings _settings;

        private Vector3 _lastPos;

        public CameraDragHandler(CinemachineTransposer transposer, CameraSettings settings)
        {
            _transposer = transposer;
            _settings = settings;
        }
        
        public void StartDrag(Vector3 mousePos)
        {
            _lastPos = mousePos;
        }

        public Vector2 GetRawMovement(Vector3 mousePos)
        {
            var delta = mousePos - _lastPos;
            var movement = -new Vector2(delta.x, delta.y) * _settings.moveSpeed;
            _lastPos = mousePos;
            
            return movement;
        }

        public void ApplyMovement(Vector2 movement)
        {
            var offset = _transposer.m_FollowOffset;
            
            offset.x += movement.x;
            offset.y += movement.y;

            _transposer.m_FollowOffset = offset;
        }
    }
}