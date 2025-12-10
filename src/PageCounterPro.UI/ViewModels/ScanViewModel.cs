using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using PageCounterPro.UI.Views;
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
    private ScanResult? _currentScanResult;

    // Accent colors for folder groups
    private static readonly string[] FolderAccentColors =
    {
        "#0047AB", // Cobalt (primary)
        "#0EA5E9", // Teal/cyan
        "#22C55E", // Green
        "#8B5CF6", // Purple
        "#EC4899", // Pink/magenta
        "#F97316", // Orange
        "#F59E0B", // Amber/gold
        "#EF4444", // Red
        "#003380"  // Cobalt dark
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
                // Check for unsupported/unconfigured file types and show dialog if needed
                var settings = _settingsService.GetSettings();
                result = await HandleUnconfiguredFileTypesAsync(result, settings);

                // Export results
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

    [RelayCommand(CanExecute = nameof(CanReExport))]
    private async Task ReExportAsync()
    {
        if (_currentScanResult == null || string.IsNullOrEmpty(LastExportPath))
        {
            return;
        }

        try
        {
            IsScanning = true;
            StatusMessage = "Re-exporting with updated page counts...";

            var updatedResult = BuildScanResultFromViewState();
            var settings = _settingsService.GetSettings();

            // Get the directory and use it as output path (will overwrite)
            var outputDir = Path.GetDirectoryName(LastExportPath);
            await _exportService.ExportAsync(updatedResult, settings.ExportFormat, outputDir);

            StatusMessage = "Re-export completed!";
            _logger.LogInformation("Re-export completed to {Path}", LastExportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-export failed");
            StatusMessage = $"Re-export failed: {ex.Message}";
            MessageBox.Show(
                $"Re-export failed:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private bool CanReExport() => _currentScanResult != null && !string.IsNullOrEmpty(LastExportPath) && !IsScanning;

    /// <summary>
    /// Builds a ScanResult from the current ViewModel state, using EffectivePageCount values.
    /// </summary>
    private ScanResult BuildScanResultFromViewState()
    {
        if (_currentScanResult == null)
        {
            throw new InvalidOperationException("No scan result available");
        }

        // Create a new result with updated file metadata
        var result = new ScanResult
        {
            RootFolderPath = _currentScanResult.RootFolderPath,
            StartTime = _currentScanResult.StartTime,
            EndTime = _currentScanResult.EndTime,
            TotalFilesFound = _currentScanResult.TotalFilesFound,
            FilesProcessed = _currentScanResult.FilesProcessed,
            FilesWithErrors = _currentScanResult.FilesWithErrors,
            WasCancelled = _currentScanResult.WasCancelled,
            IsComplete = _currentScanResult.IsComplete,
            ExportFilePath = _currentScanResult.ExportFilePath
        };

        // Build file list from current ViewModel state
        foreach (var folderGroup in FolderGroups)
        {
            foreach (var fileVm in folderGroup.Files)
            {
                result.Files.Add(new FileMetadata
                {
                    FullPath = fileVm.FullPath,
                    RootPath = fileVm.RootPath,
                    FileName = fileVm.FileName,
                    FileType = fileVm.FileType.ToLowerInvariant(),
                    FileSizeBytes = fileVm.FileSizeBytes,
                    PageCount = fileVm.EffectivePageCount,
                    ProcessedSuccessfully = fileVm.Status == "✓",
                    Notes = fileVm.Notes
                });
            }
        }

        return result;
    }

    private void UpdateRecentFiles(ScanResult result)
    {
        // Store for re-export
        _currentScanResult = result;

        // Define editable file type categories (images, videos, audio - files that count as 1 page)
        var editableCategories = new HashSet<string> { "Image", "Video", "Audio" };

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
                IsExpanded = false, // Collapsed by default
                ExportPath = result.ExportFilePath
            };

            foreach (var file in result.Files)
            {
                var category = GetFileTypeCategory(file.FileType.ToLowerInvariant());
                var isEditable = editableCategories.Contains(category);

                var fileVm = new RecentFileViewModel
                {
                    FileName = file.FileName,
                    FileType = file.FileType.ToUpperInvariant(),
                    PageCount = file.PageCount?.ToString() ?? "-",
                    Status = file.ProcessedSuccessfully ? "✓" : "✗",
                    Notes = file.Notes ?? string.Empty,
                    FullPath = file.FullPath,
                    RootPath = file.RootPath,
                    FileSizeBytes = file.FileSizeBytes,
                    OriginalPageCount = file.PageCount,
                    IsEditable = isEditable,
                    CountAsOnePage = file.PageCount > 0 // Default to current state
                };

                folderGroup.AddFile(fileVm);
            }

            // Calculate initial total from effective page counts
            folderGroup.RecalculateTotalPages();

            FolderGroups.Add(folderGroup);
        });
    }

    /// <summary>
    /// Checks for unconfigured file types and shows dialog if any are found.
    /// Shows dialog for both unsupported files AND "count as 1 page" types (images, videos, audio).
    /// Returns updated ScanResult with user preferences applied.
    /// </summary>
    private async Task<ScanResult> HandleUnconfiguredFileTypesAsync(ScanResult result, AppSettings settings)
    {
        // Categories that should prompt for configuration (these are "count as 1 page" types)
        var configurableCategories = new HashSet<string> { "Image", "Video", "Audio" };

        // Find all files that need configuration:
        // 1. Unsupported files (marked as such)
        // 2. Files in configurable categories (Image, Video, Audio) that haven't been configured yet
        var filesToConfigure = result.Files
            .Where(f =>
            {
                var ext = f.FileType.ToLowerInvariant();
                var category = GetFileTypeCategory(ext);

                // Check if this extension is already configured
                var isConfigured = settings.FileTypePreferences.TryGetValue(ext, out var pref) && pref.IsConfigured;
                if (isConfigured) return false;

                // Include if unsupported OR in a configurable category
                var isUnsupported = !f.ProcessedSuccessfully && f.Notes?.Contains("Unsupported") == true;
                var isConfigurableCategory = configurableCategories.Contains(category);

                return isUnsupported || isConfigurableCategory;
            })
            .GroupBy(f => f.FileType.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => (Category: GetFileTypeCategory(g.Key), FileCount: g.Count(), MetadataReadable: true)
            );

        if (filesToConfigure.Count == 0)
        {
            return result;
        }

        // Show dialog on UI thread
        var dialogResult = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var vm = new FileTypeOptionsViewModel();
            vm.PopulateFromScan(filesToConfigure, settings.FileTypePreferences);

            var dialog = new FileTypeOptionsDialog(vm)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();

            return vm.DialogResult ? vm.GetPreferences() : null;
        });

        if (dialogResult != null)
        {
            // Save user preferences
            foreach (var kvp in dialogResult)
            {
                settings.FileTypePreferences[kvp.Key] = kvp.Value;
            }
            await _settingsService.SaveSettingsAsync(settings);
            _logger.LogInformation("Saved file type preferences for {Count} extensions", dialogResult.Count);
        }

        // Apply preferences to update results
        return ApplyFileTypePreferences(result, settings);
    }

    /// <summary>
    /// Applies user file type preferences to update scan results.
    /// </summary>
    private ScanResult ApplyFileTypePreferences(ScanResult result, AppSettings settings)
    {
        // Categories that can be toggled
        var configurableCategories = new HashSet<string> { "Image", "Video", "Audio" };

        foreach (var file in result.Files)
        {
            var ext = file.FileType.ToLowerInvariant();
            var category = GetFileTypeCategory(ext);

            // Apply preferences to configurable file types
            if (configurableCategories.Contains(category) || (!file.ProcessedSuccessfully && file.Notes?.Contains("Unsupported") == true))
            {
                if (settings.FileTypePreferences.TryGetValue(ext, out var pref) && pref.IsConfigured)
                {
                    if (pref.CountAs1Page)
                    {
                        // Update the file metadata to count as 1 page
                        file.PageCount = 1;
                        file.ProcessedSuccessfully = true;
                        file.Notes = string.Empty;
                    }
                    else
                    {
                        // Keep as 0 pages but mark as processed
                        file.PageCount = 0;
                        file.ProcessedSuccessfully = true;
                        file.Notes = "Excluded from count";
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Determines the category for a file extension.
    /// </summary>
    private static string GetFileTypeCategory(string extension)
    {
        var videoExtensions = new HashSet<string> { "mov", "mp4", "avi", "wmv", "mkv", "flv", "webm", "m4v", "3gp" };
        var imageExtensions = new HashSet<string> { "jpg", "jpeg", "png", "gif", "bmp", "tif", "tiff", "webp", "ico", "heic", "heif", "raw", "svg" };
        var audioExtensions = new HashSet<string> { "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a" };
        var documentExtensions = new HashSet<string> { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf" };

        if (videoExtensions.Contains(extension)) return "Video";
        if (imageExtensions.Contains(extension)) return "Image";
        if (audioExtensions.Contains(extension)) return "Audio";
        if (documentExtensions.Contains(extension)) return "Document";
        return "Unknown";
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

    /// <summary>
    /// Adds a file to the folder group and subscribes to its property changes.
    /// </summary>
    public void AddFile(RecentFileViewModel file)
    {
        file.PropertyChanged += OnFilePropertyChanged;
        Files.Add(file);
    }

    /// <summary>
    /// Recalculates total pages from all files' effective page counts.
    /// </summary>
    public void RecalculateTotalPages()
    {
        TotalPages = Files.Sum(f => f.EffectivePageCount);
    }

    private void OnFilePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecentFileViewModel.EffectivePageCount) ||
            e.PropertyName == nameof(RecentFileViewModel.CountAsOnePage))
        {
            RecalculateTotalPages();
        }
    }

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

    [ObservableProperty]
    private string _notes = string.Empty;

    // Properties for per-file page count toggle and re-export
    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private string _rootPath = string.Empty;

    [ObservableProperty]
    private long _fileSizeBytes;

    [ObservableProperty]
    private int? _originalPageCount;

    [ObservableProperty]
    private bool _countAsOnePage = true;

    [ObservableProperty]
    private bool _isEditable;

    /// <summary>
    /// Gets the effective page count based on toggle state.
    /// If editable and toggled on: 1 page. If toggled off: 0 pages.
    /// If not editable: original page count.
    /// </summary>
    public int EffectivePageCount => IsEditable
        ? (CountAsOnePage ? 1 : 0)
        : (OriginalPageCount ?? 0);

    partial void OnCountAsOnePageChanged(bool value)
    {
        OnPropertyChanged(nameof(EffectivePageCount));
        // Update the displayed page count
        PageCount = EffectivePageCount.ToString();
    }
}
