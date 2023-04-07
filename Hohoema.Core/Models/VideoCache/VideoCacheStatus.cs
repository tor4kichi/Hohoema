namespace Hohoema.Models.VideoCache
{
    // Listring管理とDLセッション管理を分ける

    public enum VideoCacheStatus
    {
        Pending,
        Downloading,
        DownloadPaused,
        Completed,
        Failed,
    }
}
