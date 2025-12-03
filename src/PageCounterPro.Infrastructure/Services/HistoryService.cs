namespace PageCounterPro.Infrastructure.Services;

using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Service for managing scan history using JSON file storage.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private const string AppName = "PageCounterPro";
    private const string HistoryFileName = "History.json";
    private readonly ILogger<HistoryService> _logger;
    private readonly string _historyFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HistoryService(ILogger<HistoryService> logger)
    {
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataDir = Path.Combine(localAppData, AppName);

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _historyFilePath = Path.Combine(appDataDir, HistoryFileName);
    }

    public async Task AddEntryAsync(ScanHistoryEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadEntriesAsync();
            entries.Add(entry);

            // Keep only the last 100 entries
            if (entries.Count > 100)
            {
                entries = entries.OrderByDescending(e => e.Timestamp).Take(100).ToList();
            }

            await SaveEntriesAsync(entries);
            _logger.LogInformation("Added history entry for scan {ScanId}", entry.ScanId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<ScanHistoryEntry>> GetHistoryAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadEntriesAsync();
            return entries.OrderByDescending(e => e.Timestamp).ToList().AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearHistoryAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await SaveEntriesAsync(new List<ScanHistoryEntry>());
            _logger.LogInformation("Cleared all history entries");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveEntryAsync(Guid scanId)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadEntriesAsync();
            var removed = entries.RemoveAll(e => e.ScanId == scanId);

            if (removed > 0)
            {
                await SaveEntriesAsync(entries);
                _logger.LogInformation("Removed history entry {ScanId}", scanId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<ScanHistoryEntry>> LoadEntriesAsync()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return new List<ScanHistoryEntry>();
            }

            var json = await File.ReadAllTextAsync(_historyFilePath);
            return JsonSerializer.Deserialize<List<ScanHistoryEntry>>(json, JsonOptions)
                   ?? new List<ScanHistoryEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load history, returning empty list");
            return new List<ScanHistoryEntry>();
        }
    }

    private async Task SaveEntriesAsync(List<ScanHistoryEntry> entries)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save history");
            throw;
        }
    }
}
