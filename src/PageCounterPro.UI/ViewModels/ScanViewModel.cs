using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PageCounterPro.UI.ViewModels;

/// <summary>
/// View model for the scan view.
/// </summary>
public partial class ScanViewModel : ObservableObject
{
    private readonly IPageCountService _pageCountService;
    private readonly IExportService _exportService;
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ScanViewModel> _logger;

    private CancellationTokenSource? _cancellationTokenSource;

    // Accent colors for folder groups
    private static readonly string[] FolderAccentColors =
    {
        "#0EA5E9", // Teal/cyan
        "#22C55E", // Green
        "#8B5CF6", // Purple
        "#EC4899", // Pink/magenta
        "#F97316", // Orange
        "#F59E0B", // Amber/gold
        "#EF4444", // Red
        "#2563EB", // Secondary blue
        "#1D4ED8"  // Primary blue (last since closest to app accent)
    };

    [ObservableProperty]
    private string? _selectedFolderPath;

    [ObservableProperty]
    private bool _includeSubfolders = true;

    [ObservableProperty]
    private int? _maxDepth;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _canStartScan;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private int _processedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string? _currentFileName;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _lastExportPath;

    [ObservableProperty]
    private ObservableCollection<FolderGroupViewModel> _folderGroups = new();

    public ScanViewModel(
        IPageCountService pageCountService,
        IExportService exportService,
        IHistoryService historyService,
        ISettingsService settingsService,
        ILogger<ScanViewModel> logger)
    {
        _pageCountService = pageCountService;
        _exportService = exportService;
        _historyService = historyService;
        _settingsService = settingsService;
        _logger = logger;

        var settings = _settingsService.GetSettings();
        IncludeSubfolders = settings.DefaultIncludeSubfolders;
    }

    partial void OnSelectedFolderPathChanged(string? value)
    {
        UpdateCanStartScan();
    }

    partial void OnIsScanningChanged(bool value)
    {
        UpdateCanStartScan();
    }

    private void UpdateCanStartScan()
    {
        CanStartScan = !string.IsNullOrEmpty(SelectedFolderPath) && !IsScanning;
    }

