using AA.Annotate.Platform.Linux;

namespace AA.Annotate.Platform.Linux.Tests;

public sealed class XdgPortalResponseMapperTests
{
    [Fact]
    public void CancellationNeverFallsBackToAnImage()
    {
        var decision = XdgPortalResponseMapper.Evaluate(
            1,
            new Dictionary<string, object>
            {
                ["uri"] = "file:///ignored.png"
            });

        Assert.Equal(XdgPortalResponseKind.Cancelled, decision.Kind);
        Assert.Null(decision.LocalPath);
    }

    [Fact]
    public void BackendTerminationIsUnavailableWithGnomeGuidance()
    {
        var decision = XdgPortalResponseMapper.Evaluate(
            2,
            new Dictionary<string, object>());

        Assert.Equal(XdgPortalResponseKind.Unavailable, decision.Kind);
        Assert.Contains("GNOME Wayland", decision.Message, StringComparison.Ordinal);
        Assert.Contains("X11", decision.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://example.com/capture.png")]
    [InlineData("capture.png")]
    public void SuccessRejectsNonLocalUris(string uri)
    {
        var decision = XdgPortalResponseMapper.Evaluate(
            0,
            new Dictionary<string, object> { ["uri"] = uri });

        Assert.Equal(XdgPortalResponseKind.Unavailable, decision.Kind);
        Assert.Null(decision.LocalPath);
    }

    [Fact]
    public void SuccessRequiresAnExistingFile()
    {
        var missing = Path.Combine(
            Path.GetTempPath(),
            "AA.Annotate.Portal.Tests",
            Guid.NewGuid().ToString("N"),
            "missing.png");
        var decision = XdgPortalResponseMapper.Evaluate(
            0,
            new Dictionary<string, object>
            {
                ["uri"] = new Uri(missing).AbsoluteUri
            });

        Assert.Equal(XdgPortalResponseKind.Unavailable, decision.Kind);
    }

    [Fact]
    public void SuccessReturnsTheExistingLocalFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            var decision = XdgPortalResponseMapper.Evaluate(
                0,
                new Dictionary<string, object>
                {
                    ["uri"] = new Uri(path).AbsoluteUri
                });

            Assert.Equal(XdgPortalResponseKind.Completed, decision.Kind);
            Assert.Equal(path, decision.LocalPath);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
