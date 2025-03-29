using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RetroBatScraper.Models;

public class Setting
{
    public Guid SettingId { get; set; }

    public required String Key { get; set; }

    public required String Type { get; set; }

    public required String Value { get; set; }

    public static void Configure(EntityTypeBuilder<Setting> entity)
    {
        entity.HasKey(m => m.SettingId);

        entity.Property(m => m.Key).IsRequired();

        entity.Property(m => m.Type).IsRequired();

        entity.Property(m => m.Value).IsRequired();

        entity.HasIndex(m => m.Key).IsUnique();

        entity.HasData(new Setting
                       {
                           SettingId = Guid.NewGuid(),
                           Key = "RetroBatPath",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           SettingId = Guid.NewGuid(),
                           Key = "ScreenScraperDevId",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           SettingId = Guid.NewGuid(),
                           Key = "ScreenScraperDevPassword",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           SettingId = Guid.NewGuid(),
                           Key = "ScreenScraperUserName",
                           Type = "String",
                           Value = ""
                       },
                       new Setting
                       {
                           SettingId = Guid.NewGuid(),
                           Key = "ScreenScraperUserPassword",
                           Type = "String",
                           Value = ""
                       });
    }
}