namespace RetroBatScraper.Models;

public class GameLink
{
    public required String Name { get; set; }
    public required String FileName { get; set; }
    public required String Url { get; set; }
    public List<String> Languages { get; set; } = [];
    public List<String> Regions { get; set; } = [];
    public List<String> Tags { get; set; } = [];
}
