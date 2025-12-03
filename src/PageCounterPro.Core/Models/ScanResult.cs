namespace PageCounterPro.Core.Models;

/// <summary>
/// Represents the result of a complete folder scan.
/// </summary>
public sealed class ScanResult
{
    /// <summary>
    /// Unique identifier for this scan.
    /// </summary>
    public Guid ScanId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the scan started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Timestamp when the scan completed.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// The root folder that was scanned.
    /// </summary>
    public required string RootFolderPath { get; init; }

    /// <summary>
    /// Total number of files found.
    /// </summary>
    public int TotalFilesFound { get; set; }

    /// <summary>
    /// Number of files successfully processed.
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Number of files with errors.
    /// </summary>
    public int FilesWithErrors { get; set; }

    /// <summary>
    /// Whether the scan was cancelled.
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Whether the scan completed successfully.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Path to the exported file, if any.
    /// </summary>
    public string? ExportFilePath { get; set; }

    /// <summary>
    /// All file metadata collected during the scan.
    /// </summary>
    public List<FileMetadata> Files { get; } = new();

    /// <summary>
    /// Duration of the scan.
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;
}
