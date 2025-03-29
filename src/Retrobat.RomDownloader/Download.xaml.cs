using System;

namespace RetroBat
{
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
                Progress.Content = $"Downloaded: {e.BytesReceived:N0} / {e.TotalBytes:N0} bytes ({e.ProgressPercentage:F1}%) Speed: {e.SpeedMbps:F2} MB/s";
            }));
        }
    }
}
