using System;
using Unity.Entities;
using UnityEngine;

namespace GameManagement
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class GameLoopSystemGroup : ComponentSystemGroup
    {
        // Note: This will be controlled externally via Enabled property
    }
    
    public class DotsGameLoopBridge : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameFinishListener, IDisposable
    {
        private World _world;
        private GameLoopSystemGroup _gameLoopGroup;
    
        private bool _isInitialized;

        public void Initialize()
        {
            _world = World.DefaultGameObjectInjectionWorld;
        
            if (_world == null || !_world.IsCreated)
            {
                Debug.LogError("[DotsGameLoopBridge] No default world found! DOTS systems won't run.");
                return;
            }
        
            _gameLoopGroup = _world.GetExistingSystemManaged<GameLoopSystemGroup>();
        
            if (_gameLoopGroup == null)
            {
                Debug.LogError("[DotsGameLoopBridge] GameLoopSystemGroup not found in world!");
                return;
            }
        
            _gameLoopGroup.Enabled = false;
            _isInitialized = true;
        
            Debug.Log($"[DotsGameLoopBridge] Initialized. GameLoopSystemGroup: {_gameLoopGroup != null}");
        }
        public void OnStartGame()
        {
            if (!_isInitialized) return;
        
            Debug.Log("[DotsGameLoopBridge] Starting DOTS systems");
        
            _gameLoopGroup.Enabled = true;
        }

        public void OnPause()
        {
            if (!_isInitialized) return;
        
            Debug.Log("[DotsGameLoopBridge] Pausing DOTS systems");
        
            _gameLoopGroup.Enabled = false;
        }

        public void OnResume()
        {
            if (!_isInitialized) return;
        
            Debug.Log("[DotsGameLoopBridge] Resuming DOTS systems");
        
            _gameLoopGroup.Enabled = true;
        }

        public void OnFinishGame()
        {
            if (!_isInitialized) return;
        
            Debug.Log("[DotsGameLoopBridge] Finishing DOTS systems");
        
            _gameLoopGroup.Enabled = false;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[DotsGameLoopBridge] Disposing");
            
            if (_gameLoopGroup != null)
            {
                _gameLoopGroup.Enabled = false;
            }
        }
    }
}