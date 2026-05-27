#nullable enable

using Cysharp.Threading.Tasks;

namespace Game.SceneWorkflow
{
    public interface ISceneFlow
    {
        UniTask WaitForInit();
    }
}
