#nullable enable
using System;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Formulas
{
    /// <summary>
    /// Marks a string field as a designer-editable math formula.
    /// Slot tokens {0}..{n} in the formula are bound to numeric members of the
    /// owning object through a <see cref="FormulaSlot"/>[] field declared next to it,
    /// whose name is passed to the constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FormulaAttribute : PropertyAttribute
    {
        public string SlotsFieldName { get; }

        public FormulaAttribute(string slotsFieldName)
        {
            SlotsFieldName = slotsFieldName;
        }
    }
}
