namespace PageCounterPro.Infrastructure.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for managing scan history.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Adds a new entry to the scan history.
    /// </summary>
    /// <param name="entry">The history entry to add.</param>
    Task AddEntryAsync(ScanHistoryEntry entry);

    /// <summary>
    /// Gets all history entries, ordered by timestamp descending.
    /// </summary>
    Task<IReadOnlyList<ScanHistoryEntry>> GetHistoryAsync();

    /// <summary>
    /// Clears all history entries.
    /// </summary>
    Task ClearHistoryAsync();

    /// <summary>
    /// Removes a specific history entry.
    /// </summary>
    /// <param name="scanId">The ID of the scan to remove.</param>
    Task RemoveEntryAsync(Guid scanId);
}
