using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RetroBatScraper.Models;
using RetroBatScraper.Services;

namespace RetroBatScraper.ViewModels;

public partial class PlatformSettingsViewModel : ObservableObject
{
    private readonly WeakReference<ICloseWindow> _windowReference = new(null!);

    private readonly Platform _platform;
    private readonly Boolean _newPlatform;
    private readonly ApplicationDbContext _dbContext;
    private readonly ScreenScraperService _screenScraperService;
    private readonly FileDownloaderService _fileDownloaderService;

    public ObservableCollection<Platform> AvailablePlatforms { get; }
    public ObservableCollection<GameViewModel> Games { get; }
    public ObservableCollection<FilterButton> FilterButtons { get; }
    
    [ObservableProperty]
    private String _path;

    [ObservableProperty]
    private Platform? _selectedPlatform;

    [ObservableProperty]
    private String _extensions;

    [ObservableProperty]
    private String _url;

    [ObservableProperty]
    private Boolean _isSaving;

    [ObservableProperty]
    private String _statusText = "";

    public PlatformSettingsViewModel(Guid? platformId, ApplicationDbContext dbContext, ScreenScraperService screenScraperService, FileDownloaderService fileDownloaderService)
    {
        if (platformId == null)
        {
            _platform = new()
            {
                PlatformId = Guid.NewGuid(),
                Name = "New Platform",
                Extensions = "",
                MediaType = "",
                Path = "",
                RomType = "",
                Names = []
            };

            _newPlatform = true;
        }
        else
        {
            _platform = dbContext.Platforms.First(m => m.PlatformId == platformId);

            _newPlatform = false;
        }

        _dbContext = dbContext;
        _screenScraperService = screenScraperService;
        _fileDownloaderService = fileDownloaderService;

        AvailablePlatforms = [.. screenScraperService.Platforms.Select(m => new Platform
        {
            ScreenScraperId = m.Id,
            Name = m.Names.Us ?? m.Names.Eu,
            Names = [],
            Extensions = m.Extensions ?? "",
            MediaType = m.MediaType,
            Path = "",
            RomType = m.RomType,
            Company = m.Company,
            Type = m.Type
        }).OrderBy(m => m.Name)];

        _path = _platform.Path;
        _selectedPlatform = AvailablePlatforms.FirstOrDefault(m => m.ScreenScraperId == _platform.ScreenScraperId);
        _extensions = _platform.Extensions;
        _url = _platform.Url ?? "";

        var games = dbContext.Games.Where(m => m.PlatformId == _platform.PlatformId).ToList();
        Games = [.. games.Select(game => new GameViewModel(game)).ToList()];

        foreach (var game in Games)
        {
            game.IsSelected = game.InitialSelected;
        }
        
        FilterButtons = [];

        var regions = Games.SelectMany(g => g.GameLink?.Regions ?? []).OrderBy(m => m).Distinct().ToList();
        foreach (var region in regions)
        {
            FilterButtons.Add(new()
            { 
                Text = region,
                State = FilterState.Undefined,
                Type = FilterType.Region
            });
        }

        var languages = Games.SelectMany(g => g.GameLink?.Languages ?? []).OrderBy(m => m).Distinct().ToList();
        foreach (var language in languages)
        {
            FilterButtons.Add(new()
            {
                Text = language,
                State = FilterState.Undefined,
                Type = FilterType.Language
            });
        }

        FilterButtons.Add(new()
        {
            Text = "Has Tags",
            State = FilterState.Undefined,
            Type = FilterType.AllTags
        });

        var tags = Games.SelectMany(g => g.GameLink?.Tags ?? []).OrderBy(m => m).Distinct().ToList();
        foreach (var tag in tags)
        {
            FilterButtons.Add(new()
            {
                Text = tag,
                State = FilterState.Undefined,
                Type = FilterType.Tag
            });
        }

        UpdateStatusText();
    }

    public void SetWindow(ICloseWindow window)
    {
        _windowReference.SetTarget(window);
    }

    [RelayCommand]
    private async Task FetchMetaData()
    {
        if (SelectedPlatform?.ScreenScraperId == null)
        {
            return;
        }

        var screenScraperPlatform = await _screenScraperService.GetPlatform(SelectedPlatform.ScreenScraperId.Value);

        if (screenScraperPlatform == null)
        {
            return;
        }

        Extensions = screenScraperPlatform.Extensions ?? "";

        _platform.ScreenScraperId = screenScraperPlatform.Id;
        _platform.Name = screenScraperPlatform.Names.Us ?? screenScraperPlatform.Names.Eu;
        _platform.Company = screenScraperPlatform.Company;
        _platform.Type = screenScraperPlatform.Type;
        _platform.RomType = screenScraperPlatform.RomType;
        _platform.MediaType = screenScraperPlatform.MediaType;
        _platform.Names = screenScraperPlatform.GetAllNames();
    }

