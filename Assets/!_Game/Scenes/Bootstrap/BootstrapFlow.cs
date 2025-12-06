using Game.Configs;
using Game.Feature.Input;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapFlow : MonoBehaviour
{
    private LoadingService _loadingService;
    private CursorSetter _cursorSetter;
    private Container _bootSceneContainer;
    private ConfigContainer _configContainer;

    [Inject]
    public void Construct(Container container, LoadingService loadingService, CursorSetter cursorSetter, ConfigContainer configContainer)
    {
        _bootSceneContainer = container;
        SceneScope.OnSceneContainerBuilding += OverrideParent;
        
        _loadingService = loadingService;
        _cursorSetter = cursorSetter;
        _configContainer = configContainer;
    }

    private async void Start()
    {
        Log.Boot.D("BootstrapFlow.Start()");
        await _loadingService.BeginLoading(_configContainer);
        await _loadingService.BeginLoading(_cursorSetter);

        //after everything got loaded, start the scene
        SceneManager.LoadSceneAsync(RuntimeConstants.Scenes.World, LoadSceneMode.Additive)
            .completed += operation =>
        {
            SceneScope.OnSceneContainerBuilding -= OverrideParent;
        };
    }
    
    private void OverrideParent(Scene scene, ContainerBuilder builder)
    {
        builder.SetParent(_bootSceneContainer);
    }
}