using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using SharpCompress.Archives;

namespace RetroBat;

public sealed class FileDownloader
{
    private Process _aria2CProcess;

    public class DownloadProgressChangedEventArgs(String task, String raw, Int64 bytesReceived, Int64 totalBytes, Int64 speed, String eta) : EventArgs
    {
        public Double ProgressPercentage { get; private set; } = (Double)bytesReceived / totalBytes * 100;
        public String Task { get; private set; } = task;
        public String Raw { get; private set; } = raw;
        public Int64 Speed { get; private set; } = speed;
        public String Eta { get; private set; } = eta;
        public Int64 BytesReceived { get; private set; } = bytesReceived;
        public Int64 TotalBytes { get; private set; } = totalBytes;
    }

    public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;

    public static Action<String> Log { get; set; }

    private void OnProgressChanged(DownloadProgressChangedEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    public void DownloadFileAsync(String url, String destinationPath)
    {
        destinationPath = destinationPath.Replace("\\", "/");

        var filePath = Path.GetDirectoryName(destinationPath);
        var fileName = Path.GetFileName(destinationPath);

        var arguments = $"--continue --max-connection-per-server=4 --human-readable=false --console-log-level=notice --file-allocation=none --dir=\"{filePath}\" --out=\"{fileName}\" \"{url}\"";

        Log($"aria2c.exe {arguments}");

        _aria2CProcess = new()
        {
            StartInfo = new()
            {
                FileName = "aria2c.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            Application.Current.Exit += (_, _) =>
            {
                try
                {
                    _aria2CProcess.Kill();
                }
                catch
                {
                    // Ignored
                }
            };
        });

        String error = null;

        _aria2CProcess.OutputDataReceived += ProcessOutputHandler;

        _aria2CProcess.ErrorDataReceived += (_, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                error = args.Data;
            }
        };

        _aria2CProcess.Start();
        _aria2CProcess.BeginOutputReadLine();
        _aria2CProcess.BeginErrorReadLine();

        _aria2CProcess.WaitForExit();

        if (error != null)
        {
            Log($"Error: {error}");
            throw new Exception($"Error downloading file: {error}");
        }

        if (_aria2CProcess.ExitCode != 0)
        {
            Log($"ExitCode: {_aria2CProcess.ExitCode}");
            throw new Exception($"Error downloading file: aria2c exited with code {_aria2CProcess.ExitCode}");
        }
    }

    private void ProcessOutputHandler(Object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        var input = e.Data.Trim();

        if (String.IsNullOrEmpty(input))
        {
            return;
        }
        
        var progressMatch = Regex.Match(e.Data, @"\[#[a-f0-9]+ (\d+)B/(\d+)B(?:\((\d+)%\))? CN:(\d+) DL:(\d+)B(?: ETA:(?:(\d+)m)?(\d+)s)?]");

        if (!progressMatch.Success)
        {
            var statusMatch = Regex.Match(e.Data, @"\] (.+)$");
            
            if (statusMatch.Success)
            {
                var message = statusMatch.Groups[1].Value;
                OnProgressChanged(new("Downloading", message, 0, 0, 0, ""));
            }
            else
            {
                OnProgressChanged(new("Downloading", input, 0, 0, 0, ""));
            }
            
            return;
        }

        var bytesDownloaded = Int64.Parse(progressMatch.Groups[1].Value);
        var totalBytes = Int64.Parse(progressMatch.Groups[2].Value);
        var downloadSpeed = Int64.Parse(progressMatch.Groups[5].Value);
        
        if (progressMatch.Success)
        {
            var eta = "";

            if (progressMatch.Groups[6].Success || progressMatch.Groups[7].Success)
            {
                var minutes = progressMatch.Groups[6].Success ? Int32.Parse(progressMatch.Groups[6].Value) : 0;
                var seconds = Int32.Parse(progressMatch.Groups[7].Value);
                eta = $"{minutes}m{seconds}s";
            }
            
            OnProgressChanged(new("Downloading", "", bytesDownloaded, totalBytes, downloadSpeed, eta));
        }
    }

    public void UnpackFileAsync(String path, String destinationPath)
    {
        var tempDirectory = Path.Combine(Path.GetDirectoryName(destinationPath)!, "tmp");

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }

        Directory.CreateDirectory(tempDirectory);

        try
        {
            OnProgressChanged(new("Unpacking", "", 0, 0, 0, ""));

            using var archive = ArchiveFactory.Open(path);

            var sw = Stopwatch.StartNew();

            var totalSize = archive.TotalSize;

            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                archive.CompressedBytesRead += (_, args) =>
                {
                    var speed = args.CompressedBytesRead / sw.Elapsed.TotalSeconds;
                    OnProgressChanged(new("Unpacking", "", args.CompressedBytesRead, totalSize, (Int64) speed, ""));
                };

                entry.WriteToDirectory(tempDirectory);
            }

            // Find the extracted file
            var extractedFile = Directory.GetFiles(tempDirectory).FirstOrDefault();

            if (extractedFile != null)
            {
                // Check if the extensions match
                var extractedFileExtension = Path.GetExtension(extractedFile);
                var destinationFileExtension = Path.GetExtension(destinationPath);

                if (!String.Equals(extractedFileExtension, destinationFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    throw new($"The extracted file extension '{extractedFileExtension}' does not match the destination file extension '{destinationFileExtension}'.");
                }

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                File.Move(extractedFile, destinationPath);
            }
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}