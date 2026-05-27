#nullable enable

using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.SceneWorkflow;
using Game.Feature.Input;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using GameManagement;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

public sealed class BootstrapFlow : MonoBehaviour, ISceneFlow
{
    private readonly UniTaskCompletionSource _initCompleted = new();

    private LoadingService _loadingService = null!;
    private CursorSetter _cursorSetter = null!;
    private Container _bootSceneContainer = null!;
    private ConfigContainer _configContainer = null!;
    private DotsGameLoopBridge _dotsGameLoopBridge = null!;
    private BlobContainer _blobContainer = null!;
    private ThrowSettingsSetter _throwSettingsSetter = null!;

    UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

    [Inject]
    private void Construct(Container container, LoadingService loadingService, CursorSetter cursorSetter, ConfigContainer configContainer, DotsGameLoopBridge dotsGameLoopBridge, BlobContainer blobContainer, ThrowSettingsSetter throwSettingsSetter)
    {
        SceneScope.OnSceneContainerBuilding += OverrideParent;
        _bootSceneContainer = container;
        _loadingService = loadingService;
        _cursorSetter = cursorSetter;
        _configContainer = configContainer;
        _dotsGameLoopBridge = dotsGameLoopBridge;
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
        _initCompleted.TrySetResult();
    }

    private void OverrideParent(UnityEngine.SceneManagement.Scene scene, ContainerBuilder builder)
    {
        SceneScope.OnSceneContainerBuilding -= OverrideParent;
        builder.SetParent(_bootSceneContainer);
    }
}
