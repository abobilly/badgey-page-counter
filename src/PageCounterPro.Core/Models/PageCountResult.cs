namespace PageCounterPro.Core.Models;

/// <summary>
/// Represents the result of a page count operation for a single file.
/// </summary>
public sealed class PageCountResult
{
    /// <summary>
    /// The calculated page count, or null if it could not be determined.
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Additional notes about the page count operation (e.g., method used, errors, runtime for videos).
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the page count was successfully determined.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Creates a successful page count result.
    /// </summary>
    public static PageCountResult Successful(int pageCount, string notes) => new()
    {
        PageCount = pageCount,
        Notes = notes,
        Success = true
    };

    /// <summary>
    /// Creates a failed page count result.
    /// </summary>
    public static PageCountResult Failed(string notes) => new()
    {
        PageCount = null,
        Notes = notes,
        Success = false
    };

    /// <summary>
    /// Creates an unsupported file type result.
    /// </summary>
    public static PageCountResult Unsupported() => new()
    {
        PageCount = null,
        Notes = "Unsupported file type for page counting",
        Success = false
    };
}
