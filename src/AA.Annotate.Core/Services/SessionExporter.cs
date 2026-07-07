using System.Text.Json;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;

namespace AA.Annotate.Core.Services;

public sealed class SessionExporter
{
    private readonly IAnnotationArtifactWriter? _artifactWriter;
    private readonly JsonSerializerOptions _jsonOptions = SessionJsonOptions.Create();

    public SessionExporter(IAnnotationArtifactWriter? artifactWriter = null)
    {
        _artifactWriter = artifactWriter;
    }

    public async Task ExportAsync(SessionPaths paths, AnnotationSession session, CancellationToken cancellationToken = default)
    {
        var exportSession = NormalizeForExport(session);
        exportSession = await WriteAnnotationArtifactsAsync(paths, exportSession, cancellationToken);
        Directory.CreateDirectory(paths.ExportFolder);
        await File.WriteAllTextAsync(paths.ReviewMarkdownPath, ReviewMarkdownWriter.Write(exportSession), cancellationToken);

        await using var stream = File.Create(paths.AnnotationsJsonPath);
        await JsonSerializer.SerializeAsync(stream, exportSession, _jsonOptions, cancellationToken);
    }

    private async Task<AnnotationSession> WriteAnnotationArtifactsAsync(
        SessionPaths paths,
        AnnotationSession session,
        CancellationToken cancellationToken)
    {
        if (_artifactWriter is null)
        {
            return session;
        }

        var captures = new List<AnnotationCapture>(session.Captures.Count);
        foreach (var capture in session.Captures)
        {
            captures.Add(await _artifactWriter.WriteAsync(paths, capture, cancellationToken));
        }

        return session with { Captures = captures };
    }

    private static AnnotationSession NormalizeForExport(AnnotationSession session)
    {
        return session with
        {
            Captures = session.Captures
                .Select(NormalizeCaptureForExport)
                .ToList()
        };
    }

    private static AnnotationCapture NormalizeCaptureForExport(AnnotationCapture capture)
    {
        if (capture.CropRect is not { } crop)
        {
            return capture;
        }

        var annotations = capture.Annotations
            .Select(annotation => TryClipAnnotation(annotation, crop))
            .Where(annotation => annotation is not null)
            .Select(annotation => annotation!)
            .Select((annotation, index) => annotation with { Number = index + 1 })
            .ToList();

        return capture with
        {
            ScreenshotPath = capture.CroppedPath ?? capture.ScreenshotPath,
            ThumbnailPath = capture.CroppedPath ?? capture.ThumbnailPath,
            Annotations = annotations,
            PrivacyMasks = capture.PrivacyMasks?
                .Select(mask => TryClipPrivacyMask(mask, crop))
                .Where(mask => mask is not null)
                .Select(mask => mask!)
                .ToList()
        };
    }

    private static Annotation? TryClipAnnotation(Annotation annotation, RectInt crop)
    {
        var result = AnnotationCropPolicy.Classify(annotation.BoxRect, crop);
        if (result.ExportBoxRect is not { } exportBoxRect)
        {
            return null;
        }

        return annotation with
        {
            BoxRect = exportBoxRect
        };
    }

    private static PrivacyMask? TryClipPrivacyMask(PrivacyMask mask, RectInt crop)
    {
        var intersection = Intersect(mask.BoxRect, crop);
        if (intersection is null)
        {
            return null;
        }

        return mask with
        {
            BoxRect = new RectInt(
                intersection.Value.X - crop.X,
                intersection.Value.Y - crop.Y,
                intersection.Value.Width,
                intersection.Value.Height)
        };
    }

    private static RectInt? Intersect(RectInt first, RectInt second)
    {
        var left = Math.Max(first.X, second.X);
        var top = Math.Max(first.Y, second.Y);
        var right = Math.Min(first.X + first.Width, second.X + second.Width);
        var bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
        if (right <= left || bottom <= top)
        {
            return null;
        }

        return new RectInt(left, top, right - left, bottom - top);
    }
}
