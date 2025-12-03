namespace PageCounterPro.Core.PageCountProviders;

using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for plain text files.
/// Uses character and line counting with configurable heuristics.
/// </summary>
public sealed class TextPageCountProvider : IPageCountProvider
{
    private readonly ILogger<TextPageCountProvider> _logger;

    public TextPageCountProvider(ILogger<TextPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.TextExtensions;

    public bool CanHandle(FileInfo file)
    {
        var extension = file.Extension.TrimStart('.').ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public PageCountResult GetPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            var content = File.ReadAllText(file.FullName);
            var totalLines = content.Split('\n').Length;
            var totalCharacters = content.Length;

            // Use lines per page for estimation
            var linesPerPage = settings.LinesPerPage > 0 ? settings.LinesPerPage : PageCountConstants.DefaultLinesPerPage;
            var pageCount = (int)Math.Ceiling((double)totalLines / linesPerPage);

            // Ensure at least 1 page for non-empty files
            if (pageCount == 0 && totalCharacters > 0)
            {
                pageCount = 1;
            }

            _logger.LogDebug("Text file {FileName} has {Lines} lines, estimated {Pages} pages",
                file.Name, totalLines, pageCount);

            return PageCountResult.Successful(
                pageCount,
                $"OK – estimated pages based on {linesPerPage} lines per page; totalLines={totalLines}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read text file {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read text file; exception: {ex.Message}");
        }
    }
}
