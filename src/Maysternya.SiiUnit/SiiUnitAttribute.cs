using System;

namespace Maysternya.SiiUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class SiiUnitAttribute : Attribute
{
    public SiiUnitAttribute(string className)
    {
        if (String.IsNullOrWhiteSpace(className))
        {
            throw new ArgumentNullException(nameof(className));
        }

        this.ClassName = className;
    }

    public string ClassName { get; }
}
