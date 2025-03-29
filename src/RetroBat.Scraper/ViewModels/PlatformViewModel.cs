using CommunityToolkit.Mvvm.ComponentModel;
using RetroBatScraper.Models;

namespace RetroBatScraper.ViewModels;

public partial class PlatformViewModel(Platform platform) : ObservableObject
{
    [ObservableProperty]
    private Int32 _countTotalGames;

    [ObservableProperty]
    private Int32 _countSelectedGames;

    [ObservableProperty]
    private Int32 _countScrapePending;

    [ObservableProperty]
    private Int32 _countScrapeSuccess;

    [ObservableProperty]
    private Int32 _countScrapeError;

    [ObservableProperty]
    private Int32 _countScrapeSkipped;

    public Guid PlatformId { get; } = platform.PlatformId;
    public Int32? ScreenScraperId { get; } = platform.ScreenScraperId;
    public String Path { get; } = platform.Path;
    public String Name { get; } = platform.Name;
    public String Extensions { get; } = platform.Extensions;
    public String? Company { get; } = platform.Company;
    public String? Type { get; } = platform.Type;
    public String RomType { get; } = platform.RomType;
    public String MediaType { get; } = platform.MediaType;
    public String? Url { get; } = platform.Url;

    public List<String> Names { get; } = [];
    public List<Game> Games { get; } = platform.Games ?? [];
}