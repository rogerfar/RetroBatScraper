using System;
using System.Collections.Generic;

namespace RetroBat.Scraper.Models;

using System.Xml.Serialization;

[XmlRoot("gameList")]
public class XmlGameList
{
    [XmlElement("game")]
    public List<XmlGame> Games { get; set; } = [];
}

public class XmlGame
{
    [XmlElement("path")]
    public String Path { get; set; } = String.Empty;

}
