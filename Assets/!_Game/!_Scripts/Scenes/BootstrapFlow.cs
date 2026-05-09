using Game.Configs;
using Game.Feature.Input;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using GameManagement;
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
    private DotsGameLoopBridge _dotsGameLoopBridge;
    private BlobContainer _blobContainer;
    private ThrowSettingsSetter _throwSettingsSetter;

    [Inject]
    public void Construct(Container container, LoadingService loadingService, CursorSetter cursorSetter, ConfigContainer configContainer, DotsGameLoopBridge dotsGameLoopBridge, BlobContainer blobContainer, ThrowSettingsSetter throwSettingsSetter)
    {
        SceneScope.OnSceneContainerBuilding += OverrideParent;
        _dotsGameLoopBridge = dotsGameLoopBridge;
        _bootSceneContainer = container;

        _loadingService = loadingService;
        _cursorSetter = cursorSetter;
        _configContainer = configContainer;
        _blobContainer = blobContainer;
        _throwSettingsSetter = throwSettingsSetter;
    }

    private async void Start()
    {
        Log.Boot.D("BootstrapFlow.Start()");
        _dotsGameLoopBridge.Initialize();
        _throwSettingsSetter.Initialize();

        await _loadingService.BeginLoading(_configContainer);
        await _loadingService.BeginLoading(_cursorSetter);

        _blobContainer.Initialize();

        SceneManager.LoadSceneAsync(RuntimeConstants.Scenes.World, LoadSceneMode.Additive)
            .completed += OnNextSceneLoaded;
    }
    
    private void OnNextSceneLoaded(AsyncOperation _) => SceneScope.OnSceneContainerBuilding -= OverrideParent;
    private void OverrideParent(Scene scene, ContainerBuilder builder) => builder.SetParent(_bootSceneContainer);
}