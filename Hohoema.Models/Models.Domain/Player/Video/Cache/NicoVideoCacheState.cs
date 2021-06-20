namespace Hohoema.Models.Domain.Player.Video.Cache
{
	public enum NicoVideoCacheState
	{
        NotCacheRequested,
		Pending,
		Downloading,
		Cached,

		Failed,
		FailedWithQualityNotAvairable,
		DeletedFromUser,
		DeletedFromNiconicoServer,
	}
}
