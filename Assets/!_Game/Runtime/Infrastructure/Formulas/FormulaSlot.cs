#nullable enable
using System;

namespace BarkingBird.Runtime.Infrastructure.Formulas
{
    /// <summary>
    /// Binding of one {i} formula token to a numeric member of the owning object.
    /// <see cref="TestValue"/> is used only by the inspector preview.
    /// </summary>
    [Serializable]
    public struct FormulaSlot
    {
        public string MemberName;
        public float TestValue;
    }
}
