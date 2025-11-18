using GameManagement;
using Reflex.Core;
using UnityEngine;

public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private GameLoopManager _gameLoopManager;
    [SerializeField] private GameManagerUIController _gameManagerUIController;
    [SerializeField] private DayNightCycle _dayNightCycle;
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddInterfacesAndSelf(_dayNightCycle);
        
        builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
        builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
        builder.AddSingleton(new GameManager(_gameLoopManager, _gameManagerUIController), typeof(GameManager)); // Lazily construct GameManager, bind as GameManager

    }
}
