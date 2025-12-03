using System;
using Game.Feature.Camera;
using Game.Feature.Input;
using GameManagement;
using Reflex.Core;
using UnityEngine;

public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private SceneData _sceneData;
    [SerializeField] private GameLoopManager _gameLoopManager;
    [SerializeField] private GameManagerUIController _gameManagerUIController;
    [SerializeField] private DayNightCycle _dayNightCycle;
    [SerializeField] private GameDataSetter _gameDataSetter;
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton(_sceneData, typeof(SceneData));
        builder.AddInterfacesAndSelf(_dayNightCycle);
        InstallInputs(builder);
        builder.OnContainerBuilt += PreGameManagementForceResolve;
        
        InstallGameLoop(builder);
        
        return;
        void PreGameManagementForceResolve(Container container)
        {
            builder.OnContainerBuilt -= PreGameManagementForceResolve;

            container.Resolve<CursorSetter>();
            container.Resolve<ScrollController>();
            container.Resolve<CameraMovement>();
        }
    }

    private void InstallGameLoop(ContainerBuilder builder)
    {
        builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
        builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
        builder.AddSingleton(typeof(GameManager), typeof(GameManager));

        builder.OnContainerBuilt += PostGameManagementForceResolve;

        return;
        void PostGameManagementForceResolve(Container container)
        {
            builder.OnContainerBuilt -= PostGameManagementForceResolve;

            container.Resolve<GameManager>();
        }
    }

    private void InstallInputs(ContainerBuilder builder)
    {
        builder.AddSingleton(_gameDataSetter, typeof(GameDataSetter));
        builder.AddSingleton(new InputManager(), typeof(InputManager), typeof(IDisposable));
        builder.AddSingleton(typeof(MousePositionProvider), typeof(MousePositionProvider));

        // if (Application.isEditor) builder.AddSingleton(typeof(DummyCursorSetter), typeof(DummyCursorSetter), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(CursorMovementCalculations), typeof(CursorMovementCalculations), typeof(IGameListener));
        builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener));
        builder.AddSingleton(typeof(InteractController), typeof(InteractController), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(CursorSetter), typeof(CursorSetter));
        
        builder.AddSingleton(typeof(ScrollController), typeof(ScrollController), typeof(IDisposable));
        
        builder.AddSingleton(typeof(CameraMovement), typeof(CameraMovement), typeof(IGameListener), typeof(IDisposable));
    }
}
