using AA.Annotate.App.Services;
using SkiaSharp;

namespace AA.Annotate.App.Tests;

public sealed class PortableImageOperationsTests
{
    [Fact]
    public void CreateTextPaint_UsesBundledBoldInterTypeface()
    {
        using var paint = PortableImageOperations.CreateTextPaint(24, SKColors.Black);

        Assert.Equal("Inter", paint.Typeface.FamilyName);
        Assert.True(paint.Typeface.FontWeight >= (int)SKFontStyleWeight.Bold);
    }
}
