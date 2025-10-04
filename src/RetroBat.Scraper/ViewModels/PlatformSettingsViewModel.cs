using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using Microsoft.EntityFrameworkCore;
using RetroBat.Scraper.Models;
using RetroBat.Scraper.Services;
using SharpCompress.Archives;

namespace RetroBat.Scraper.ViewModels;

public partial class PlatformSettingsViewModel : ObservableObject
{
    private readonly WeakReference<ICloseWindow> _windowReference = new(null!);

    private readonly Platform _platform;
    private readonly Boolean _newPlatform;
    private readonly ApplicationDbContext _dbContext;
    private readonly ScreenScraperService _screenScraperService;
    private readonly FileDownloaderService _fileDownloaderService;

    public ObservableCollection<Platform> AvailablePlatforms { get; }
    public ObservableCollection<GameViewModel> Games { get; } = [];
    public ObservableCollection<FilterButton> FilterButtons { get; } = [];
    
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
                Extension = "",
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
            Extension = m.Extensions ?? "",
            MediaType = m.MediaType,
            Path = "",
            RomType = m.RomType,
            Company = m.Company,
            Type = m.Type
        }).OrderBy(m => m.Name)];

        _path = _platform.Path;
        _selectedPlatform = AvailablePlatforms.FirstOrDefault(m => m.ScreenScraperId == _platform.ScreenScraperId);
        _extensions = _platform.Extension;
        _url = _platform.Url ?? "";

        UpdateFilterBar();
    }

    public void SetWindow(ICloseWindow window)
    {
        _windowReference.SetTarget(window);
    }

    [RelayCommand]
    private async Task FetchMetaData()
    {
        StatusText = "Fetching metadata...";

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
        _platform.Extension = (screenScraperPlatform.Extensions ?? "").Split(',').FirstOrDefault() ?? "";

        UpdateStatusText();
    }

    [RelayCommand]
    private async Task DownloadGameList()
    {
        StatusText = "Downloading game list...";

        var gameLinks = await _fileDownloaderService.GetGames(Url);

        StatusText = $"Processing {gameLinks.Count} games...";

        foreach (var gameLink in gameLinks)
        {
            StatusText = $"Processing {gameLink.FileName}...";

            var game = await _dbContext.Games.FirstOrDefaultAsync(m => m.PlatformId == _platform.PlatformId && m.FileName == gameLink.FileName);

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
                    FileName = System.IO.Path.GetFileNameWithoutExtension(gameLink.FileName),
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
            StatusText = $"Adding {game.FileName}...";

            Games.Add(new(game));
        }

        UpdateFilterBar();
    }

    [RelayCommand]
    private async Task DetermineExtension()
    {
        var selectedGame = Games.FirstOrDefault(g => g.IsSelected && g.Url != null);

        if (selectedGame == null)
        {
            MessageBox.Show("Please select a game to determine the extension.");
            return;
        }

        IsSaving = true;

        var tempPath = System.IO.Path.Combine(_platform.Path, "tmp");

        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }

        Directory.CreateDirectory(tempPath);

        var fileName = selectedGame.FileName;

        try
        {
            Extensions = "";

            StatusText = $"Downloading {fileName}...";

            // Download
            var destinationPath = System.IO.Path.Combine(tempPath, fileName);

            var downloader = new DownloadService();

            downloader.DownloadProgressChanged += (_, e) =>
            {
                StatusText = $"Downloading {fileName}... {(Int32) e.ProgressPercentage}%";
            };

            await downloader.DownloadFileTaskAsync(selectedGame.Url, destinationPath);

            StatusText = $"Extracting {fileName}...";

            // Unpack
            using var archive = ArchiveFactory.Open(destinationPath);

            var totalSize = archive.TotalSize;

            archive.CompressedBytesRead += (_, args) =>
            {
                var perc = ((Double)args.CompressedBytesRead / totalSize) * 100.0;
                StatusText = $"Extracting {fileName}... {(Int32) perc}%";
            };

            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                entry.WriteToDirectory(tempPath);
            }

            // Find first file that is not zip
            var extractedFiles = Directory.GetFiles(tempPath);
            var firstFile = extractedFiles.FirstOrDefault(file => !file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (firstFile != null)
            {
                var extension = System.IO.Path.GetExtension(firstFile);
                Extensions = extension.Trim('.');
            }
            else
            {
                MessageBox.Show("No files found in the archive.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            IsSaving = false;

            UpdateStatusText();
        }
    }

    [RelayCommand]
    private async Task CreateFakeGames()
    {
        if (String.IsNullOrWhiteSpace(Extensions))
        {
            MessageBox.Show("Please provide an extension");

            return;
        }

        var extensions = Extensions.Split(',');

        if (extensions.Length > 1)
        {
            MessageBox.Show("Multiple extensions are not supported. Please provide a single extension which matches the file extension of the unzipped file.");

            return;
        }

        var extension = extensions.First();

        IsSaving = true;

        StatusText = "Creating fake games...";

        foreach (var game in Games)
        {
            if (game.IsSelected && game.Url != null)
            {
                StatusText = $"Creating fake game for {game.FileName}.{extension}...";

                await _fileDownloaderService.DownloadFakeGames(game.FileName, game.Url, Path, extension);
            }
        }

        IsSaving = false;
    }

    [RelayCommand]
    private async Task ResetScrape()
    {
        var allGames = await _dbContext.Games.Where(m => m.PlatformId == _platform.PlatformId).ToListAsync();

        foreach (var game in allGames)
        {
            game.ScrapeStatus = GameScrapeStatus.NotScraped;
            game.ScreenScraperData = null;
            game.ScrapeResult = null;
            game.ScreenScraperId = null;
        }

        await _dbContext.SaveChangesAsync();
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
                    Extension = Extensions,
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
                platform.Extension = Extensions;
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
            game.IsSelected = files.Contains(game.FileName);
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
    private void RemoveFailed()
    {
        var query = Games.AsEnumerable();

        foreach (var game in query)
        {
            if (game.Game.ScrapeStatus is GameScrapeStatus.Error or GameScrapeStatus.NotFound)
            {
                game.IsSelected = false;
            }
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
        
        // Handle Aftermarket filters
        var aftermarketFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Aftermarket);
        if (aftermarketFilter != null)
        {
            query = aftermarketFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsAftermarket == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsAftermarket != true),
                _ => query
            };
        }

        // Handle Beta filters
        var betaFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Beta);
        if (betaFilter != null)
        {
            query = betaFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsBeta == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsBeta != true),
                _ => query
            };
        }

        // Handle Demo filters
        var demoFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Demo);
        if (demoFilter != null)
        {
            query = demoFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsDemo == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsDemo != true),
                _ => query
            };
        }

        // Handle Kiosk filters
        var kioskFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Kiosk);
        if (kioskFilter != null)
        {
            query = kioskFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsKiosk == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsKiosk != true),
                _ => query
            };
        }

        // Handle Prototype filters
        var prototypeFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Prototype);
        if (prototypeFilter != null)
        {
            query = prototypeFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsPrototype == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsPrototype != true),
                _ => query
            };
        }

        // Handle Test filters
        var testFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Test);
        if (testFilter != null)
        {
            query = testFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsTestProgram == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsTestProgram != true),
                _ => query
            };
        }

        // Handle Unlicensed filters
        var unlicensedFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.Unlicensed);
        if (unlicensedFilter != null)
        {
            query = unlicensedFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.IsUnlicensed == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.IsUnlicensed != true),
                _ => query
            };
        }

        // Handle AllTags filters
        var allTagsFilter = FilterButtons.FirstOrDefault(f => f.Type == FilterType.AllTags);
        if (allTagsFilter != null)
        {
            query = allTagsFilter.State switch
            {
                FilterState.Active => query.Where(g => g.GameLink?.Tags.Count != 0 == true),
                FilterState.Inactive => query.Where(g => g.GameLink?.Tags.Count != 0 != true),
                _ => query
            };
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

    private void UpdateFilterBar()
    {
        Games.Clear();

        var games = _dbContext.Games.Where(m => m.PlatformId == _platform.PlatformId).ToList();

        foreach (var game in games.Select(game => new GameViewModel(game)).ToList())
        {
            game.IsSelected = game.InitialSelected;
            Games.Add(game);
        }

        FilterButtons.Clear();

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
            Text = "Aftermarket",
            State = FilterState.Undefined,
            Type = FilterType.Aftermarket
        });

        FilterButtons.Add(new()
        {
            Text = "Beta",
            State = FilterState.Undefined,
            Type = FilterType.Beta
        });

        FilterButtons.Add(new()
        {
            Text = "Demo",
            State = FilterState.Undefined,
            Type = FilterType.Demo
        });

        FilterButtons.Add(new()
        {
            Text = "Kiosk",
            State = FilterState.Undefined,
            Type = FilterType.Kiosk
        });
        
        FilterButtons.Add(new()
        {
            Text = "Prototype",
            State = FilterState.Undefined,
            Type = FilterType.Prototype
        });

        FilterButtons.Add(new()
        {
            Text = "Test",
            State = FilterState.Undefined,
            Type = FilterType.Test
        });

        FilterButtons.Add(new()
        {
            Text = "Unlicensed",
            State = FilterState.Undefined,
            Type = FilterType.Unlicensed
        });

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
}

public class PlatformSettingsViewModelFactory(ApplicationDbContext dbContext, ScreenScraperService screenScraperService, FileDownloaderService fileDownloaderService)
{
    public PlatformSettingsViewModel Create(Guid? platformId)
    {
        return new(platformId, dbContext, screenScraperService, fileDownloaderService);
    }
}