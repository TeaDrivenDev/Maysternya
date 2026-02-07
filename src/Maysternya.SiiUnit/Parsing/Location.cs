namespace Maysternya.SiiUnit.Parsing;

public sealed class Location
{
    internal Location(int line, int column, int offset)
    {
        this.Line = line;
        this.Column = column;
        this.Offset = offset;
    }

    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }
}
