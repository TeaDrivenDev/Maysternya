using System;

namespace TeaDriven.Maysternya.SiiUnit;

[Flags]
public enum SiiParsingOptions
{
    None = 0,

    IncludeNamelessClasses = 1  << 0,

    AllowExtraContentAfterEnd =  1 << 1
}
