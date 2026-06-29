namespace AA.Annotate.Core.Geometry;

public readonly record struct DisplaySettingsNumberSource(
    int OriginalIndex,
    RectInt Bounds,
    bool IsPrimary);
