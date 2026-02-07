using Maysternya.SiiUnit.Parsing;

namespace Maysternya.SiiUnit;

public sealed class SiiSyntaxException : SiiException
{
    internal SiiSyntaxException(TextSpan span, string message) : base(message)
    {
        this.Span = span;
    }

    internal SiiSyntaxException(Token token, string message) : this(token.Span, message)
    {
    }

    public TextSpan Span { get; }
}
