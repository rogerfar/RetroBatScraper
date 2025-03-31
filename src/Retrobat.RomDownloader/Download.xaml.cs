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

            Progress.Content = $"{ConvertBytes(e.BytesReceived)} / {ConvertBytes(e.TotalBytes)} ({e.ProgressPercentage:F1}%)";
            Speed.Content = $"{e.SpeedMbps / 8.0d:N2} MB/s";
        }));
    }

    private static String ConvertBytes(Int64 bytes)
    {
        const Int64 kb = 1024;
        const Int64 mb = kb * 1024;
        const Int64 gb = mb * 1024;

        return bytes switch
        {
            >= gb => $"{(Double)bytes / gb:F2} GB",
            >= mb => $"{(Double)bytes / mb:F2} MB",
            >= kb => $"{(Double)bytes / kb:F2} KB",
            _ => $"{bytes} bytes"
        };
    }
}