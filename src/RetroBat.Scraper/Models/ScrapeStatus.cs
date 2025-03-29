namespace RetroBatScraper.Models;

public class ScrapeStatus
{
    public Int32 ThreadId { get; set; }
    public String CurrentGame { get; set; } = "";
    public String Status { get; set; } = "Idle";
    public Boolean IsActive { get; set; }
    public Int32 Progress { get; set; }
    public Double DownloadProgress { get; set; }
    public Double DownloadProgressPercentage { get; set; }
    public Int64 DownloadBytesReceived { get; set; }
    public Double DownloadSpeedMbps { get; set; }
    public Int64 DownloadTotalBytes { get; set; }

    public String DisplayStatus
    {
        get
        {
            if (DownloadProgressPercentage > 0)
            {
                return $"{Status} ({DownloadSpeedMbps:N2}Mbps)";
            }

            return Status;
        }
    }

    public void Reset(String newStatus)
    {
        Status = newStatus;
        Progress = 0;
        DownloadProgress = 0;
        DownloadProgressPercentage = 0;
        DownloadBytesReceived = 0;
        DownloadSpeedMbps = 0;
    }
}