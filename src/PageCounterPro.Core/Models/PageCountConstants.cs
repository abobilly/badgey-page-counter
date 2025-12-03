namespace PageCounterPro.Core.Models;

/// <summary>
/// Constants for page counting heuristics.
/// </summary>
public static class PageCountConstants
{
    /// <summary>
    /// Default number of characters per page for text estimation.
    /// Based on typical 8.5x11 page with standard margins and 12pt font.
    /// </summary>
    public const int DefaultCharactersPerPage = 1800;

    /// <summary>
    /// Default number of lines per page for text estimation.
    /// </summary>
    public const int DefaultLinesPerPage = 50;

    /// <summary>
    /// Default number of rows per page for spreadsheet estimation.
    /// </summary>
    public const int DefaultRowsPerPage = 50;

    /// <summary>
    /// Default number of columns per page for spreadsheet estimation.
    /// </summary>
    public const int DefaultColumnsPerPage = 10;

    /// <summary>
    /// Supported PDF extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> PdfExtensions = new[] { "pdf" };

    /// <summary>
    /// Supported Word document extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> WordExtensions = new[] { "doc", "docx", "rtf" };

    /// <summary>
    /// Supported plain text extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> TextExtensions = new[] { "txt", "log", "md", "json", "xml", "html", "htm", "css", "js", "cs", "py", "java" };

    /// <summary>
    /// Supported spreadsheet extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> SpreadsheetExtensions = new[] { "xls", "xlsx", "csv" };

    /// <summary>
    /// Supported image extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> ImageExtensions = new[] { "jpg", "jpeg", "png", "gif", "bmp", "tif", "tiff", "webp", "ico" };

    /// <summary>
    /// Supported video extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> VideoExtensions = new[] { "mov", "mp4", "avi", "wmv", "mkv", "flv", "webm" };

    /// <summary>
    /// Supported PowerPoint extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> PowerPointExtensions = new[] { "ppt", "pptx" };

    /// <summary>
    /// All supported extensions.
    /// </summary>
    public static readonly IReadOnlyList<string> AllSupportedExtensions;

    static PageCountConstants()
    {
        var all = new List<string>();
        all.AddRange(PdfExtensions);
        all.AddRange(WordExtensions);
        all.AddRange(TextExtensions);
        all.AddRange(SpreadsheetExtensions);
        all.AddRange(ImageExtensions);
        all.AddRange(VideoExtensions);
        all.AddRange(PowerPointExtensions);
        AllSupportedExtensions = all.AsReadOnly();
    }
}
