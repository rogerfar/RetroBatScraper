using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using RetroBat.Scraper.Models;

namespace RetroBat.Scraper.Services;

public class FileDownloaderService
{
    private readonly HttpClient _httpClient;

    public FileDownloaderService()
    {
        _httpClient = new();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0");
    }

    private static readonly Regex LinkRegex = new("""<td class="link"><a href="(?<link>.*?)" title="(?<title>.*?)">(?<name>.*?)</a></td>""", RegexOptions.Singleline & RegexOptions.IgnoreCase & RegexOptions.Compiled);
    private static readonly Regex RegionRegex = new(@"\((?<region>.*?)\)", RegexOptions.IgnoreCase & RegexOptions.Compiled);

    private static readonly Dictionary<String, String> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        { "En", "English" },
        { "Fr", "French" },
        { "De", "German" },
        { "Es", "Spanish" },
        { "It", "Italian" },
        { "Ja", "Japanese" },
        { "Nl", "Dutch" },
        { "Sv", "Swedish" },
        { "Pt", "Portuguese" },
        { "Da", "Danish" },
        { "Fi", "Finnish" },
        { "No", "Norwegian" },
        { "Ko", "Korean" },
        { "Zh", "Chinese" },
        { "Ru", "Russian" },
        { "Pl", "Polish" }
    };

    private static readonly Dictionary<String, String> Regions = new()
    {
        { "USA", "English" },
        { "Japan", "Japanese" },
        { "Europe", "English" },
        { "Germany", "German" },
        { "Australia", "English" },
        { "World", "English" },
        { "France", "French" },
        { "Italy", "Italian" },
        { "Spain", "Spanish" },
        { "Brazil", "Portuguese" },
        { "Asia", "" },
        { "Unknown", "" }
    };

    public async Task<List<GameLink>> GetGames(String url)
    {
        var page = await _httpClient.GetStringAsync(url);

        url = url.TrimEnd('/');

        var matches = LinkRegex.Matches(page);
        var games = new List<GameLink>();

        foreach (Match match in matches)
        {
            var link = Uri.UnescapeDataString(match.Groups["link"].Value).Trim();
            var name = Uri.UnescapeDataString(match.Groups["name"].Value).Trim();
            
            if (name.Contains("[BIOS]", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            name = HttpUtility.HtmlDecode(name);
            
            var newGame = new GameLink
            {
                Name = name,
                FileName = name,
                Url = $"{url}/{link}"
            };

            name = Path.GetFileNameWithoutExtension(name);

            // Get the region from the name, often in brackets
            var regionMatches = RegionRegex.Matches(name);

            foreach (Match regionMatch in regionMatches)
            {
                var v = regionMatch.Groups[1].Value;
                name = name.Replace($"({v})", "");

                var contents = v.Split(',');

                foreach (var entry in contents)
                {
                    var trimmed = entry.Trim();

                    if (Regions.ContainsKey(trimmed))
                    {
                        newGame.Regions.Add(trimmed);

                        if (Regions[trimmed] != "")
                        {
                            newGame.Languages.Add(Regions[trimmed]);
                        }
                    }
                    else if (Languages.TryGetValue(trimmed, out var language))
                    {
                        newGame.Languages.Add(language);
                    }
                    else
                    {
                        newGame.Tags.Add(trimmed);
                    }
                }
            }

            while (name.Contains("  "))
            {
                name = name.Replace("  ", " ");
            }
            
            newGame.Name = name.Trim();
            newGame.Languages = newGame.Languages.Distinct().OrderBy(m => m).ToList();
            newGame.Regions = newGame.Regions.Distinct().OrderBy(m => m).ToList();

            games.Add(newGame);
        }

        return games.ToList();
    }

    public async Task DownloadFakeGames(String gameFileName, String url, String path, String extension)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var files = Directory.GetFiles(path, "*.*");

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (String.Equals(fileName, gameFileName))
            {
                return;
            }
        }

        extension = extension.TrimStart('.');

        var destinationFileName = Path.Combine(path, $"{gameFileName}.{extension}");

        if (File.Exists(destinationFileName))
        {
            return;
        }

        await File.WriteAllTextAsync(destinationFileName, url);
    }
}