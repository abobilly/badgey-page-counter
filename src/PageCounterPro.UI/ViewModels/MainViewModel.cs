using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace PageCounterPro.UI.ViewModels;

/// <summary>
/// Main view model that coordinates navigation between views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentView = "Scan";

    private readonly ScanViewModel _scanViewModel;
    private readonly HistoryViewModel _historyViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    public MainViewModel()
    {
        _scanViewModel = App.Services.GetRequiredService<ScanViewModel>();
        _historyViewModel = App.Services.GetRequiredService<HistoryViewModel>();
        _settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();

        // Start with Scan view
        CurrentViewModel = _scanViewModel;
    }

    [RelayCommand]
    private void NavigateToScan()
    {
        CurrentView = "Scan";
        CurrentViewModel = _scanViewModel;
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        CurrentView = "History";
        _historyViewModel.RefreshHistoryCommand.Execute(null);
        CurrentViewModel = _historyViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = "Settings";
        CurrentViewModel = _settingsViewModel;
    }
}
