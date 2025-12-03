namespace PageCounterPro.Infrastructure.Interfaces;

using PageCounterPro.Core.Models;

/// <summary>
/// Interface for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings GetSettings();

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    Task ResetToDefaultsAsync();
}
