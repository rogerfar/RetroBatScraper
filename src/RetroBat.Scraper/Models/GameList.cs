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

    [XmlElement("path")]
    public String Path { get; set; } = String.Empty;

    [XmlElement("name")]
    public String Name { get; set; } = String.Empty;

    [XmlElement("desc")]
    public String? Description { get; set; } = String.Empty;

    [XmlElement("image")]
    public String? Image { get; set; } = String.Empty;

    [XmlElement("video")]
    public String? Video { get; set; } = String.Empty;

    [XmlElement("marquee")]
    public String? Marquee { get; set; } = String.Empty;

    [XmlElement("thumbnail")]
    public String? Thumbnail { get; set; } = String.Empty;

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
    public String? Players { get; set; } = String.Empty;

    [XmlElement("md5")]
    public String Md5 { get; set; } = String.Empty;

    [XmlElement("lang")]
    public String? Language { get; set; } = String.Empty;

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
