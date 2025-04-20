using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RetroBat.Scraper.Models;

public class Platform
{
    public Guid PlatformId { get; set; }

    public Int32? ScreenScraperId { get; set; }

    public required String Path { get; set; }

    public required String Name { get; set; }

    public required String Extensions { get; set; }

    public String? Company { get; set; }

    public String? Type { get; set; }

    public required String RomType { get; set; }

    public required String MediaType { get; set; }

    public required List<String> Names { get; set; }

    public String? Url { get; set; }

    public List<Game>? Games { get; set; }

    public static void Configure(EntityTypeBuilder<Platform> entity)
    {
        entity.HasKey(m => m.PlatformId);
    }
}
