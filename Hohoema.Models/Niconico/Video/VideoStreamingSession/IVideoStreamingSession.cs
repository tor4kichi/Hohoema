﻿using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Models.Niconico.Video.VideoStreamingSession
{
    public interface IStreamingSession : IDisposable
    {
        Task StartPlayback(MediaPlayer player, TimeSpan initialPosition = default);
    }
    
    public interface IVideoStreamingSession : IStreamingSession
    {
        string QualityId { get; }
        NicoVideoQuality Quality { get; }
    }

    public interface IVideoStreamingDownloadSession : IVideoStreamingSession
    {
        Task<Uri> GetDownloadUrlAndSetupDonwloadSession();
    }

}