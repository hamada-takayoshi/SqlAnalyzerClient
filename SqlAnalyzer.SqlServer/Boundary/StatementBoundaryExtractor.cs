using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.SqlServer.Boundary;

public sealed class StatementBoundaryExtractor
{
    public StatementBoundaryExtractionResult Extract(string? sqlText)
    {
        string text = sqlText ?? string.Empty;
        int length = text.Length;

        int? semicolonIndex = FindFirstSemicolonOutsideStringAndComments(text);
        int? goLineStartIndex = FindFirstGoBatchLineOutsideString(text);

        int boundaryEndExclusive = length;
        BoundaryKind boundaryKind = BoundaryKind.EndOfText;

        if (semicolonIndex.HasValue || goLineStartIndex.HasValue)
        {
            bool useSemicolon = semicolonIndex.HasValue &&
                                (!goLineStartIndex.HasValue || semicolonIndex.Value < goLineStartIndex.Value);

            if (useSemicolon)
            {
                boundaryKind = BoundaryKind.Semicolon;
                boundaryEndExclusive = semicolonIndex!.Value + 1;
            }
            else
            {
                boundaryKind = BoundaryKind.GoBatch;
                boundaryEndExclusive = goLineStartIndex!.Value;
            }
        }

        string normalizedText = text[..boundaryEndExclusive];
        int trailingStartIndex = boundaryEndExclusive;
        if (boundaryKind == BoundaryKind.Semicolon)
        {
            trailingStartIndex = Math.Min(boundaryEndExclusive, length);
        }
        else if (boundaryKind == BoundaryKind.GoBatch && goLineStartIndex.HasValue)
        {
            trailingStartIndex = FindIndexAfterGoLine(text, goLineStartIndex.Value);
        }

        bool hasTrailingStatements = HasNonWhitespace(text.AsSpan(Math.Min(trailingStartIndex, length)));

        return new StatementBoundaryExtractionResult
        {
            Boundary = new StatementBoundary
            {
                StartIndex = 0,
                EndIndexExclusive = boundaryEndExclusive,
                Kind = boundaryKind
            },
            NormalizedText = normalizedText,
            HasTrailingStatements = hasTrailingStatements
        };
    }

    private static bool HasNonWhitespace(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                return true;
            }
        }

        return false;
    }

    private static int? FindFirstSemicolonOutsideStringAndComments(string text)
    {
        bool inString = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\r' || c == '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }

                continue;
            }

            if (!inString)
            {
                if (c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (c == '\'')
                {
                    inString = true;
                    continue;
                }

                if (c == ';')
                {
                    return i;
                }

                continue;
            }

            if (c == '\'' && next == '\'')
            {
                i++;
                continue;
            }

            if (c == '\'')
            {
                inString = false;
            }
        }

        return null;
    }

    private static int? FindFirstGoBatchLineOutsideString(string text)
    {
        bool inString = false;
        int lineStart = 0;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (!inString && c == '\'')
            {
                inString = true;
                i++;
                continue;
            }

            if (inString)
            {
                if (c == '\'' && next == '\'')
                {
                    i += 2;
                    continue;
                }

                if (c == '\'')
                {
                    inString = false;
                }

                i++;
                continue;
            }

            if (c == '\r' || c == '\n')
            {
                int lineEnd = i;
                if (IsGoLine(text, lineStart, lineEnd))
                {
                    return lineStart;
                }

                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                i++;
                lineStart = i;
                continue;
            }

            i++;
        }

        if (!inString && lineStart <= text.Length && IsGoLine(text, lineStart, text.Length))
        {
            return lineStart;
        }

        return null;
    }

    private static bool IsGoLine(string text, int start, int endExclusive)
    {
        if (start < 0 || endExclusive < start)
        {
            return false;
        }

        ReadOnlySpan<char> line = text.AsSpan(start, endExclusive - start).Trim();
        return line.Equals("GO".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static int FindIndexAfterGoLine(string text, int goLineStartIndex)
    {
        int i = goLineStartIndex;
        while (i < text.Length && text[i] != '\r' && text[i] != '\n')
        {
            i++;
        }

        if (i < text.Length && text[i] == '\r')
        {
            i++;
            if (i < text.Length && text[i] == '\n')
            {
                i++;
            }

            return i;
        }

        if (i < text.Length && text[i] == '\n')
        {
            i++;
        }

        return i;
    }
}
