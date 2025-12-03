using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using PageCounterPro.Core.PageCountProviders;
using PageCounterPro.Core.Services;
using PageCounterPro.Infrastructure.Interfaces;
using PageCounterPro.Infrastructure.Logging;
using PageCounterPro.Infrastructure.Services;
using PageCounterPro.UI.ViewModels;
using System.Windows;

namespace PageCounterPro.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new FileLoggerProvider());
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((context, services) =>
            {
                // Infrastructure services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IHistoryService, HistoryService>();
                services.AddSingleton<IExportService, ExportService>();

                // App settings (from settings service)
                services.AddSingleton(sp => sp.GetRequiredService<ISettingsService>().GetSettings());

                // Core services
                services.AddSingleton<IFileScanner, FileScanner>();
                services.AddSingleton<IPageCountService, PageCountService>();

                // Page count providers
                services.AddSingleton<IPageCountProvider, PdfPageCountProvider>();
                services.AddSingleton<IPageCountProvider, TextPageCountProvider>();
                services.AddSingleton<IPageCountProvider, ImagePageCountProvider>();
                services.AddSingleton<IPageCountProvider, VideoPageCountProvider>();
                services.AddSingleton<IPageCountProvider, SpreadsheetPageCountProvider>();
                services.AddSingleton<IPageCountProvider, WordPageCountProvider>();
                services.AddSingleton<IPageCountProvider, PowerPointPageCountProvider>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<ScanViewModel>();
                services.AddTransient<HistoryViewModel>();
                services.AddTransient<SettingsViewModel>();
            })
            .Build();

        Services = _host.Services;

        var logger = Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("PageCounter Pro starting up");

        // Set up global exception handling
        DispatcherUnhandledException += (sender, args) =>
        {
            logger.LogError(args.Exception, "Unhandled exception in UI thread");
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "PageCounter Pro - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                logger.LogCritical(ex, "Fatal unhandled exception");
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            logger.LogError(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("PageCounter Pro shutting down");

        _host?.Dispose();
        base.OnExit(e);
    }
}
