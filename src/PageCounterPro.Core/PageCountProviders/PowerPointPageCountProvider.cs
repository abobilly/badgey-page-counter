namespace PageCounterPro.Core.PageCountProviders;

using DocumentFormat.OpenXml.Packaging;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for PowerPoint presentations (pptx, ppt).
/// </summary>
public sealed class PowerPointPageCountProvider : IPageCountProvider
{
    private readonly ILogger<PowerPointPageCountProvider> _logger;

    public PowerPointPageCountProvider(ILogger<PowerPointPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.PowerPointExtensions;

    public bool CanHandle(FileInfo file)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public PageCountResult GetPageCount(FileInfo file, AppSettings settings)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            "pptx" => GetPptxPageCount(file),
            "ppt" => GetPptPageCount(file),
            _ => PageCountResult.Unsupported()
        };
    }

    private PageCountResult GetPptxPageCount(FileInfo file)
    {
        try
        {
            using var document = PresentationDocument.Open(file.FullName, false);
            var presentationPart = document.PresentationPart;

            if (presentationPart?.Presentation?.SlideIdList != null)
            {
                var slideCount = presentationPart.Presentation.SlideIdList.Count();

                _logger.LogDebug("PPTX file {FileName} has {Slides} slides", file.Name, slideCount);
                return PageCountResult.Successful(slideCount, $"OK – {slideCount} slide(s) from presentation");
            }

            return PageCountResult.Successful(1, "OK – PPTX treated as 1 page; slides could not be counted");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read PPTX {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read PPTX; exception: {ex.Message}");
        }
    }

    private PageCountResult GetPptPageCount(FileInfo file)
    {
        try
        {
            // Legacy PPT format - estimate based on file size
            var fileSize = file.Length;
            // Rough estimate: ~50KB per slide for PPT files
            var estimatedSlides = Math.Max(1, (int)Math.Ceiling(fileSize / 51200.0));

            _logger.LogDebug("PPT file {FileName} estimated at {Slides} slides based on file size",
                file.Name, estimatedSlides);

            return PageCountResult.Successful(
                estimatedSlides,
                $"OK – estimated slides based on file size ({fileSize} bytes); legacy PPT format");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process PPT {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to process PPT; exception: {ex.Message}");
        }
    }
}
