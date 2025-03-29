using System.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RetroBat;

namespace Retrobat.RomDownloader
{
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
                
                var downloadExitCode = await DownloadGame(args);

                if (downloadExitCode != 0)
                {
                    Environment.Exit(downloadExitCode);
                }

                var gameExitCode = StartGame(argsString);

                if (gameExitCode != 0)
                {
                    Environment.Exit(gameExitCode);
                }

                Environment.Exit(0);
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        private static async Task<Int32> DownloadGame(Dictionary<String, String> args)
        {
            args.TryGetValue("rom", out var fileName);

            if (String.IsNullOrWhiteSpace(fileName))
            {
                return 2;
            }

            fileName = fileName.Trim('\"');

            var fileInfo = new FileInfo(fileName);

            if (!fileInfo.Exists)
            {
                return 3;
            }
            
            if (fileInfo.Length < 1024 && fileInfo.Extension == ".zip")
            {
                var contents = File.ReadAllText(fileName);
                contents = contents.Trim();

                if (!contents.StartsWith("http"))
                {
                    return 0;
                }

                var downloadWindow = new Download();

                var fileDownloader = new FileDownloader();
                fileDownloader.ProgressChanged += downloadWindow.FileDownloaderProgressChanged;

                downloadWindow.Show();
                await fileDownloader.DownloadFileAsync(contents, fileName);
                downloadWindow.Close();
            }

            return 0;
        }

        private static Int32 StartGame(String arguments)
        {
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

        private static void Log(String text)
        {
            var logPath = Path.Combine(ExecutablePath, "emulatorLauncher.log");
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {text}{Environment.NewLine}");
        }
    }
}
