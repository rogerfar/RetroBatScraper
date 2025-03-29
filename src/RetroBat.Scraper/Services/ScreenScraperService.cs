using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RetroBatScraper.Models;
using RetroBatScraper.ViewModels;

namespace RetroBatScraper.Services;

public class ScreenScraperService
{
    private readonly GameListXmlService _gameListXmlService;
    private readonly SettingsService _settingsService;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ScreenScraperFR.ScreenScraperFRClient _client;

    public List<ScreenScraperFR.Platform> Platforms { get; private set; } = [];

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private UserInfoViewModel? _userInfo;

    public ScreenScraperService(GameListXmlService gameListXmlService, SettingsService settingsService, IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _gameListXmlService = gameListXmlService;
        _settingsService = settingsService;
        _dbContextFactory = dbContextFactory;
        _client = GetClient();
    }
    
    public async Task<UserInfoViewModel?> GetLoggedInUser()
    {
        if (_userInfo == null)
        {
            await GetUserInfo();
        }
        
        return _userInfo;
    }

    public async Task<UserInfoViewModel?> GetUserInfo()
    {
        var userInfo = await _client.GetUserInfo();

        if (userInfo != null)
        {
            if (_userInfo == null)
            {
                _userInfo = new();
            }
            else
            {
                _userInfo.Username = userInfo.Username;
                _userInfo.UserId = userInfo.UserId;
                _userInfo.UserLevel = userInfo.UserLevel;
                _userInfo.ContributionLevel = userInfo.ContributionLevel;
                _userInfo.PlatformMediaUploads = userInfo.PlatformMediaUploads;
                _userInfo.InfoTextUploads = userInfo.InfoTextUploads;
                _userInfo.RomAssociations = userInfo.RomAssociations;
                _userInfo.GameMediaUploads = userInfo.GameMediaUploads;
                _userInfo.ApprovedProposals = userInfo.ApprovedProposals;
                _userInfo.RejectedProposals = userInfo.RejectedProposals;
                _userInfo.RejectionRate = userInfo.RejectionRate;
                _userInfo.MaxThreads = userInfo.MaxThreads;
                _userInfo.MaxDownloadSpeed = userInfo.MaxDownloadSpeed;
                _userInfo.RequestsToday = userInfo.RequestsToday;
                _userInfo.FailedRequestsToday = userInfo.FailedRequestsToday;
                _userInfo.MaxRequestsPerMinute = userInfo.MaxRequestsPerMinute;
                _userInfo.MaxRequestsPerDay = userInfo.MaxRequestsPerDay;
                _userInfo.MaxFailedRequestsPerDay = userInfo.MaxFailedRequestsPerDay;
                _userInfo.VisitCount = userInfo.VisitCount;
                _userInfo.LastVisitDate = userInfo.LastVisitDate;
                _userInfo.FavoriteRegion = userInfo.FavoriteRegion;
            }
        }

        return _userInfo;
    }

    public async Task Cache()
    {
        await GetLoggedInUser();

        if (File.Exists("platforms.json"))
        {
            var platformsJson = await File.ReadAllTextAsync("platforms.json");
            Platforms = JsonSerializer.Deserialize<List<ScreenScraperFR.Platform>>(platformsJson)!;
        }
        else
        {
            Platforms = await _client.GetPlatforms();

            var platformsJson = JsonSerializer.Serialize(Platforms);

            await File.WriteAllTextAsync("platforms.json", platformsJson);
        }
    }

    public async Task<ScreenScraperFR.Platform?> GetPlatform(Int32 platformId)
    {
        var platforms = await _client.GetPlatforms();

        return platforms.FirstOrDefault(m => m.Id == platformId);
    }

