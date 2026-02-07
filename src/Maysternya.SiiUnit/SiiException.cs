using System;

namespace TeaDriven.Maysternya.SiiUnit;

public class SiiException : Exception
{
    internal SiiException(string message) : base(message)
    {
    }
}
