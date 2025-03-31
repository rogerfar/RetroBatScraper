using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RetroBat;

public sealed class FileDownloader
{
    public class DownloadProgressChangedEventArgs(Int64 bytesReceived, Int64 totalBytes, Double speedMbps) : EventArgs
    {
        public Double ProgressPercentage { get; private set; } = (Double)bytesReceived / totalBytes * 100;
        public Double SpeedMbps { get; private set; } = speedMbps;
        public Int64 BytesReceived { get; private set; } = bytesReceived;
        public Int64 TotalBytes { get; private set; } = totalBytes;
    }

    public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;

    private void OnProgressChanged(DownloadProgressChangedEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    public async Task DownloadFileAsync(String url, String destinationPath)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0");

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        using var contentStream = await response.Content.ReadAsStreamAsync();

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new Byte[81920];
        Int64 totalBytesRead = 0;
        Int32 bytesRead;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var lastUpdate = DateTimeOffset.UtcNow;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);

            totalBytesRead += bytesRead;

            if (totalBytes > 0)
            {
                var speedMbps = totalBytesRead / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;

                if ((DateTimeOffset.UtcNow - lastUpdate).TotalMilliseconds > 100)
                {
                    lastUpdate = DateTimeOffset.UtcNow;
                    OnProgressChanged(new(totalBytesRead, totalBytes, speedMbps));
                }
            }
        }
    }
}