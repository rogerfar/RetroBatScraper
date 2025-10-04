using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RetroBat.Scraper.Models;

public class Setting
{
    public required String Key { get; set; }

    public required String Type { get; set; }

    public required String Value { get; set; }

    public static void Configure(EntityTypeBuilder<Setting> entity)
    {
        entity.HasKey(m => m.Key);

        entity.Property(m => m.Type).IsRequired();

        entity.Property(m => m.Value).IsRequired();

        entity.HasIndex(m => m.Key).IsUnique();

        entity.HasData(new Setting
                       {
                           Key = "RetroBatPath",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           Key = "ScreenScraperDevId",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           Key = "ScreenScraperDevPassword",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           Key = "ScreenScraperUserName",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           Key = "ScreenScraperUserPassword",
                           Type = "String",
                           Value = ""
                       });
    }
}