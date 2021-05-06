namespace Hohoema.Models.Domain.VideoCache
{
    public enum VideoCacheDownloadOperationFailedReason
    {
        None,
        Unknown,
        InternetUnavairable,
        NoUsageAuthority,
        CanNotCacheEncryptedContent,
        StorageCapacityLimitReached,
        RistrictedDownloadLineCount,
        NoMorePendingOrPausingItem,
        VideoDeleteFromServer,
    }
}
