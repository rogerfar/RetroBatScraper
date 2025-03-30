using System.IO;
using RetroBatScraper.Models;
using System.Xml.Serialization;
using System.Xml;
using Serilog;

namespace RetroBatScraper.Services;

public class GameListXmlService
{
    private readonly XmlSerializer _serializer = new(typeof(XmlGameList));

    public async Task<XmlGameList> ReadGameListAsync(String filePath)
    {
        var xml = await File.ReadAllTextAsync(filePath);

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = false
        };

        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        var lineInfo = xmlReader as IXmlLineInfo;

        try
        {
            var serializer = new XmlSerializer(typeof(XmlGameList));
            return (XmlGameList)serializer.Deserialize(xmlReader)!;
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, ex.Message);

            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                Log.Information("Line: {LineNumber}, Position: {LinePosition}", lineInfo.LineNumber, lineInfo.LinePosition);

                var lines = xml.Split('\n');
                var errorLine = lineInfo.LineNumber - 1;
                var startLine = Math.Max(0, errorLine - 2);
                var endLine = Math.Min(lines.Length - 1, errorLine + 2);

                for (var i = startLine; i <= endLine; i++)
                {
                    var marker = (i == errorLine) ? ">>" : "  ";
                    Log.Information($"{marker} {i + 1}: {lines[i].TrimEnd()}");
                }
            }
            
            throw;
        }
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
