namespace PageCounterPro.Infrastructure.Services;

using PageCounterPro.Core.Models;
using PageCounterPro.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Service for managing application settings using JSON file storage.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string AppName = "PageCounterPro";
    private const string SettingsFileName = "Settings.json";
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private AppSettings _cachedSettings;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataDir = Path.Combine(localAppData, AppName);

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _settingsFilePath = Path.Combine(appDataDir, SettingsFileName);
        _cachedSettings = LoadSettings();
    }

    public AppSettings GetSettings()
    {
        lock (_lock)
        {
            return _cachedSettings;
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        lock (_lock)
        {
            _cachedSettings = settings;
        }

        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        var defaultSettings = new AppSettings();
        await SaveSettingsAsync(defaultSettings);
        _logger.LogInformation("Settings reset to defaults");
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found, using defaults");
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults");
                return new AppSettings();
            }

            _logger.LogInformation("Settings loaded successfully");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            return new AppSettings();
        }
    }
}