    [RelayCommand]
    private void SelectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder to Scan",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFolderPath = dialog.FolderName;
            StatusMessage = $"Selected: {SelectedFolderPath}";
            _logger.LogInformation("Folder selected: {Path}", SelectedFolderPath);
        }
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolderPath) || IsScanning)
            return;

        _logger.LogInformation("Starting scan of {Path}", SelectedFolderPath);

        IsScanning = true;
        ProgressPercentage = 0;
        ProcessedCount = 0;
        TotalCount = 0;
        CurrentFileName = null;
        StatusMessage = "Discovering files...";
        IsIndeterminate = true;
        FolderGroups.Clear();

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var options = new ScanOptions
            {
                RootFolderPath = SelectedFolderPath,
                IncludeSubfolders = IncludeSubfolders,
                MaxDepth = MaxDepth
            };

            var progress = new Progress<ScanProgress>(p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (p.IsEnumerating)
                    {
                        IsIndeterminate = true;
                        ProcessedCount = p.ProcessedCount;
                        TotalCount = 0;
                        StatusMessage = p.StatusMessage ?? $"Discovering files... {p.ProcessedCount} found";
                    }
                    else
                    {
                        IsIndeterminate = false;
                        TotalCount = p.TotalCount;
                        ProcessedCount = p.ProcessedCount;
                        ProgressPercentage = p.ProgressPercentage;
                        CurrentFileName = p.CurrentFile;
                        StatusMessage = p.StatusMessage ?? $"Processing {ProcessedCount} of {TotalCount} files...";
                    }
                });
            });

            var result = await _pageCountService.ExecuteScanAsync(
                options,
                progress,
                _cancellationTokenSource.Token);

            if (result.WasCancelled)
            {
                StatusMessage = $"Scan cancelled. Processed {result.FilesProcessed} of {result.TotalFilesFound} files.";
                _logger.LogInformation("Scan cancelled");
            }
            else
            {
                // Export results
                var settings = _settingsService.GetSettings();
                var exportPath = await _exportService.ExportAsync(result, settings.ExportFormat, settings.CustomExportDirectory);
                result.ExportFilePath = exportPath;
                LastExportPath = exportPath;

                // Add to history
                var historyEntry = new ScanHistoryEntry
                {
                    ScanId = result.ScanId,
                    Timestamp = result.StartTime,
                    RootFolderPath = result.RootFolderPath,
                    TotalFilesProcessed = result.FilesProcessed,
                    FilesWithErrors = result.FilesWithErrors,
                    ExportFilePath = exportPath,
                    ExportFormat = settings.ExportFormat,
                    IsComplete = result.IsComplete,
                    DurationSeconds = result.Duration.TotalSeconds
                };
                await _historyService.AddEntryAsync(historyEntry);

                StatusMessage = $"Scan complete! Processed {result.FilesProcessed} files. Export saved.";
                _logger.LogInformation("Scan completed successfully");

                // Update recent files display
                UpdateRecentFiles(result);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed");
            StatusMessage = $"Scan failed: {ex.Message}";
            MessageBox.Show(
                $"An error occurred during the scan:\n\n{ex.Message}",
                "Scan Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling scan...";
        _logger.LogInformation("Scan cancellation requested");
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (string.IsNullOrEmpty(LastExportPath) || !File.Exists(LastExportPath))
        {
            var defaultDir = _exportService.GetDefaultExportDirectory();
            if (Directory.Exists(defaultDir))
            {
                Process.Start("explorer.exe", defaultDir);
            }
            return;
        }

        var folder = Path.GetDirectoryName(LastExportPath);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            Process.Start("explorer.exe", $"/select,\"{LastExportPath}\"");
        }
    }

    private void UpdateRecentFiles(ScanResult result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            FolderGroups.Clear();

            // Show just ONE group for the root scan folder (not subfolders)
            var folderName = Path.GetFileName(result.RootFolderPath);
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = result.RootFolderPath; // Use full path for root drives
            }

            var accentColor = FolderAccentColors[0]; // Use first accent color

            var folderGroup = new FolderGroupViewModel
            {
                FolderName = folderName,
                FolderPath = result.RootFolderPath,
                AccentColor = accentColor,
                FileCount = result.Files.Count,
                TotalPages = result.Files.Sum(f => f.PageCount ?? 0),
                IsExpanded = false, // Collapsed by default
                ExportPath = result.ExportFilePath
            };

            foreach (var file in result.Files)
            {
                folderGroup.Files.Add(new RecentFileViewModel
                {
                    FileName = file.FileName,
                    FileType = file.FileType.ToUpperInvariant(),
                    PageCount = file.PageCount?.ToString() ?? "-",
                    Status = file.ProcessedSuccessfully ? "✓" : "✗"
                });
            }

            FolderGroups.Add(folderGroup);
        });
    }
}

/// <summary>
/// View model for a folder group in the scan results.
/// </summary>
public partial class FolderGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _folderName = string.Empty;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _accentColor = "#1D4ED8";

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string? _exportPath;

    [ObservableProperty]
    private ObservableCollection<RecentFileViewModel> _files = new();

    public Brush AccentBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(AccentColor));

    [RelayCommand]
    private void OpenFolder()
    {
        if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
        {
            Process.Start("explorer.exe", FolderPath);
        }
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (!string.IsNullOrEmpty(ExportPath) && File.Exists(ExportPath))
        {
            var folder = Path.GetDirectoryName(ExportPath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                Process.Start("explorer.exe", $"/select,\"{ExportPath}\"");
            }
        }
    }
}

/// <summary>
/// View model for recently processed files display.
/// </summary>
public partial class RecentFileViewModel : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileType = string.Empty;

    [ObservableProperty]
    private string _pageCount = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;
}
