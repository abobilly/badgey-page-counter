namespace PageCounterPro.Core.PageCountProviders;

using DocumentFormat.OpenXml.Packaging;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for Word documents (docx, doc, rtf).
/// </summary>
public sealed class WordPageCountProvider : IPageCountProvider
{
    private readonly ILogger<WordPageCountProvider> _logger;

    public WordPageCountProvider(ILogger<WordPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.WordExtensions;

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
            "docx" => GetDocxPageCount(file, settings),
            "doc" => GetDocPageCount(file, settings),
            "rtf" => GetRtfPageCount(file, settings),
            _ => PageCountResult.Unsupported()
        };
    }

    private PageCountResult GetDocxPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            using var document = WordprocessingDocument.Open(file.FullName, false);

            // Try to get page count from extended properties
            var extendedFileProperties = document.ExtendedFilePropertiesPart;
            if (extendedFileProperties?.Properties?.Pages?.Text is string pageText
                && int.TryParse(pageText, out var pageCount)
                && pageCount > 0)
            {
                _logger.LogDebug("DOCX file {FileName} has {Pages} pages from metadata", file.Name, pageCount);
                return PageCountResult.Successful(pageCount, "OK – pages via document metadata");
            }

            // Fallback: estimate based on content
            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body != null)
            {
                var text = mainPart.Document.Body.InnerText ?? string.Empty;
                var charCount = text.Length;
                var charsPerPage = settings.CharactersPerPage > 0
                    ? settings.CharactersPerPage
                    : PageCountConstants.DefaultCharactersPerPage;

                var estimatedPages = Math.Max(1, (int)Math.Ceiling((double)charCount / charsPerPage));

                _logger.LogDebug("DOCX file {FileName} estimated at {Pages} pages from content",
                    file.Name, estimatedPages);

                return PageCountResult.Successful(
                    estimatedPages,
                    $"OK – estimated pages based on {charsPerPage} chars per page; totalChars={charCount}");
            }

            return PageCountResult.Successful(1, "OK – DOCX treated as 1 page; content could not be read");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read DOCX {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read DOCX; exception: {ex.Message}");
        }
    }

    private PageCountResult GetDocPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            // Legacy DOC format - estimate based on file size
            var fileSize = file.Length;
            // Rough estimate: ~15KB per page for DOC files
            var estimatedPages = Math.Max(1, (int)Math.Ceiling(fileSize / 15360.0));

            _logger.LogDebug("DOC file {FileName} estimated at {Pages} pages based on file size",
                file.Name, estimatedPages);

            return PageCountResult.Successful(
                estimatedPages,
                $"OK – estimated pages based on file size ({fileSize} bytes); legacy DOC format");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process DOC {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to process DOC; exception: {ex.Message}");
        }
    }

    private PageCountResult GetRtfPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            // RTF - read as text and estimate
            var content = File.ReadAllText(file.FullName);

            // Remove RTF control codes for rough character count
            var textContent = System.Text.RegularExpressions.Regex.Replace(content, @"\\[a-z]+\d*\s?|\{|\}", "");
            var charCount = textContent.Length;
            var charsPerPage = settings.CharactersPerPage > 0
                ? settings.CharactersPerPage
                : PageCountConstants.DefaultCharactersPerPage;

            var estimatedPages = Math.Max(1, (int)Math.Ceiling((double)charCount / charsPerPage));

            _logger.LogDebug("RTF file {FileName} estimated at {Pages} pages from content",
                file.Name, estimatedPages);

            return PageCountResult.Successful(
                estimatedPages,
                $"OK – estimated pages based on {charsPerPage} chars per page; approxChars={charCount}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read RTF {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read RTF; exception: {ex.Message}");
        }
    }
}
