﻿using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.VideoStreamingSession;
using Hohoema.Models.Repository.Niconico.NicoVideo.Comment;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo
{
    public interface INiconicoCommentSessionProvider
    {
        string ContentId { get; }
        Task<ICommentSession> CreateCommentSessionAsync();
    }

    public interface INiconicoVideoSessionProvider
    {
        string ContentId { get; }
        ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }
        Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality);
    }


    // TODO: QualityとQualityIDは一つのクラスで扱うべき

    public class Quality
    {
        public string QualityId { get; }
        public NicoVideoQuality NicoVideoQuality { get; }

        // TODO: Mntone.Video.VideoContentの情報を持たせる
    }

}
