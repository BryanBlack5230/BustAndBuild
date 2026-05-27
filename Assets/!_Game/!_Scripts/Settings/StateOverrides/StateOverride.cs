#nullable enable

using System;
using Cysharp.Threading.Tasks;

namespace Game.Configs
{
    [Serializable]
    public abstract class StateOverride
    {
        public abstract UniTask Apply();
    }
}
