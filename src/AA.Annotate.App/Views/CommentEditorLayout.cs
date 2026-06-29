namespace AA.Annotate.App.Views;

public static class CommentEditorLayout
{
    public const double MinimumTextHostHeight = 36;
    public const double MaximumTextHostHeight = 156;

    private const double LineHeight = 20;
    private const double VerticalPadding = 14;

    public static double CalculateTextHostHeight(string? text, int charactersPerLine)
    {
        var lineCount = CountVisualLines(text, Math.Max(1, charactersPerLine));
        var desiredHeight = VerticalPadding + lineCount * LineHeight;
        return Math.Clamp(desiredHeight, MinimumTextHostHeight, MaximumTextHostHeight);
    }

    private static int CountVisualLines(string? text, int charactersPerLine)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        var total = 0;
        foreach (var logicalLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            total += Math.Max(1, (int)Math.Ceiling(logicalLine.Length / (double)charactersPerLine));
        }

        return total;
    }
}
