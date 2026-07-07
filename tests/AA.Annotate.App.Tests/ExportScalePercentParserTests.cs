using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class ExportScalePercentParserTests
{
    [Theory]
    [InlineData("60", 60)]
    [InlineData("60%", 60)]
    [InlineData(" 40 %", 40)]
    public void ParseOrDefaultAcceptsTypedPercentages(string text, int expected)
    {
        Assert.Equal(expected, ExportScalePercentParser.ParseOrDefault(text, fallbackPercent: 100));
    }

    [Theory]
    [InlineData("0", 20)]
    [InlineData("-10", 20)]
    [InlineData("1", 20)]
    [InlineData("101", 100)]
    public void ParseOrDefaultClampsToValidExportRange(string text, int expected)
    {
        Assert.Equal(expected, ExportScalePercentParser.ParseOrDefault(text, fallbackPercent: 100));
    }

    [Fact]
    public void ParseOrDefaultFallsBackForInvalidText()
    {
        Assert.Equal(75, ExportScalePercentParser.ParseOrDefault("abc", fallbackPercent: 75));
    }
}
