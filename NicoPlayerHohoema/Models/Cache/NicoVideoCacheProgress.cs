using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;

namespace NicoPlayerHohoema.Models.Cache
{
    public class NicoVideoCacheProgress : NicoVideoCacheRequest
    {
        public DownloadOperation DownloadOperation { get; set; }
        public IStreamingSession Session { get; }

        public NicoVideoCacheProgress()
        {

        }

        public NicoVideoCacheProgress(NicoVideoCacheRequest req, DownloadOperation op, IVideoStreamingDownloadSession session)
        {
            RawVideoId = req.RawVideoId;
            Quality = session.Quality;
            IsRequireForceUpdate = req.IsRequireForceUpdate;
            RequestAt = req.RequestAt;
            DownloadOperation = op;
            Session = session;
        }
    }
}
