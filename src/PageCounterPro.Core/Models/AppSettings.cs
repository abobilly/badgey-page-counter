namespace PageCounterPro.Core.Models;

/// <summary>
/// Application settings that can be configured by the user.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// The preferred export format.
    /// </summary>
    public ExportFormat ExportFormat { get; set; } = ExportFormat.Xlsx;

    /// <summary>
    /// Custom export directory. If null, uses default app data location.
    /// </summary>
    public string? CustomExportDirectory { get; set; }

    /// <summary>
    /// Maximum degree of parallelism for file processing.
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Number of characters per page for text file estimation.
    /// </summary>
    public int CharactersPerPage { get; set; } = PageCountConstants.DefaultCharactersPerPage;

    /// <summary>
    /// Number of lines per page for text file estimation.
    /// </summary>
    public int LinesPerPage { get; set; } = PageCountConstants.DefaultLinesPerPage;

    /// <summary>
    /// Whether to include subfolders by default.
    /// </summary>
    public bool DefaultIncludeSubfolders { get; set; } = true;

    /// <summary>
    /// User preferences for file types (how to handle unknown/special file types).
    /// Key is the lowercase file extension without dot (e.g., "avi", "mov").
    /// </summary>
    public Dictionary<string, FileTypePreference> FileTypePreferences { get; set; } = new();
}

/// <summary>
/// User preference for how to handle a specific file type.
/// </summary>
public sealed class FileTypePreference
{
    /// <summary>
    /// Whether to count this file type as 1 page.
    /// </summary>
    public bool CountAs1Page { get; set; } = true;

    /// <summary>
    /// Whether to show duration for video files (if available).
    /// </summary>
    public bool ShowDuration { get; set; } = true;

    /// <summary>
    /// Whether this preference has been configured by the user.
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// The category of this file type (e.g., "Video", "Image", "Unknown").
    /// </summary>
    public string Category { get; set; } = "Unknown";
}

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Excel XLSX format.
    /// </summary>
    Xlsx,

    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    Csv
}
