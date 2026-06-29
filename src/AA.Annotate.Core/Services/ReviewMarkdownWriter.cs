using System.Text;
using AA.Annotate.Core.Models;

namespace AA.Annotate.Core.Services;

public static class ReviewMarkdownWriter
{
    public static string Write(AnnotationSession session)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Annotation Review");
        builder.AppendLine();

        foreach (var capture in session.Captures.OrderBy(capture => capture.Number))
        {
            builder.AppendLine($"## Capture {capture.Number}");
            builder.AppendLine();
            builder.AppendLine($"Image: {capture.CroppedPath ?? capture.ScreenshotPath}");
            builder.AppendLine($"Full screenshot: {capture.ScreenshotPath}");
            builder.AppendLine(capture.CropRect is { } crop
                ? $"Crop: x={crop.X}, y={crop.Y}, width={crop.Width}, height={crop.Height}"
                : "Crop: none");
            builder.AppendLine();

            foreach (var annotation in capture.Annotations.OrderBy(annotation => annotation.Number))
            {
                var rect = capture.CroppedPath is not null && capture.CropRect is { } activeCrop
                    ? new(
                        annotation.BoxRect.X - activeCrop.X,
                        annotation.BoxRect.Y - activeCrop.Y,
                        annotation.BoxRect.Width,
                        annotation.BoxRect.Height)
                    : annotation.BoxRect;
                builder.AppendLine($"{annotation.Number}. x={rect.X}, y={rect.Y}, width={rect.Width}, height={rect.Height}");
                if (capture.CroppedPath is not null)
                {
                    var fullRect = annotation.BoxRect;
                    builder.AppendLine($"   Full screenshot box: x={fullRect.X}, y={fullRect.Y}, width={fullRect.Width}, height={fullRect.Height}");
                }

                builder.AppendLine($"   {annotation.Comment}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
