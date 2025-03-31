using System.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using RetroBat;
using RetroBatScraper.Models;

namespace Retrobat.RomDownloader;

public partial class App
{
    private static readonly String ExecutablePath = Directory.GetCurrentDirectory();

    private async void App_Startup(Object sender, StartupEventArgs e)
    {
        try
        {
            var args = ParseArgsToDictionary(e.Args);
            var argsString = String.Join(" ", args.Select(m => $"-{m.Key} \"{m.Value}\""));

            Log(argsString);

            if (!args.TryGetValue("rom", out var fileName))
            {
                if (!args.TryGetValue("gameinfo", out var gameinfo))
                {
                    Log($"Cannot find 'rom' or 'gameinfo' argument");
                    Environment.Exit(1);
                }
                else
                {
                    var gameList = GetGamInfo(gameinfo);
                    var gameInfo = gameList.Games.FirstOrDefault();

                    if (gameInfo == null)
                    {
                        Log($"Gameinfo file does not contain any games");
                        Environment.Exit(1);
                    }

                    fileName = gameInfo.Path;
                }
            }
                
            var downloadExitCode = await DownloadGame(fileName);

            if (downloadExitCode != 0)
            {
                Log($"Downloader exited with exit code {downloadExitCode}");
                Environment.Exit(downloadExitCode);
            }

            var gameExitCode = StartGame(argsString);

            if (gameExitCode != 0)
            {
                Log($"Startgame exited with exit code {downloadExitCode}");
                Environment.Exit(gameExitCode);
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task<Int32> DownloadGame(String fileName)
    {
        if (String.IsNullOrWhiteSpace(fileName))
        {
            Log($"Cannot find rom file path in argument 'rom'");
            return 2;
        }

        fileName = fileName.Trim('\"');

        var fileInfo = new FileInfo(fileName);

        if (!fileInfo.Exists)
        {
            Log($"Cannot find rom file {fileName}");
            return 3;
        }

        if (fileInfo.Length < 1024 && fileInfo.Extension == ".zip")
        {
            var contents = File.ReadAllText(fileName);
            contents = contents.Trim();

            if (!contents.StartsWith("http"))
            {
                Log($"Rom does not contain a valid URL");

                return 0;
            }

            var downloadWindow = new Download();

            var fileDownloader = new FileDownloader();
            fileDownloader.ProgressChanged += downloadWindow.FileDownloaderProgressChanged;

            Log($"Downloading rom file from {contents} to {fileName}");

            downloadWindow.Show();

            try
            {
                await fileDownloader.DownloadFileAsync(contents, fileName);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            downloadWindow.Close();

            if (!File.Exists(fileName))
            {
                Log($"Downloaded rom file does not exist");
                return 4;
            }
        }
        else
        {
            Log($"Not downloading rom file, size is {fileInfo.Length} and extension is {fileInfo.Extension}");
        }

        return 0;
    }

    private static Int32 StartGame(String arguments)
    {
        Log($"Starting executable emulatorLauncher_original.exe with arguments");

        var process = new Process();
        process.StartInfo.FileName = "emulatorLauncher_original.exe";
        process.StartInfo.Arguments = arguments;

        process.Start();

        process.WaitForExit();

        return process.ExitCode;
    }

    private static Dictionary<String, String> ParseArgsToDictionary(String[] args)
    {
        var argsDictionary = new Dictionary<String, String>();

        for (var i = 0; i < args.Length; i+=2)
        {
            var arg = args[i];
            if (arg.StartsWith("-") && i + 1 < args.Length)
            {
                argsDictionary[args[i].TrimStart('-')] = args[i + 1];
            }
        }

        return argsDictionary;
    }

    public XmlGameList GetGamInfo(String filePath)
    {
        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = false
        };

        var xml = File.ReadAllText(filePath);

        using var stringReader = new StringReader(xml);

        using var xmlReader = XmlReader.Create(stringReader, settings);

        var serializer = new XmlSerializer(typeof(XmlGameList));

        return (XmlGameList)serializer.Deserialize(xmlReader)!;
    }

    private static void Log(String text)
    {
        var logPath = Path.Combine(ExecutablePath, "emulatorLauncher.log");
        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {text}{Environment.NewLine}");
    }
}