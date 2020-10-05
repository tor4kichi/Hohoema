using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public sealed class NicoVideoQualityEntity 
    {
        internal NicoVideoQualityEntity(
            bool isAvailable, NicoVideoQuality quality, string qualityId, 
            int? bitrate = null, int? width = null, int? height = null
            )
        {
            IsAvailable = isAvailable;
            Quality = quality;
            QualityId = qualityId;
            Bitrate = bitrate;
            Width = width;
            Height = height;
        }

        public bool IsAvailable { get; }
        
        public NicoVideoQuality Quality { get; }
        public string QualityId { get; }

        public int? Bitrate { get; }
        public int? Width { get; }
        public int? Height { get; }
    }    
}
