using System;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure.Commands;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Camera
{
    public class WorldCameraHandler : IGameStartListener, IGamePauseListener, IGameResumeListener, IDisposable
    {
        private readonly GameObject _birdViewGO;
        private readonly IDisposable _changeSceneSub;

        private bool _canMove;

        public WorldCameraHandler(WorldSceneData worldSceneData, CommandDispatcher dispatcher)
        {
            _birdViewGO = worldSceneData.birdViewCamera.gameObject;
            _birdViewGO.SetActive(true);

            _changeSceneSub = dispatcher.Register<ChangeSceneCommand>(OnChangeScene);
        }

        public void Dispose()
        {
            _changeSceneSub.Dispose();
        }

        public void OnStartGame() => _canMove = true;
        public void OnPause() => _canMove = false;
        public void OnResume() => _canMove = true;

        private void OnChangeScene(ChangeSceneCommand cmd)
        {
            if (!_canMove) return;
            if (cmd.SwitchUp && _birdViewGO.activeSelf || !cmd.SwitchUp && !_birdViewGO.activeSelf) return;

            _birdViewGO.SetActive(cmd.SwitchUp);
        }
    }
}
