using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player
{
    public interface INiconicoCommentSessionProvider
    {
        VideoId ContentId { get; }
        Task<ICommentSession> CreateCommentSessionAsync();
    }

    public interface INiconicoVideoSessionProvider
    {
        VideoId ContentId { get; }
        ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }
        Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality);
    }

    public class Quality
    {
        public string QualityId { get; }
        public NicoVideoQuality NicoVideoQuality { get; }
    }

}
