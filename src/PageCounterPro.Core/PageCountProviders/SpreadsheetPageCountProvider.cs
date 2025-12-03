namespace PageCounterPro.Core.PageCountProviders;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Page count provider for spreadsheet files (xlsx, xls, csv).
/// </summary>
public sealed class SpreadsheetPageCountProvider : IPageCountProvider
{
    private readonly ILogger<SpreadsheetPageCountProvider> _logger;

    public SpreadsheetPageCountProvider(ILogger<SpreadsheetPageCountProvider> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => PageCountConstants.SpreadsheetExtensions;

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
            "xlsx" => GetXlsxPageCount(file),
            "xls" => GetXlsPageCount(file),
            "csv" => GetCsvPageCount(file, settings),
            _ => PageCountResult.Unsupported()
        };
    }

    private PageCountResult GetXlsxPageCount(FileInfo file)
    {
        try
        {
            using var document = SpreadsheetDocument.Open(file.FullName, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart == null)
            {
                return PageCountResult.Failed("Error: XLSX workbook part not found");
            }

            int totalPages = 0;
            int sheetCount = 0;
            var sheetDetails = new List<string>();

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                sheetCount++;
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData == null) continue;

                var rows = sheetData.Elements<Row>().ToList();
                var rowCount = rows.Count;
                var maxColumns = rows.Any()
                    ? rows.Max(r => r.Elements<Cell>().Count())
                    : 0;

                // Estimate pages based on rows and columns
                var rowPages = (int)Math.Ceiling((double)rowCount / PageCountConstants.DefaultRowsPerPage);
                var colPages = (int)Math.Ceiling((double)maxColumns / PageCountConstants.DefaultColumnsPerPage);
                var sheetPages = Math.Max(1, rowPages * Math.Max(1, colPages));

                totalPages += sheetPages;
                sheetDetails.Add($"Sheet{sheetCount}:{rowCount}rows");
            }

            if (totalPages == 0) totalPages = 1;

            _logger.LogDebug("XLSX file {FileName} has {Sheets} sheets, estimated {Pages} pages",
                file.Name, sheetCount, totalPages);

            return PageCountResult.Successful(
                totalPages,
                $"OK – estimated {totalPages} pages across {sheetCount} sheet(s); {string.Join(", ", sheetDetails)}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read XLSX {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read XLSX; exception: {ex.Message}");
        }
    }

    private PageCountResult GetXlsPageCount(FileInfo file)
    {
        // XLS (legacy format) - return estimate based on file size
        try
        {
            var fileSize = file.Length;
            // Rough estimate: ~10KB per page for XLS files
            var estimatedPages = Math.Max(1, (int)Math.Ceiling(fileSize / 10240.0));

            _logger.LogDebug("XLS file {FileName} estimated at {Pages} pages based on file size",
                file.Name, estimatedPages);

            return PageCountResult.Successful(
                estimatedPages,
                $"OK – estimated pages based on file size ({fileSize} bytes); legacy XLS format");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process XLS {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to process XLS; exception: {ex.Message}");
        }
    }

    private PageCountResult GetCsvPageCount(FileInfo file, AppSettings settings)
    {
        try
        {
            var lineCount = File.ReadLines(file.FullName).Count();
            var linesPerPage = settings.LinesPerPage > 0 ? settings.LinesPerPage : PageCountConstants.DefaultLinesPerPage;
            var pageCount = Math.Max(1, (int)Math.Ceiling((double)lineCount / linesPerPage));

            _logger.LogDebug("CSV file {FileName} has {Lines} lines, estimated {Pages} pages",
                file.Name, lineCount, pageCount);

            return PageCountResult.Successful(
                pageCount,
                $"OK – estimated pages based on {linesPerPage} lines per page; totalLines={lineCount}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read CSV {FileName}", file.Name);
            return PageCountResult.Failed($"Error: failed to read CSV; exception: {ex.Message}");
        }
    }
}
