using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using System.Windows;

namespace PageCounterPro.UI.ViewModels;

/// <summary>
/// View model for the settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IExportService _exportService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private ExportFormat _exportFormat;

    [ObservableProperty]
    private string? _customExportDirectory;

    [ObservableProperty]
    private int _maxParallelism;

    [ObservableProperty]
    private int _charactersPerPage;

    [ObservableProperty]
    private int _linesPerPage;

    [ObservableProperty]
    private bool _defaultIncludeSubfolders;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string? _statusMessage;

    public IReadOnlyList<ExportFormat> ExportFormats { get; } = Enum.GetValues<ExportFormat>();

    public SettingsViewModel(
        ISettingsService settingsService,
        IExportService exportService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _exportService = exportService;
        _logger = logger;

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.GetSettings();

        ExportFormat = settings.ExportFormat;
        CustomExportDirectory = settings.CustomExportDirectory;
        MaxParallelism = settings.MaxParallelism;
        CharactersPerPage = settings.CharactersPerPage;
        LinesPerPage = settings.LinesPerPage;
        DefaultIncludeSubfolders = settings.DefaultIncludeSubfolders;

        HasUnsavedChanges = false;
        StatusMessage = null;
    }

    partial void OnExportFormatChanged(ExportFormat value) => MarkAsChanged();
    partial void OnCustomExportDirectoryChanged(string? value) => MarkAsChanged();
    partial void OnMaxParallelismChanged(int value) => MarkAsChanged();
    partial void OnCharactersPerPageChanged(int value) => MarkAsChanged();
    partial void OnLinesPerPageChanged(int value) => MarkAsChanged();
    partial void OnDefaultIncludeSubfoldersChanged(bool value) => MarkAsChanged();

    private void MarkAsChanged()
    {
        HasUnsavedChanges = true;
        StatusMessage = "You have unsaved changes.";
    }

    [RelayCommand]
    private void BrowseExportDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Export Directory",
            InitialDirectory = CustomExportDirectory ?? _exportService.GetDefaultExportDirectory()
        };

        if (dialog.ShowDialog() == true)
        {
            CustomExportDirectory = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void ClearCustomExportDirectory()
    {
        CustomExportDirectory = null;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Validate settings
            if (MaxParallelism < 1)
            {
                MaxParallelism = 1;
            }
            else if (MaxParallelism > Environment.ProcessorCount * 2)
            {
                MaxParallelism = Environment.ProcessorCount * 2;
            }

            if (CharactersPerPage < 100)
            {
                CharactersPerPage = 100;
            }

            if (LinesPerPage < 10)
            {
                LinesPerPage = 10;
            }

            var settings = new AppSettings
            {
                ExportFormat = ExportFormat,
                CustomExportDirectory = CustomExportDirectory,
                MaxParallelism = MaxParallelism,
                CharactersPerPage = CharactersPerPage,
                LinesPerPage = LinesPerPage,
                DefaultIncludeSubfolders = DefaultIncludeSubfolders
            };

            await _settingsService.SaveSettingsAsync(settings);

            HasUnsavedChanges = false;
            StatusMessage = "Settings saved successfully!";
            _logger.LogInformation("Settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            StatusMessage = $"Failed to save settings: {ex.Message}";
            MessageBox.Show(
                $"Failed to save settings:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to defaults?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _settingsService.ResetToDefaultsAsync();
                LoadSettings();
                StatusMessage = "Settings reset to defaults.";
                _logger.LogInformation("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings");
                StatusMessage = $"Failed to reset settings: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void DiscardChanges()
    {
        LoadSettings();
        StatusMessage = "Changes discarded.";
    }
}
