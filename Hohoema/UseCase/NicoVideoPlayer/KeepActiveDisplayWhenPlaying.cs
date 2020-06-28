using Hohoema.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.UseCase.NicoVideoPlayer
{
    public class KeepActiveDisplayWhenPlaying : IDisposable
    {
		private readonly MediaPlayer _mediaPlayer;
		private readonly IScheduler _scheduler;

		public KeepActiveDisplayWhenPlaying(MediaPlayer mediaPlayer, IScheduler scheduler)
        {
			_mediaPlayer = mediaPlayer;
			_scheduler = scheduler;
			_mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
		}

		private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
		{
			_scheduler.Schedule(() =>
			{
				SetKeepDisplayWithCurrentState(sender.PlaybackState);
			});
		}

		private void SetKeepDisplayWithCurrentState(MediaPlaybackState state)
		{
			if (state == MediaPlaybackState.Paused || state == MediaPlaybackState.None)
			{
				ExitKeepDisplay();
			}
			else
			{
				SetKeepDisplayIfEnable();
			}
		}


		void SetKeepDisplayIfEnable()
		{
			ExitKeepDisplay();

			DisplayRequestHelper.RequestKeepDisplay();
		}

		void ExitKeepDisplay()
		{
			DisplayRequestHelper.StopKeepDisplay();
		}

		public void Dispose()
		{
			_mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

			ExitKeepDisplay();
		}
	}
}
