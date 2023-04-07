namespace Hohoema.Models.VideoCache
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
        RequirePermission_Admission,
        RequirePermission_Premium,
        RequirePermission_Ppv,
    }
}
