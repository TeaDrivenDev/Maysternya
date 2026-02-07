namespace TeaDriven.Maysternya.SiiUnit.Parsing;

internal record Token(string Text, TokenKind Kind, TextSpan Span, string FileName, object Tag)
{
    public override string ToString()
    {
        switch (this.Kind)
        {
            case TokenKind.EndOfInput:
                return "end-of-input";

            case TokenKind.Identifier:
                return this.Text;

            case TokenKind.Number:
            case TokenKind.True:
            case TokenKind.False:
                return this.Kind.ToString().ToLowerInvariant();

            default:
                return this.Text;
        }
    }
}
