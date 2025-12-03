namespace PageCounterPro.Core.PageCountProviders;

using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PdfSharpCore.Pdf.IO;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for PDF files.
/// </summary>
public sealed class PdfPageCountProvider : IPageCountProvider
{
    private readonly ILogger<PdfPageCountProvider> _logger;

    public PdfPageCountProvider(ILogger<PdfPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.PdfExtensions;

    public bool CanHandle(FileInfo file)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public PageCountResult GetPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            using var document = PdfReader.Open(file.FullName, PdfDocumentOpenMode.InformationOnly);
            var pageCount = document.PageCount;

            _logger.LogDebug("PDF {FileName} has {PageCount} pages", file.Name, pageCount);

            return PageCountResult.Successful(pageCount, "OK – PDF pages from library");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read PDF {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read PDF; exception: {ex.Message}");
        }
    }
}
