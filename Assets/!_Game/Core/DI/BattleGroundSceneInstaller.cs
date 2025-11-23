using System;
using Game.Feature.Input;
using GameManagement;
using Reflex.Core;
using UnityEngine;

public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private GameLoopManager _gameLoopManager;
    [SerializeField] private GameManagerUIController _gameManagerUIController;
    [SerializeField] private DayNightCycle _dayNightCycle;
    [SerializeField] private GameDataSetter _gameDataSetter;
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddInterfacesAndSelf(_dayNightCycle);
        InstallInputs(builder);

        builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
        builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
        builder.AddSingleton(new GameManager(_gameLoopManager, _gameManagerUIController), typeof(GameManager)); // Lazily construct GameManager, bind as GameManager

    }

    private void InstallInputs(ContainerBuilder builder)
    {
        builder.AddSingleton(_gameDataSetter, typeof(GameDataSetter));
        builder.AddSingleton(new InputManager(), typeof(InputManager));
        builder.AddSingleton(typeof(MousePositionProvider), typeof(MousePositionProvider));
        
        if (Application.isEditor) builder.AddSingleton(typeof(DummyCursorSetter), typeof(DummyCursorSetter), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(CursorMovementCalculations), typeof(CursorMovementCalculations), typeof(IGameListener));
        builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener));
        builder.AddSingleton(new CursorSetter(_gameDataSetter), typeof(CursorSetter));
    }
}
