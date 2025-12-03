namespace PageCounterPro.Core.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for the page counting orchestration service.
/// </summary>
public interface IPageCountService
{
    /// <summary>
    /// Executes a complete folder scan and page count operation.
    /// </summary>
    /// <param name="options">Scan configuration options.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Token to support cancellation.</param>
    /// <returns>The complete scan result.</returns>
    Task<ScanResult> ExecuteScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the page count for a single file.
    /// </summary>
    /// <param name="file">The file to analyze.</param>
    /// <returns>The page count result.</returns>
    PageCountResult GetPageCount(FileInfo file);
}
