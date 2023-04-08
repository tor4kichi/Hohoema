using Hohoema.Models.Niconico.Video;
using Hohoema.Models.VideoCache;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Video.Commands;

namespace Hohoema.ViewModels.VideoCache.Commands;

public sealed class CacheAddRequestCommand : VideoContentSelectionCommandBase
{
    private readonly VideoCacheManager _videoCacheManager;
    private readonly DialogService _dialogService;

    public CacheAddRequestCommand(
        VideoCacheManager videoCacheManager,
        DialogService dialogService
        )
    {
        _videoCacheManager = videoCacheManager;
        _dialogService = dialogService;
    }

    public NicoVideoQuality VideoQuality { get; set; } = NicoVideoQuality.Unknown;

    protected override void Execute(IVideoContent content)
    {
        _ = _videoCacheManager.PushCacheRequestAsync(content.VideoId, VideoQuality);
    }
}
