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
