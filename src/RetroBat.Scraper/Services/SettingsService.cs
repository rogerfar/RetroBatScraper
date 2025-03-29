using Microsoft.EntityFrameworkCore;
using RetroBatScraper.Models;

namespace RetroBatScraper.Services;

public class SettingsService(ApplicationDbContext context) 
{
    public async Task<List<Setting>> GetAllSettingsAsync()
    {
        return await context.Settings.ToListAsync();
    }

    public String? GetSetting(String key)
    {
        var setting = context.Settings.FirstOrDefault(m => m.Key == key);
        return setting?.Value;
    }

    public async Task SaveSettingAsync(Setting setting)
    {
        var existingSetting = await context.Settings.FirstOrDefaultAsync(s => s.Key == setting.Key);

        if (existingSetting != null)
        {
            existingSetting.Value = setting.Value;
            context.Settings.Update(existingSetting);
        }
        await context.SaveChangesAsync();
    }
}