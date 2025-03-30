namespace RetroBatScraper.Models;

using System.Xml.Serialization;

[XmlRoot("gameList")]
public class XmlGameList
{
    [XmlElement("game")]
    public List<XmlGame> Games { get; set; } = [];
}

public class XmlGame
{
    [XmlAttribute("id")]
    public String Id { get; set; } = String.Empty;

    [XmlAttribute("source")]
    public String? Source { get; set; } = String.Empty;

    [XmlElement("path")]
    public String Path { get; set; } = String.Empty;

    [XmlElement("name")]
    public String Name { get; set; } = String.Empty;

    [XmlElement("sortname")]
    public String? SortName { get; set; } = String.Empty;

    [XmlElement("desc")]
    public String? Description { get; set; } = String.Empty;

    // Emulator settings
    [XmlElement("emulator")]
    public String? Emulator { get; set; } = String.Empty;

    [XmlElement("core")]
    public String? Core { get; set; } = String.Empty;

    // Media files
    [XmlElement("image")]
    public String? Image { get; set; } = String.Empty;

    [XmlElement("video")]
    public String? Video { get; set; } = String.Empty;

    [XmlElement("marquee")]
    public String? Marquee { get; set; } = String.Empty;

    [XmlElement("thumbnail")]
    public String? Thumbnail { get; set; } = String.Empty;

    [XmlElement("fanart")]
    public String? FanArt { get; set; } = String.Empty;

    [XmlElement("titleshot")]
    public String? TitleShot { get; set; } = String.Empty;

    [XmlElement("cartridge")]
    public String? Cartridge { get; set; } = String.Empty;

    [XmlElement("map")]
    public String? Map { get; set; } = String.Empty;

    [XmlElement("manual")]
    public String? Manual { get; set; } = String.Empty;

    [XmlElement("boxart")]
    public String? BoxArt { get; set; } = String.Empty;

    [XmlElement("wheel")]
    public String? Wheel { get; set; } = String.Empty;

    [XmlElement("mix")]
    public String? Mix { get; set; } = String.Empty;

    [XmlElement("boxback")]
    public String? BoxBack { get; set; } = String.Empty;

    [XmlElement("magazine")]
    public String? Magazine { get; set; } = String.Empty;

    [XmlElement("bezel")]
    public String? Bezel { get; set; } = String.Empty;

    // Game metadata
    [XmlElement("rating")]
    public Decimal? Rating { get; set; }

    [XmlElement("releasedate")]
    public String? ReleaseDate { get; set; } = String.Empty;

    [XmlElement("developer")]
    public String? Developer { get; set; } = String.Empty;

    [XmlElement("publisher")]
    public String? Publisher { get; set; } = String.Empty;

    [XmlElement("genre")]
    public String? Genre { get; set; } = String.Empty;

    [XmlElement("family")]
    public String? Family { get; set; }

    [XmlElement("players")]
    public String? Players { get; set; }

    // Technical metadata
    [XmlElement("md5")]
    public String Md5 { get; set; } = String.Empty;

    [XmlElement("crc32")]
    public String? Crc32 { get; set; } = String.Empty;

    [XmlElement("cheevosHash")]
    public String? CheevosHash { get; set; } = String.Empty;

    [XmlElement("cheevosId")]
    public String? CheevosId { get; set; } = String.Empty;

    [XmlElement("scraperId")]
    public String? ScraperId { get; set; } = String.Empty;

    // Region and language
    [XmlElement("lang")]
    public String? Language { get; set; } = String.Empty;

    [XmlElement("region")]
    public String? Region { get; set; } = String.Empty;

    // System specific
    [XmlElement("arcadesystemname")]
    public String? ArcadeSystemName { get; set; } = String.Empty;

    [XmlElement("genreIds")]
    public String? GenreIds { get; set; } = String.Empty;

    // User data
    [XmlElement("favorite")]
    public Boolean? Favorite { get; set; }

    [XmlElement("hidden")]
    public Boolean? Hidden { get; set; }

    [XmlElement("kidgame")]
    public Boolean? KidGame { get; set; }

    [XmlElement("playcount")]
    public Int32? PlayCount { get; set; }

    [XmlElement("lastplayed")]
    public String? LastPlayed { get; set; } = String.Empty;

    [XmlElement("gametime")]
    public String? GameTime { get; set; } = String.Empty;

    // Scraper info
    [XmlElement("scrap")]
    public XmlGameScrap Scrap { get; set; } = new();
}

public class XmlGameScrap
{
    [XmlAttribute("name")]
    public String Name { get; set; } = String.Empty;

    [XmlAttribute("date")]
    public String Date { get; set; } = String.Empty;
}