    [RelayCommand]
    private async Task Update()
    {
        var gameLinks = await _fileDownloaderService.GetGames(Url);

        await _dbContext.Games.Where(m => m.PlatformId == _platform.PlatformId).ExecuteDeleteAsync();

        foreach (var gameLink in gameLinks)
        {
            var game = await _dbContext.Games.FirstOrDefaultAsync(m => m.PlatformId == _platform.PlatformId && m.FileNameWithExtension == gameLink.FileName);

            if (game != null)
            {
                game.GameLinkData = JsonSerializer.Serialize(gameLink);
            }
            else
            {
                game = new()
                {
                    GameId = Guid.NewGuid(),
                    ScreenScraperId = null,
                    PlatformId = _platform.PlatformId,
                    Name = gameLink.Name,
                    FileNameWithExtension = gameLink.FileName,
                    FileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(gameLink.FileName),
                    ScreenScraperData = null,
                    GameLinkData = JsonSerializer.Serialize(gameLink),
                    ScrapeStatus = GameScrapeStatus.NotScraped,
                    IsSelected = false
                };

                await _dbContext.Games.AddAsync(game);
            }
            
            await _dbContext.SaveChangesAsync();
        }

        var games = _dbContext.Games.Where(m => m.PlatformId == _platform.PlatformId).ToList();
        Games.Clear();
        
        foreach (var game in games)
        {
            Games.Add(new(game));
        }

        UpdateStatusText();
    }

