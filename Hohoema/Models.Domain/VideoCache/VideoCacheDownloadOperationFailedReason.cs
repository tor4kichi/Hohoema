namespace Hohoema.Models.Domain.VideoCache
{
    public enum VideoCacheDownloadOperationFailedReason
    {
        Unknown,
        None,
        InternetUnavairable,
        NoUsageAuthority,
        CanNotCacheEncryptedContent,
        StorageCapacityLimitReached,
        RistrictedDownloadLineCount,
        NoMorePendingOrPausingItem,
        VideoDeleteFromServer,
    }
}
