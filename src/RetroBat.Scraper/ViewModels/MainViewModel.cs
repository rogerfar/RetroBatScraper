using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RetroBat.Scraper.Views;
using Microsoft.EntityFrameworkCore;
using RetroBat.Scraper.Models;
using RetroBat.Scraper.Services;

namespace RetroBat.Scraper.ViewModels;

public partial class MainViewModel(IServiceProvider serviceProvider, ScreenScraperService screenScraperService, ApplicationDbContext dbContext, IDbContextFactory<ApplicationDbContext> dbContextFactory) : ObservableObject
{
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<PlatformViewModel> _platforms = [];

    [ObservableProperty]
    private ObservableCollection<ScrapeStatus> _scrapeStatuses = [];

    [ObservableProperty]
    private Boolean _isScraping;

    [ObservableProperty]
    private UserInfoViewModel? _userInfo;

    public async Task Initialize()
    {
        UserInfo = await screenScraperService.GetLoggedInUser();

        await ReloadPlatforms();
    }

    [RelayCommand]
    private async Task ToggleScraping()
    {
        if (IsScraping)
        {
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
            }

            IsScraping = false;
            _cancellationTokenSource = null;

            return;
        }

        _cancellationTokenSource = new();
        IsScraping = true;

        var failedScrapes = await dbContext.Games.Where(m => m.ScrapeStatus != GameScrapeStatus.Success).ToListAsync();

        foreach (var game in failedScrapes)
        {
            game.ScrapeStatus = GameScrapeStatus.NotScraped;
            game.ScrapeResult = null;
        }

        await dbContext.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            while (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && IsScraping)
            {
                foreach (var platform in Platforms)
                {
                    await using var localDbContext = await dbContextFactory.CreateDbContextAsync();

                    var platformGames = localDbContext.Games.AsNoTracking().Where(g => g.PlatformId == platform.PlatformId && g.IsSelected).ToList();

                    platform.CountTotalGames = platformGames.Count;
                    platform.CountSelectedGames = platformGames.Count(g => g.IsSelected);
                    platform.CountScrapePending = platformGames.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.NotScraped);
                    platform.CountScrapeSuccess = platformGames.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.Success);
                    platform.CountScrapeError = platformGames.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.Error);
                    platform.CountScrapeSkipped = platformGames.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.NotFound);
                }

                UserInfo = await screenScraperService.GetUserInfo();

                await Task.Delay(1000, CancellationToken.None);
            }
        });

        _ = Task.Run(async () =>
        {
            var progress = new Progress<List<ScrapeStatus>>(statuses =>
            {
                ScrapeStatuses = [.. statuses];
            });

            await screenScraperService.StartScraping(progress, _cancellationTokenSource.Token);

            IsScraping = false;
            _cancellationTokenSource = null;
        });
    }
    
    [RelayCommand]
    private async Task OpenSettings()
    {
        var settingsViewModel = serviceProvider.GetRequiredService<SettingsViewModel>();
        await settingsViewModel.LoadSettingsCommand.ExecuteAsync(null);

        var window = new SettingsWindow
        {
            DataContext = settingsViewModel
        };
    
        settingsViewModel.SetWindow(window);
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task AddNewPlatform()
    {
        var platformSettingsViewModelFactory = serviceProvider.GetRequiredService<PlatformSettingsViewModelFactory>();
        var platformVm = platformSettingsViewModelFactory.Create(null);

        var window = new PlatformSettingsWindow
        {
            DataContext = platformVm
        };

        platformVm.SetWindow(window);

        window.ShowDialog();
        
        await ReloadPlatforms();
    }

    [RelayCommand]
    private async Task PlatformSelected(PlatformViewModel platformViewModel)
    {
        var platformSettingsViewModelFactory = serviceProvider.GetRequiredService<PlatformSettingsViewModelFactory>();
        var platformVm = platformSettingsViewModelFactory.Create(platformViewModel.PlatformId);
        
        var window = new PlatformSettingsWindow
        {
            DataContext = platformVm
        };

        platformVm.SetWindow(window);

        window.ShowDialog();

        await ReloadPlatforms();
    }

    private async Task ReloadPlatforms()
    {
        var platforms = await dbContext.Platforms.OrderBy(p => p.Names[0]).Select(m => new PlatformViewModel(m)
        {
            CountTotalGames = m.Games!.Count,
            CountSelectedGames = m.Games!.Count(g => g.IsSelected),
            CountScrapePending = m.Games!.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.NotScraped),
            CountScrapeSuccess = m.Games!.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.Success),
            CountScrapeError = m.Games!.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.Error),
            CountScrapeSkipped = m.Games!.Count(g => g.IsSelected && g.ScrapeStatus == GameScrapeStatus.NotFound),
        }).ToListAsync();

        Platforms = [.. platforms.OrderBy(m=>m.Name)];
    }
}

