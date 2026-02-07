namespace Maysternya.SiiUnit.Parsing;

public sealed class TextSpan
{
    internal TextSpan(Location start, Location end)
    {
        this.Start = start;
        this.End = end;
    }

    public Location Start { get; }
    public Location End { get; }
    public int Length => this.End?.Offset - this.Start?.Offset ?? -1;

    internal TextSpan WithEnd(Location end)
    {
        return new TextSpan(this.Start, end);
    }
}
