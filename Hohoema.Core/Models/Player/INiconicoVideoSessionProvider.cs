#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player.Comment;
using NiconicoToolkit.Video;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Hohoema.Models.Player;

public interface INiconicoCommentSessionProvider<TComment> where TComment : IComment
{
    VideoId ContentId { get; }
    Task<ICommentSession<TComment>> CreateCommentSessionAsync();
}

public interface INiconicoVideoSessionProvider
{
    VideoId ContentId { get; }
    ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }
    Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQualityEntity quality);
}

public class Quality
{
    public string QualityId { get; }
    public NicoVideoQuality NicoVideoQuality { get; }
}
