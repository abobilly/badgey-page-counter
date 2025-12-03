namespace PageCounterPro.Infrastructure.Services;

using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

/// <summary>
/// Service for exporting scan results to XLSX and CSV files.
/// </summary>
public sealed class ExportService : IExportService
{
    private const string AppName = "PageCounterPro";
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public string GetDefaultExportDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var exportDir = Path.Combine(localAppData, AppName, "Exports");

        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
        }

        return exportDir;
    }

    public async Task<string> ExportAsync(ScanResult result, ExportFormat format, string? outputPath = null)
    {
        var directory = outputPath ?? GetDefaultExportDirectory();

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var folderName = SanitizeFileName(Path.GetFileName(result.RootFolderPath));
        var extension = format == ExportFormat.Xlsx ? "xlsx" : "csv";
        var fileName = $"PageCount_{folderName}_{timestamp}.{extension}";
        var filePath = Path.Combine(directory, fileName);

        _logger.LogInformation("Exporting to {Path} in {Format} format", filePath, format);

        if (format == ExportFormat.Xlsx)
        {
            await ExportToXlsxAsync(result, filePath);
        }
        else
        {
            await ExportToCsvAsync(result, filePath);
        }

        _logger.LogInformation("Export completed: {Path}", filePath);
        return filePath;
    }

    private async Task ExportToXlsxAsync(ScanResult result, string filePath)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Page Counts");

            // Add headers
            var headers = new[] { "FullPath", "RootPath", "FileName", "FileSizeBytes", "FileType", "PageCount", "Notes" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Add data rows
            var row = 2;
            foreach (var file in result.Files)
            {
                worksheet.Cell(row, 1).Value = file.FullPath;
                worksheet.Cell(row, 2).Value = file.RootPath;
                worksheet.Cell(row, 3).Value = file.FileName;
                worksheet.Cell(row, 4).Value = file.FileSizeBytes;
                worksheet.Cell(row, 5).Value = file.FileType;

                if (file.PageCount.HasValue)
                {
                    worksheet.Cell(row, 6).Value = file.PageCount.Value;
                }

                worksheet.Cell(row, 7).Value = file.Notes;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add summary info
            var summarySheet = workbook.Worksheets.Add("Summary");
            summarySheet.Cell(1, 1).Value = "Scan Summary";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;

            summarySheet.Cell(2, 1).Value = "Root Folder:";
            summarySheet.Cell(2, 2).Value = result.RootFolderPath;

            summarySheet.Cell(3, 1).Value = "Scan Date:";
            summarySheet.Cell(3, 2).Value = result.StartTime.ToString("yyyy-MM-dd HH:mm:ss");

            summarySheet.Cell(4, 1).Value = "Total Files:";
            summarySheet.Cell(4, 2).Value = result.TotalFilesFound;

            summarySheet.Cell(5, 1).Value = "Files Processed:";
            summarySheet.Cell(5, 2).Value = result.FilesProcessed;

            summarySheet.Cell(6, 1).Value = "Files with Errors:";
            summarySheet.Cell(6, 2).Value = result.FilesWithErrors;

            summarySheet.Cell(7, 1).Value = "Scan Duration:";
            summarySheet.Cell(7, 2).Value = result.Duration.ToString(@"hh\:mm\:ss");

            summarySheet.Cell(8, 1).Value = "Status:";
            summarySheet.Cell(8, 2).Value = result.WasCancelled ? "Cancelled" : (result.IsComplete ? "Complete" : "Incomplete");

            summarySheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        });
    }

    private async Task ExportToCsvAsync(ScanResult result, string filePath)
    {
        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Write header
        csv.WriteField("FullPath");
        csv.WriteField("RootPath");
        csv.WriteField("FileName");
        csv.WriteField("FileSizeBytes");
        csv.WriteField("FileType");
        csv.WriteField("PageCount");
        csv.WriteField("Notes");
        await csv.NextRecordAsync();

        // Write data
        foreach (var file in result.Files)
        {
            csv.WriteField(file.FullPath);
            csv.WriteField(file.RootPath);
            csv.WriteField(file.FileName);
            csv.WriteField(file.FileSizeBytes);
            csv.WriteField(file.FileType);
            csv.WriteField(file.PageCount?.ToString() ?? string.Empty);
            csv.WriteField(file.Notes);
            await csv.NextRecordAsync();
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "export" : sanitized;
    }
}
