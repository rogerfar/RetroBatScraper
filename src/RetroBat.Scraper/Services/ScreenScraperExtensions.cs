using ScreenScraperFR;

namespace RetroBatScraper.Services;

public static class ScreenScraperExtensions
{
    public static List<String> GetAllNames(this Platform platform)
    {
        var allNames = new List<String>(platform.Names.Eu.Split(','));

        if (platform.Names.Names != null)
        {
            allNames.AddRange(platform.Names.Names.Split(','));
        }

        if (platform.Names.Hyperspin != null)
        {
            allNames.AddRange(platform.Names.Hyperspin.Split(','));
        }

        if (platform.Names.Jp != null)
        {
            allNames.AddRange(platform.Names.Jp.Split(','));
        }
            
        if (platform.Names.Launchbox != null)
        {
            allNames.AddRange(platform.Names.Launchbox.Split(','));
        }
            
        if (platform.Names.Recalbox != null)
        {
            allNames.AddRange(platform.Names.Recalbox.Split(','));
        }
            
        if (platform.Names.Retropie != null)
        {
            allNames.AddRange(platform.Names.Retropie.Split(','));
        }
            
        if (platform.Names.Us != null)
        {
            allNames.AddRange(platform.Names.Us.Split(','));
        }

        var allNamesWithSpace = allNames.Where(m => m.Contains(' ')).ToList();

        allNames.AddRange(allNamesWithSpace.Select(name => name.Replace(" ", "")));

        allNames = [.. allNames.Distinct().OrderBy(m => m)];

        return allNames;
    }
}