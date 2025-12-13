using System;
using Unity.Entities;

namespace GameManagement
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class GameLoopSystemGroup : ComponentSystemGroup { }
    
    public class DotsGameLoopBridge : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameFinishListener, IDisposable
    {
        private World _world;
        private GameLoopSystemGroup _gameLoopGroup;
    
        private bool _isInitialized;

        public void Initialize()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated) return;
        
            _gameLoopGroup = _world.GetExistingSystemManaged<GameLoopSystemGroup>();
        
            if (_gameLoopGroup == null) return;
        
            _gameLoopGroup.Enabled = false;
            _isInitialized = true;
        }
        public void OnStartGame()
        {
            if (!_isInitialized) return;
        
            _gameLoopGroup.Enabled = true;
        }

        public void OnPause()
        {
            if (!_isInitialized) return;
        
            _gameLoopGroup.Enabled = false;
        }

        public void OnResume()
        {
            if (!_isInitialized) return;
        
            _gameLoopGroup.Enabled = true;
        }

        public void OnFinishGame()
        {
            if (!_isInitialized) return;
        
            _gameLoopGroup.Enabled = false;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            
            _gameLoopGroup.Enabled = false;
        }
    }
}