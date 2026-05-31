using System;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Camera
{
    public class WorldCameraHandler : IGameStartListener, IGamePauseListener, IGameResumeListener, IDisposable
    {
        private readonly GameObject _birdViewGO;
        
        private bool _canMove;

        public WorldCameraHandler(WorldSceneData worldSceneData)
        {
            _birdViewGO = worldSceneData.birdViewCamera.gameObject;
            _birdViewGO.SetActive(true);

            EventManager.Input.SceneChangeRequest += OnSceneChangeRequest;
        }

        public void Dispose()
        {
            EventManager.Input.SceneChangeRequest -= OnSceneChangeRequest;
        }

        public void OnStartGame() => _canMove = true;
        public void OnPause() => _canMove = false;
        public void OnResume() => _canMove = true;

        private void OnSceneChangeRequest(bool changeUp)
        {
            if (!_canMove) return;
            if (changeUp && _birdViewGO.activeSelf || !changeUp && !_birdViewGO.activeSelf) return;

            _birdViewGO.SetActive(changeUp);
        }
    }
}