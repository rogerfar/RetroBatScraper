using System;

namespace RetroBat;

public partial class Download
{
    public Download()
    {
        InitializeComponent();
    }

    public void FileDownloaderProgressChanged(Object sender, FileDownloader.DownloadProgressChangedEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            Task.Content = e.Task;

            if (e.Raw != "")
            {
                Progress.Content = e.Raw;
            }
            else if (e.BytesReceived > 0 && e.TotalBytes > 0)
            {
                Progress.Content = $"{FormatBytes(e.BytesReceived)} / {FormatBytes(e.TotalBytes)} ({e.ProgressPercentage:F1}%)";
            }
            else
            {
                Progress.Content = "";
            }

            if (e.Speed > 0 && e.Eta != "")
            {
                Speed.Content = $"{FormatBytes(e.Speed)}/s ETA: {e.Eta}";
            }
            else if (e.Speed > 0)
            {
                Speed.Content = $"{FormatBytes(e.Speed)}/s";
            }
            else
            {
                Speed.Content = "";
            }
        }));
    }

    private static String FormatBytes(Int64 bytes)
    {
        String[] sizes = ["B", "KiB", "MiB", "GiB", "TiB"];
        var order = 0;
        Double size = bytes;
            
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##}{sizes[order]}";
    }
}