namespace PageCounterPro.Core.Models;

/// <summary>
/// Options for configuring a folder scan operation.
/// </summary>
public sealed class ScanOptions
{
    /// <summary>
    /// The root folder path to scan.
    /// </summary>
    public required string RootFolderPath { get; init; }

    /// <summary>
    /// Whether to include subfolders in the scan.
    /// </summary>
    public bool IncludeSubfolders { get; init; } = true;

    /// <summary>
    /// Maximum folder depth to scan. Null means unlimited.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// File extensions to include (lowercase, without dot). Null or empty means include all.
    /// </summary>
    public IReadOnlyList<string>? FileTypeFilter { get; init; }
}
