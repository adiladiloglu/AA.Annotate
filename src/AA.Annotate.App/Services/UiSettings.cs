using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Services;

public sealed record UiSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public ToolbarPlacement? Toolbar { get; init; }
}
