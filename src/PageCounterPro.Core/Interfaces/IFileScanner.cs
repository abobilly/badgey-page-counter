namespace PageCounterPro.Core.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for the file scanner service.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Scans a folder for files based on the provided options.
    /// </summary>
    /// <param name="options">Scan configuration options.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Token to support cancellation.</param>
    /// <returns>An async enumerable of file info objects.</returns>
    IAsyncEnumerable<FileInfo> ScanFilesAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total file count for progress calculation.
    /// </summary>
    /// <param name="options">Scan configuration options.</param>
    /// <param name="cancellationToken">Token to support cancellation.</param>
    /// <returns>The total number of files.</returns>
    Task<int> GetTotalFileCountAsync(ScanOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for scan operations.
/// </summary>
public sealed class ScanProgress
{
    /// <summary>
    /// Current file being processed.
    /// </summary>
    public string? CurrentFile { get; init; }

    /// <summary>
    /// Number of files processed so far.
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// Total number of files to process.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Whether we're still counting files (indeterminate progress).
    /// </summary>
    public bool IsEnumerating { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;

    /// <summary>
    /// Current status message.
    /// </summary>
    public string? StatusMessage { get; init; }
}
