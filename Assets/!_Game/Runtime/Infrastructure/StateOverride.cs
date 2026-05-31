#nullable enable

using System;
using Cysharp.Threading.Tasks;

namespace BarkingBird.Runtime.Infrastructure
{
    [Serializable]
    public abstract class StateOverride
    {
        public abstract UniTask Apply();
    }
}
