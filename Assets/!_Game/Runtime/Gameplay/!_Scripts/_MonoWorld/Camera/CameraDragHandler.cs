using Cinemachine;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Camera
{
    public class CameraDragHandler
    {
        private readonly CinemachineTransposer _transposer;
        private readonly CameraConfig _config;

        private Vector3 _lastPos;

        public CameraDragHandler(CinemachineTransposer transposer, CameraConfig config)
        {
            _transposer = transposer;
            _config = config;
        }
        
        public void StartDrag(Vector3 mousePos)
        {
            _lastPos = mousePos;
        }

        public Vector2 GetRawMovement(Vector3 mousePos)
        {
            var delta = mousePos - _lastPos;
            var movement = -new Vector2(delta.x, delta.y) * _config.moveSpeed;
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