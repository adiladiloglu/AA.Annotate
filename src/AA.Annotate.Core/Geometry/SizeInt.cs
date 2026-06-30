using System.Text.Json.Serialization;

namespace AA.Annotate.Core.Geometry;

public readonly record struct SizeInt
{
    [JsonConstructor]
    public SizeInt(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }
}
