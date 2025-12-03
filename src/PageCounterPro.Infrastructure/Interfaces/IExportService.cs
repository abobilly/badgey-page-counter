namespace PageCounterPro.Infrastructure.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for exporting scan results to files.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports scan results to a file in the specified format.
    /// </summary>
    /// <param name="result">The scan result to export.</param>
    /// <param name="format">The export format.</param>
    /// <param name="outputPath">Optional custom output path. If null, uses default location.</param>
    /// <returns>The path to the exported file.</returns>
    Task<string> ExportAsync(ScanResult result, ExportFormat format, string? outputPath = null);

    /// <summary>
    /// Gets the default export directory.
    /// </summary>
    string GetDefaultExportDirectory();
}
