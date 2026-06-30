using System.Text.Json.Serialization;

namespace AA.Annotate.Core.Geometry;

public readonly record struct RectInt
{
    [JsonConstructor]
    public RectInt(int x, int y, int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width cannot be negative.");
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool IsEmpty => Width == 0 || Height == 0;
}
