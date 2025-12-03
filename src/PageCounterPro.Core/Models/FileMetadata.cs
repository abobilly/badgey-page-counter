namespace PageCounterPro.Core.Models;

/// <summary>
/// Represents metadata and page count information for a single file.
/// </summary>
public sealed class FileMetadata
{
    /// <summary>
    /// The full file path including filename.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// The directory path only, excluding the filename.
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// The filename only, with extension.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// File extension normalized and lowercased (e.g., pdf, docx, txt).
    /// </summary>
    public required string FileType { get; init; }

    /// <summary>
    /// The number of pages, or null if unknown.
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Additional notes (errors, metadata, etc.).
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>
    /// Whether this file was processed successfully.
    /// </summary>
    public bool ProcessedSuccessfully { get; init; }
}
