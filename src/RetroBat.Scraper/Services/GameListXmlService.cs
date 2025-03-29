using System.IO;
using RetroBatScraper.Models;
using System.Xml.Serialization;
using System.Xml;

namespace RetroBatScraper.Services;

public class GameListXmlService
{
    private readonly XmlSerializer _serializer = new(typeof(XmlGameList));

    public async Task<XmlGameList> ReadGameListAsync(String filePath)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        return (XmlGameList)_serializer.Deserialize(fileStream)!;
    }

    public async Task WriteGameListAsync(String filePath, XmlGameList gameList)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            Async = true
        };

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await using var xmlWriter = XmlWriter.Create(fileStream, settings);

        // Write XML declaration manually to match the exact format
        await xmlWriter.WriteStartDocumentAsync(standalone: false);

        _serializer.Serialize(xmlWriter, gameList);
    }
}
