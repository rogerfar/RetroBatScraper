namespace RetroBat.Scraper.Models;

public class GameLink
{
    public required String Name { get; set; }

    public required String FileName { get; set; }

    public required String Url { get; set; }

    public List<String> Languages { get; set; } = [];

    public List<String> Regions { get; set; } = [];

    public Boolean IsAftermarket { get; set; }

    public Boolean IsBeta { get; set; }

    public Boolean IsDemo { get; set; }

    public Boolean IsPrototype { get; set; }

    public Boolean IsUnlicensed { get; set; }

    public String? Edition { get; set; }

    public DateTime? BuildDate { get; set; }

    public List<String> Tags { get; set; } = [];

}
