namespace Hohoema.Models.Domain.VideoCache
{
    // Listring管理とDLセッション管理を分ける

    public enum VideoCacheStatus
    {
        Pending,
        Downloading,
        DownloadPaused,
        Completed,
        CompletedFromOldCache_NotEncypted,
        CompletedFromOldCache_Encrypted,
        Failed,
    }
}
