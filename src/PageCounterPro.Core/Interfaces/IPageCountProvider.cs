namespace PageCounterPro.Core.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for page count providers that can determine page counts for specific file types.
/// </summary>
public interface IPageCountProvider
{
    /// <summary>
    /// Gets the file extensions this provider supports (lowercase, without dot).
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Determines if this provider can handle the specified file.
    /// </summary>
    /// <param name="file">The file to check.</param>
    /// <returns>True if this provider can process the file.</returns>
    bool CanHandle(FileInfo file);

    /// <summary>
    /// Gets the page count for the specified file.
    /// </summary>
    /// <param name="file">The file to analyze.</param>
    /// <param name="settings">Application settings for heuristic configuration.</param>
    /// <returns>The page count result.</returns>
    PageCountResult GetPageCount(FileInfo file, AppSettings settings);
}
