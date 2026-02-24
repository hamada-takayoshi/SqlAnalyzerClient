namespace SqlAnalyzer.SqlServer.Formatting;

public sealed record SqlFormatOptions
{
    public int IndentationWidth { get; init; } = 4;

    public bool UppercaseKeywords { get; init; } = true;
}
