using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PageCounterPro.UI.ViewModels;

/// <summary>
/// View model for the history view.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<HistoryEntryViewModel> _historyEntries = new();

    [ObservableProperty]
    private HistoryEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private bool _hasEntries;

    public HistoryViewModel(IHistoryService historyService, ILogger<HistoryViewModel> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task RefreshHistoryAsync()
    {
        try
        {
            var entries = await _historyService.GetHistoryAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryEntries.Clear();
                foreach (var entry in entries)
                {
                    HistoryEntries.Add(new HistoryEntryViewModel
                    {
                        ScanId = entry.ScanId,
                        Timestamp = entry.Timestamp,
                        RootFolderPath = entry.RootFolderPath,
                        FolderName = Path.GetFileName(entry.RootFolderPath) ?? entry.RootFolderPath,
                        TotalFilesProcessed = entry.TotalFilesProcessed,
                        FilesWithErrors = entry.FilesWithErrors,
                        ExportFilePath = entry.ExportFilePath,
                        ExportFormat = entry.ExportFormat.ToString().ToUpperInvariant(),
                        IsComplete = entry.IsComplete,
                        Duration = TimeSpan.FromSeconds(entry.DurationSeconds)
                    });
                }
                HasEntries = HistoryEntries.Count > 0;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history");
        }
    }

    [RelayCommand]
    private void OpenExportFile()
    {
        if (SelectedEntry == null || string.IsNullOrEmpty(SelectedEntry.ExportFilePath))
            return;

        try
        {
            if (File.Exists(SelectedEntry.ExportFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedEntry.ExportFilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show(
                    "The export file no longer exists.",
                    "File Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open export file");
            MessageBox.Show(
                $"Could not open the file:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (SelectedEntry == null || string.IsNullOrEmpty(SelectedEntry.ExportFilePath))
            return;

        try
        {
            var folder = Path.GetDirectoryName(SelectedEntry.ExportFilePath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                if (File.Exists(SelectedEntry.ExportFilePath))
                {
                    Process.Start("explorer.exe", $"/select,\"{SelectedEntry.ExportFilePath}\"");
                }
                else
                {
                    Process.Start("explorer.exe", folder);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open export folder");
        }
    }

    [RelayCommand]
    private async Task DeleteEntryAsync()
    {
        if (SelectedEntry == null)
            return;

        var result = MessageBox.Show(
            "Are you sure you want to remove this history entry?\n\nThis will not delete the export file.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _historyService.RemoveEntryAsync(SelectedEntry.ScanId);
                HistoryEntries.Remove(SelectedEntry);
                HasEntries = HistoryEntries.Count > 0;
                _logger.LogInformation("Deleted history entry {ScanId}", SelectedEntry.ScanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete history entry");
            }
        }
    }

    [RelayCommand]
    private async Task ClearAllHistoryAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all history?\n\nThis will not delete any export files.",
            "Confirm Clear All",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _historyService.ClearHistoryAsync();
                HistoryEntries.Clear();
                HasEntries = false;
                _logger.LogInformation("Cleared all history entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear history");
            }
        }
    }
}

/// <summary>
/// View model for a single history entry.
/// </summary>
public partial class HistoryEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _scanId;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private string _rootFolderPath = string.Empty;

    [ObservableProperty]
    private string _folderName = string.Empty;

    [ObservableProperty]
    private int _totalFilesProcessed;

    [ObservableProperty]
    private int _filesWithErrors;

    [ObservableProperty]
    private string _exportFilePath = string.Empty;

    [ObservableProperty]
    private string _exportFormat = string.Empty;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private TimeSpan _duration;

    public string FormattedTimestamp => Timestamp.ToString("MMM dd, yyyy HH:mm");
    public string FormattedDuration => Duration.ToString(@"mm\:ss");
    public string StatusDisplay => IsComplete ? "Complete" : "Incomplete";
}
