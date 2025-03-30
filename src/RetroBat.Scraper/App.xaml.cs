using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RetroBatScraper.Services;
using RetroBatScraper.ViewModels;
using RetroBatScraper.Views;
using Serilog;

namespace RetroBatScraper;

public partial class App
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=app.db"));

        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=app.db");
        });

        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.Debug()
                     .WriteTo.File("logs.txt", rollOnFileSizeLimit: true)
                     .CreateLogger();

        services.AddSingleton<FileDownloaderService>();
        services.AddSingleton<GameListXmlService>();
        services.AddSingleton<ScreenScraperService>();
        services.AddSingleton<SettingsService>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<PlatformSettingsViewModelFactory>();
        services.AddSingleton<SettingsViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();
            }

            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();
            
            try
            {
                var screenScraperService = _serviceProvider.GetRequiredService<ScreenScraperService>();
                await screenScraperService.Cache();

                var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                await mainViewModel.Initialize();

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                mainWindow.Show();
            }
            finally
            {
                loadingWindow.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _serviceProvider.Dispose();
    }
}
