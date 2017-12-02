using Mntone.Nico2;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public class LocalVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割



        public override NicoVideoQuality Quality { get; }

        public StorageFile File { get; }

        public LocalVideoStreamingSession(StorageFile file, NicoVideoQuality requestQuality, NiconicoContext context)
            : base(context)
        {
            File = file;
            Quality = requestQuality;
        }

        protected override Task<Uri> GetVideoContentUri()
        {
            return Task.FromResult(new Uri(File.Path));
        }
    }
}
