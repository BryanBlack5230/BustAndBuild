#nullable enable

using Cysharp.Threading.Tasks;

namespace BarkingBird.Runtime.Infrastructure.SceneWorkflow
{
    public interface ISceneFlow
    {
        UniTask WaitForInit();
    }
}
