using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Niconico
{
    interface INiconicoCommentSessionProvider
    {
        Task<ICommentSession> CreateCommentSessionAsync(string contentId);
    }

    interface INiconicoStreamingSessionProvider
    {
        Task<IStreamingSession> CreateStreamingSessionAsync(string contentId, NicoVideoQuality requestedQuality = NicoVideoQuality.Unknown);
    }
}