    [RelayCommand]
    private async Task CreateFakeGames()
    {
        foreach (var game in Games)
        {
            if (game.IsSelected && game.Url != null)
            {
                await _fileDownloaderService.DownloadFakeGames(game.FileNameWithoutExtension, game.Url, Path);
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        IsSaving = true;

        try
        {
            Platform platform;

            if (SelectedPlatform == null)
            {
                return;
            }

            if (_newPlatform)
            {
                platform = new()
                {
                    PlatformId = Guid.NewGuid(),
                    ScreenScraperId = SelectedPlatform.ScreenScraperId,
                    Path = Path,
                    Name = SelectedPlatform.Name,
                    Extensions = Extensions,
                    Company = _platform.Company,
                    Type = _platform.Type,
                    RomType = _platform.RomType,
                    MediaType = _platform.MediaType,
                    Names = _platform.Names,
                    Url = Url
                };

                await _dbContext.Platforms.AddAsync(platform);
            }
            else
            {
                platform = await _dbContext.Platforms.FirstAsync(m => m.PlatformId == _platform.PlatformId);

                platform.ScreenScraperId = SelectedPlatform.ScreenScraperId;
                platform.Path = Path;
                platform.Name = SelectedPlatform.Name;
                platform.Extensions = Extensions;
                platform.Company = _platform.Company;
                platform.Type = _platform.Type;
                platform.RomType = _platform.RomType;
                platform.MediaType = _platform.MediaType;
                platform.Names = _platform.Names;
                platform.Url = Url;
            }

            await _dbContext.SaveChangesAsync();

            foreach (var game in Games)
            {
                var dbGame = await _dbContext.Games.FirstAsync(m => m.GameId == game.GameId);
                dbGame.IsSelected = game.IsSelected;
            }

            await _dbContext.SaveChangesAsync();

            if (_windowReference.TryGetTarget(out var window))
            {
                window.Close();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void CheckAll()
    {
        foreach (var game in Games)
        {
            game.IsSelected = true;
        }

        UpdateStatusText();
    }
    
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var game in Games)
        {
            game.IsSelected = false;
        }

        UpdateStatusText();
    }

    [RelayCommand]
    private void SyncWithFolder()
    {
        var files = Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories).Select(System.IO.Path.GetFileNameWithoutExtension).ToList();

        foreach (var game in Games)
        {
            game.IsSelected = files.Contains(game.FileNameWithoutExtension);
        }

        UpdateStatusText();
    }

    [RelayCommand]
    private void Unique()
    {
        var query = Games.AsEnumerable();

        var regionFilters = FilterButtons.Where(f => f.Type == FilterType.Region).ToList();
        var activeRegions = regionFilters.Where(f => f.State == FilterState.Active).Select(f => f.Text).ToList();

        var languageFilters = FilterButtons.Where(f => f.Type == FilterType.Language).ToList();
        var activeLanguages = languageFilters.Where(f => f.State == FilterState.Active).Select(f => f.Text).ToList();

        // Group by name and select the best match based on priorities
        var matchingGames = query
            .Where(m => m.IsSelected)
            .GroupBy(g => g.Name)
            .Select(group =>
            {
                return group
                    .OrderByDescending(g =>
                    {
                        var score = 0;

                        // Check if game has any matching regions
                        var hasMatchingRegions = g.GameLink?.Regions.Any(r => activeRegions.Contains(r)) ?? false;
                        // Check if game has any matching languages
                        var hasMatchingLanguages = g.GameLink?.Languages.Any(l => activeLanguages.Contains(l)) ?? false;

                        if (hasMatchingRegions && hasMatchingLanguages)
                        {
                            // Highest priority - matches both region and language
                            score = 3;
                        }
                        else if (hasMatchingLanguages)
                        {
                            // Medium priority - matches language only
                            score = 2;
                        }
                        else if (hasMatchingRegions)
                        {
                            // Lower priority - matches region only
                            score = 1;
                        }

                        return score;
                    })
                    .First(); // Take the highest scoring game from each group
            })
            .ToList();

        foreach (var game in Games)
        {
            game.IsSelected = matchingGames.Contains(game);
        }

        UpdateStatusText();
    }

    [RelayCommand]
    private void ToggleFilter(FilterButton filter)
    {
        // Cycle through states: Undefined -> Active -> Inactive -> Undefined
        filter.State = filter.State switch
        {
            FilterState.Undefined => FilterState.Active,
            FilterState.Active => FilterState.Inactive,
            FilterState.Inactive => FilterState.Undefined,
            _ => FilterState.Undefined
        };

        var query = Games.AsEnumerable();
        
        // Handle Region filters
        var regionFilters = FilterButtons.Where(f => f.Type == FilterType.Region).ToList();
        var activeRegions = regionFilters.Where(f => f.State == FilterState.Active).Select(f => f.Text).ToList();
        var inactiveRegions = regionFilters.Where(f => f.State == FilterState.Inactive).Select(f => f.Text).ToList();

        if (activeRegions.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Regions.Any(r => activeRegions.Contains(r)) == true);
        }
        if (inactiveRegions.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Regions.Any(r => inactiveRegions.Contains(r)) != true);
        }

        // Handle Language filters
        var languageFilters = FilterButtons.Where(f => f.Type == FilterType.Language).ToList();
        var activeLanguages = languageFilters.Where(f => f.State == FilterState.Active).Select(f => f.Text).ToList();
        var inactiveLanguages = languageFilters.Where(f => f.State == FilterState.Inactive).Select(f => f.Text).ToList();

        if (activeLanguages.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Languages.Any(r => activeLanguages.Contains(r)) == true);
        }
        if (inactiveLanguages.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Languages.Any(r => inactiveLanguages.Contains(r)) != true);
        }

        // Handle AllTags filters
        var allTagsFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.AllTags);
        if (allTagsFilter != null)
        {
            if (allTagsFilter.State == FilterState.Active)
            {
                query = query.Where(g => g.GameLink?.Tags.Count != 0 == true);
            }
            else if (allTagsFilter.State == FilterState.Inactive)
            {
                query = query.Where(g => g.GameLink?.Tags.Count != 0 != true);
            }
        }

        // Handle Tag filters
        var tagFilters = FilterButtons.Where(f => f.Type == FilterType.Tag).ToList();
        var activeTags = tagFilters.Where(f => f.State == FilterState.Active).Select(f => f.Text).ToList();
        var inactiveTags = tagFilters.Where(f => f.State == FilterState.Inactive).Select(f => f.Text).ToList();

        if (activeTags.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Tags.Any(r => activeTags.Contains(r)) == true);
        }
        if (inactiveTags.Count != 0)
        {
            query = query.Where(g => g.GameLink?.Tags.Any(r => inactiveTags.Contains(r)) != true);
        }

        // Apply selection to matching games
        var matchingGames = query.ToList();
        foreach (var game in Games)
        {
            game.IsSelected = matchingGames.Contains(game);
        }

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        var selectedCount = Games.Count(g => g.IsSelected);
        var totalCount = Games.Count;
        StatusText = $"Selected games: {selectedCount} / {totalCount}";
    }
}

public class PlatformSettingsViewModelFactory(ApplicationDbContext dbContext, ScreenScraperService screenScraperService, FileDownloaderService fileDownloaderService)
{
    public PlatformSettingsViewModel Create(Guid? platformId)
    {
        return new(platformId, dbContext, screenScraperService, fileDownloaderService);
    }
}