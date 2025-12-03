namespace PageCounterPro.Core.Models;

/// <summary>
/// Represents a record of a past scan in the history.
/// </summary>
public sealed class ScanHistoryEntry
{
    /// <summary>
    /// Unique identifier for this scan.
    /// </summary>
    public Guid ScanId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the scan was performed.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The root folder that was scanned.
    /// </summary>
    public required string RootFolderPath { get; init; }

    /// <summary>
    /// Total number of files processed.
    /// </summary>
    public int TotalFilesProcessed { get; init; }

    /// <summary>
    /// Number of files with errors.
    /// </summary>
    public int FilesWithErrors { get; init; }

    /// <summary>
    /// Path to the exported file.
    /// </summary>
    public required string ExportFilePath { get; init; }

    /// <summary>
    /// Export format used.
    /// </summary>
    public ExportFormat ExportFormat { get; init; }

    /// <summary>
    /// Whether the scan was completed successfully.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Duration of the scan in seconds.
    /// </summary>
    public double DurationSeconds { get; init; }
}