    public async Task StartScraping(IProgress<List<ScrapeStatus>> progress, CancellationToken cancellationToken)
    {
        var maxThreads = _userInfo!.MaxThreads;
        
        var tasks = new List<Task>();
        var statuses = Enumerable.Range(1, maxThreads)
                                 .Select(i => new ScrapeStatus { ThreadId = i })
                                 .ToList();

        var running = true;

        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested && running)
            {
                progress.Report(statuses);
                await Task.Delay(100, CancellationToken.None);
            }
        }, cancellationToken);

        for (var i = 0; i < _userInfo!.MaxThreads; i++)
        {
            var threadId = i + 1;
            var task = Task.Run(async () =>
            {
                var status = statuses.First(s => s.ThreadId == threadId);
                
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var doContinue = await ScrapeNextGame(status, cancellationToken);

                        if (!doContinue)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                finally
                {
                    status.IsActive = false;
                    status.CurrentGame = String.Empty;
                    status.Status = "Stopped";
                    progress.Report(statuses);
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        running = false;

        progress.Report(statuses);
    }

    private ScreenScraperFR.ScreenScraperFRClient GetClient()
    {
        var devId = _settingsService.GetSetting("ScreenScraperDevId") ?? String.Empty;
        var devPass = _settingsService.GetSetting("ScreenScraperDevPassword") ?? String.Empty;
        var userName = _settingsService.GetSetting("ScreenScraperUserName") ?? String.Empty;
        var userPass = _settingsService.GetSetting("ScreenScraperUserPassword") ?? String.Empty;

        return new("RetroBatScraper", devId, devPass, userName, userPass);
    }

    private async Task<Boolean> ScrapeNextGame(ScrapeStatus status, CancellationToken cancellationToken)
    {
        status.IsActive = true;
        status.CurrentGame = "";
        status.Status = "Fetching next game...";

        await Semaphore.WaitAsync(cancellationToken);

        var shouldScrapeGame = true;
        String gameListPath;
        Guid gameId;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var game = await dbContext.Games
                                      .Include(m => m.Platform)
                                      .OrderBy(m => m.Name)
                                      .FirstOrDefaultAsync(m => m.IsSelected && m.ScrapeStatus == GameScrapeStatus.NotScraped, cancellationToken);

            if (game == null)
            {
                status.IsActive = false;
                status.CurrentGame = "";
                status.Status = "Idle";

                return false;
            }

            gameId = game.GameId;

            game.ScrapeStatus = GameScrapeStatus.InProgress;

            await dbContext.SaveChangesAsync(cancellationToken);

            gameListPath = Path.Combine(game.Platform!.Path, "gamelist.xml");

            if (File.Exists(gameListPath))
            {
                var gameList = await _gameListXmlService.ReadGameListAsync(gameListPath);

                var gameListGame = gameList.Games.FirstOrDefault(m => m.Id == game.ScreenScraperId.ToString()
                                                                      || m.Path == $"./{game.FileNameWithExtension}");

                if (gameListGame != null)
                {
                    shouldScrapeGame = false;

                    if (game.ScreenScraperId == null)
                    {
                        game.ScreenScraperId = Int32.Parse(gameListGame.Id);

                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }
        finally
        {
            Semaphore.Release();
        }

        try
        {
            if (shouldScrapeGame)
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var game = await dbContext.Games.Include(m => m.Platform!).FirstAsync(m => m.GameId == gameId, cancellationToken);

                status.IsActive = true;
                status.CurrentGame = $"{game.Name} ({game.Platform!.Name})";
                status.Status = $"Searching for {game.Name}";

                var gameResult = await _client.GetGame(game.Platform!.ScreenScraperId!.Value, "rom", game.FileNameWithExtension, cancellationToken: cancellationToken);

                if (gameResult == null)
                {
                    var results = await _client.SearchGames(game.Name, game.Platform!.ScreenScraperId, cancellationToken);
                    gameResult = results.FirstOrDefault();
                }

                if (gameResult == null)
                {
                    var shortName = game.Name.Split(" ")[0];

                    status.Status = $"No match, searching for {shortName}";
                    
                    var results = await _client.SearchGames(shortName, game.Platform!.ScreenScraperId, cancellationToken);

                    ScreenScraperFR.Game? bestMatch = null;
                    Int32? bestMatchScore = null;

                    foreach (var result in results)
                    {
                        foreach (var resultName in result.Names)
                        {
                            var levenshteinDistance = Fastenshtein.Levenshtein.Distance(game.Name, resultName.Text);

                            if (bestMatch == null || levenshteinDistance < bestMatchScore)
                            {
                                bestMatch = result;
                                bestMatchScore = levenshteinDistance;
                            }
                        }
                    }

                    if (bestMatch != null)
                    {
                        gameResult = bestMatch;
                    }
                }

                if (gameResult == null)
                {
                    status.Status = "Not found";

                    game.ScrapeStatus = GameScrapeStatus.NotFound;
                    await dbContext.SaveChangesAsync(cancellationToken);

                    return true;
                }

                game.ScreenScraperId = gameResult.Id;
                game.Name = (gameResult.Names.FirstOrDefault(m => m.Region == "ss") ?? gameResult.Names.FirstOrDefault(m => m.Region == "us") ?? gameResult.Names.First()).Text;
                gameResult.Media = [];
                game.ScreenScraperData = JsonSerializer.Serialize(gameResult);

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await using (var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                var game = await dbContext.Games.Include(m => m.Platform!).FirstAsync(m => m.GameId == gameId, cancellationToken);

                var progress = new EventHandler<ScreenScraperFR.DownloadProgressEventArgs>((_, args) =>
                {
                    status.DownloadProgress = args.ProgressPercentage;
                    status.DownloadProgressPercentage = (Int32) (args.ProgressPercentage * 100.0);
                    status.DownloadBytesReceived = args.BytesReceived;
                    status.DownloadSpeedMbps = args.SpeedMbps;
                    status.DownloadTotalBytes = args.TotalBytes;
                });

                status.Reset("Downloading title image");
                await GetImage(_client, game, "title", "sstitle", progress);

                status.Reset("Downloading wheel image");
                await GetImage(_client, game, "marquee", "wheel", progress);

                status.Reset("Downloading box image");
                await GetImage(_client, game, "thumb", "box-2D", progress);

                status.Reset("Downloading video");
                await GetVideo(_client, game, "video", "video-normalized", progress);

                status.Reset("Finished");

                game.ScrapeStatus = GameScrapeStatus.Success;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await Semaphore.WaitAsync(cancellationToken);

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var game = await dbContext.Games.Include(m => m.Platform!).FirstAsync(m => m.GameId == gameId, cancellationToken);

                status.Status = "Adding to gamelist.xml";

                if (game.ScreenScraperData != null)
                {
                    var data = JsonSerializer.Deserialize<ScreenScraperFR.Game>(game.ScreenScraperData!)!;

                    var imageFileName = $"./images/{game.FileNameWithoutExtension}-title.png";
                    var videoFileName = $"./videos/{game.FileNameWithoutExtension}-video.mp4";
                    var marqueeFileName = $"./images/{game.FileNameWithoutExtension}-marquee.png";
                    var thumbFileName = $"./images/{game.FileNameWithoutExtension}-thumb.png";

                    if (!File.Exists(Path.Combine(game.Platform!.Path, imageFileName)))
                    {
                        imageFileName = null;
                    }

                    if (!File.Exists(Path.Combine(game.Platform!.Path, videoFileName)))
                    {
                        videoFileName = null;
                    }

                    if (!File.Exists(Path.Combine(game.Platform!.Path, marqueeFileName)))
                    {
                        marqueeFileName = null;
                    }

                    if (!File.Exists(Path.Combine(game.Platform!.Path, thumbFileName)))
                    {
                        thumbFileName = null;
                    }

                    Decimal? rating = data.Rating?.Text != null ? (Decimal.Parse(data.Rating.Text) / 100) : null;

                    var romFilePath = Path.Combine(game.Platform!.Path, game.FileNameWithExtension);

                    var xmlGame = new XmlGame
                    {
                        Id = game.ScreenScraperId!.Value.ToString(),
                        Path = $"./{game.FileNameWithExtension}",
                        Name = game.Name,
                        Description = data.Synopsis.FirstOrDefault(m => m.Language == "en")?.Text,
                        Image = imageFileName,
                        Video = videoFileName,
                        Marquee = marqueeFileName,
                        Thumbnail = thumbFileName,
                        Rating = rating,
                        ReleaseDate = (data.ReleaseDates.FirstOrDefault(m => m.Region == "us") ?? data.ReleaseDates.FirstOrDefault())?.Text,
                        Developer = data.Developer?.Name,
                        Publisher = data.Publisher?.Name,
                        Genre = data.Genres.FirstOrDefault(m => m.IsPrimary)?.Names.FirstOrDefault(m => m.Language == "en")?.Text,
                        Family = data.Series.FirstOrDefault()?.Names.FirstOrDefault(m => m.Language == "en")?.Text,
                        Players = data.PlayerCount?.Text,
                        Md5 = await CreateMd5(romFilePath),
                        Language = data.Rom?.Languages?.En.FirstOrDefault(),
                        Scrap = new()
                        {
                            Name = "ScreenScraper",
                            Date = $"{DateTime.Now:yyyyMMdd}T{DateTime.Now:HHmmss}"
                        }
                    };

                    if (!File.Exists(gameListPath))
                    {
                        await _gameListXmlService.WriteGameListAsync(gameListPath,
                                                                    new()
                                                                    {
                                                                        Games = [xmlGame]
                                                                    });
                    }
                    else
                    {
                        var gameList = await _gameListXmlService.ReadGameListAsync(gameListPath);

                        gameList.Games.Add(xmlGame);
                        gameList.Games = [.. gameList.Games.OrderBy(m => m.Path)];

                        await _gameListXmlService.WriteGameListAsync(gameListPath, gameList);
                    }
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            status.Status = "Error!";

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var game = await dbContext.Games.FirstAsync(m => m.GameId == gameId, cancellationToken);
            game.ScrapeResult = ex.Message;
            game.ScrapeStatus = GameScrapeStatus.Error;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static async Task GetImage(ScreenScraperFR.ScreenScraperFRClient client, Game game, String fileName, String type, EventHandler<ScreenScraperFR.DownloadProgressEventArgs> progressEvent)
    {
        var regionOrder = new List<String>
        {
            "wor",
            "us",
            "eu",
            "ss"
        };

        var outputPath = Path.Combine(game.Platform!.Path, "images");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        fileName = $"{game.FileNameWithoutExtension}-{fileName}.png";

        outputPath = Path.Combine(outputPath, fileName);

        foreach (var region in regionOrder)
        {
            if (File.Exists(outputPath))
            {
                return;
            }

            await client.GetGameImage(game.Platform!.ScreenScraperId!.Value, game.ScreenScraperId!.Value, $"{type}({region})", outputPath, progressEvent: progressEvent);
        }
    }

    private static async Task GetVideo(ScreenScraperFR.ScreenScraperFRClient client, Game game, String fileName, String type, EventHandler<ScreenScraperFR.DownloadProgressEventArgs> progressEvent)
    {
        var outputPath = Path.Combine(game.Platform!.Path, "videos");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        fileName = $"{game.FileNameWithoutExtension}-{fileName}.mp4";

        outputPath = Path.Combine(outputPath, fileName);

        if (File.Exists(outputPath))
        {
            return;
        }

        await client.GetGameVideo(game.Platform!.ScreenScraperId!.Value, game.ScreenScraperId!.Value, type, outputPath, progressEvent: progressEvent);
    }

    private static async Task<String> CreateMd5(String fileName)
    {
        var input = await File.ReadAllBytesAsync(fileName);

        var byteHash = MD5.HashData(input);
        var hash = Convert.ToHexString(byteHash);

        return hash.ToLower();
    }
}