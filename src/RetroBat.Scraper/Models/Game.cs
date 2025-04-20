using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RetroBat.Scraper.Models;

public class Game: ObservableObject
{
    public Guid GameId { get; set; }

    public Int32? ScreenScraperId { get; set; }

    public Guid PlatformId { get; set; }

    public Platform? Platform { get; set; }

    public required String Name { get; set; }

    public required String FileNameWithExtension { get; set; }

    public required String FileNameWithoutExtension { get; set; }

    public String? ScreenScraperData { get; set; }

    public String? GameLinkData { get; set; }

    public GameScrapeStatus ScrapeStatus { get; set; }

    public String? ScrapeResult { get; set; }

    public Boolean IsSelected { get; set; }

    public static void Configure(EntityTypeBuilder<Game> entity)
    {
        entity.HasKey(m => m.GameId);

        entity.HasOne(m => m.Platform)
              .WithMany(m => m.Games)
              .HasForeignKey(m => m.PlatformId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}

public enum GameScrapeStatus
{
    NotScraped,
    InProgress,
    Success,
    Error,
    NotFound
}