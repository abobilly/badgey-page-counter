namespace PageCounterPro.Core.PageCountProviders;

using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;
using TagLib;

/// <summary>
/// Page count provider for video files.
/// Returns 1 page and includes runtime in notes.
/// </summary>
public sealed class VideoPageCountProvider : IPageCountProvider
{
    private readonly ILogger<VideoPageCountProvider> _logger;

    public VideoPageCountProvider(ILogger<VideoPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.VideoExtensions;

    public bool CanHandle(FileInfo file)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public PageCountResult GetPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            using var tagFile = TagLib.File.Create(file.FullName);
            var duration = tagFile.Properties.Duration;

            if (duration.TotalSeconds > 0)
            {
                var formattedDuration = FormatDuration(duration);
                _logger.LogDebug("Video file {FileName} has runtime {Duration}", file.Name, formattedDuration);
                return PageCountResult.Successful(1, $"Runtime: {formattedDuration} (hh:mm:ss); treated as 1 page");
            }
            else
            {
                _logger.LogDebug("Video file {FileName} has unknown runtime", file.Name);
                return PageCountResult.Successful(1, "Runtime unknown; treated as 1 page; duration metadata not available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read video metadata for {FileName}", file.Name);
            return PageCountResult.Successful(1, $"Runtime unknown; treated as 1 page; failed to read metadata: {ex.Message}");
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
}
