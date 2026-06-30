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
            if (!string.IsNullOrWhiteSpace(capture.AnnotatedImagePath))
            {
                builder.AppendLine($"Annotated image: {capture.AnnotatedImagePath}");
            }

            builder.AppendLine(capture.CropRect is { } crop
                ? $"Crop: x={crop.X}, y={crop.Y}, width={crop.Width}, height={crop.Height}"
                : "Crop: none");
            builder.AppendLine();

            foreach (var annotation in capture.Annotations.OrderBy(annotation => annotation.Number))
            {
                var rect = annotation.BoxRect;
                builder.AppendLine($"{annotation.Number}. x={rect.X}, y={rect.Y}, width={rect.Width}, height={rect.Height}");
                if (!string.IsNullOrWhiteSpace(annotation.ImagePath))
                {
                    builder.AppendLine($"   Image: {annotation.ImagePath}");
                }

                builder.AppendLine($"   {annotation.Comment}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
