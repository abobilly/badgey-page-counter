namespace PageCounterPro.Core.PageCountProviders;

using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for image files.
/// Treats each image as 1 page (as if printing).
/// </summary>
public sealed class ImagePageCountProvider : IPageCountProvider
{
    private readonly ILogger<ImagePageCountProvider> _logger;

    public ImagePageCountProvider(ILogger<ImagePageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.ImageExtensions;

    public bool CanHandle(FileInfo file)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public PageCountResult GetPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            var extension = file.Extension.TrimStart('.').ToLowerInvariant();

            // For TIFF files, we could potentially count frames, but for simplicity treat as 1 page
            // unless explicitly handling multi-page TIFFs
            if (extension == "tif" || extension == "tiff")
            {
                return TryGetTiffFrameCount(file);
            }

            _logger.LogDebug("Image file {FileName} treated as 1 page", file.Name);
            return PageCountResult.Successful(1, "OK – image treated as 1 page for printing");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process image {FileName}", file.Name);
            return PageCountResult.Successful(1, $"Image treated as 1 page for printing; note: {ex.Message}");
        }
    }

    private PageCountResult TryGetTiffFrameCount(FileInfo file)
    {
        try
        {
            // For now, treat TIFF as single page - full multi-page TIFF support would require
            // additional image processing library
            _logger.LogDebug("TIFF file {FileName} treated as 1 page", file.Name);
            return PageCountResult.Successful(1, "OK – TIFF image treated as 1 page for printing");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read TIFF frame count for {FileName}", file.Name);
            return PageCountResult.Successful(1, $"TIFF treated as 1 page; could not read frame count: {ex.Message}");
        }
    }
}
