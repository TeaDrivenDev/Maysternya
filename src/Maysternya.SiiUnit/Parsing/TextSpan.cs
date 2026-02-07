namespace Maysternya.SiiUnit.Parsing;

public sealed record TextSpan(Location Start, Location End)
{
    public int Length => this.End?.Offset - this.Start?.Offset ?? -1;

    internal TextSpan WithEnd(Location end)
    {
        return new TextSpan(this.Start, end);
    }
}
