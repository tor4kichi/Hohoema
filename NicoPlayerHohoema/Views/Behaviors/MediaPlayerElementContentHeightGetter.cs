using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class MediaPlayerElementContentHeightGetter : Behavior<MediaPlayerElement>
	{

		#region ContentHeight


		public static readonly DependencyProperty ContentHeightProperty =
			DependencyProperty.RegisterAttached(
				nameof(ContentHeight),
				typeof(double),
				typeof(MediaPlayerElementContentHeightGetter),
				new PropertyMetadata(default(double)));

		public double ContentHeight
		{
			get { return (double)GetValue(ContentHeightProperty); }
			set { SetValue(ContentHeightProperty, value); }
		}



		#endregion



		#region ContentWidth

		public static readonly DependencyProperty ContentWidthProperty =
			DependencyProperty.RegisterAttached(
				nameof(ContentWidth),
				typeof(double),
				typeof(MediaPlayerElementContentHeightGetter),
				new PropertyMetadata(default(double)));

		// プログラムからアクセスするための添付プロパティのラッパー
		public double ContentWidth
		{
			get { return (double)GetValue(ContentWidthProperty); }
			set { SetValue(ContentWidthProperty, value); }
		}

		#endregion


		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}


		private bool IsSizeChanged;
		private Timer _Timer;

        MediaPlayer _MediaPlayer;

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
            _MediaPlayer = this.AssociatedObject.MediaPlayer;

            this.AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            _MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            IsSizeChanged = true;
			StartEnsureResizeNotifyTimer();
		}

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            IsSizeChanged = true;
            StartEnsureResizeNotifyTimer();
        }
        

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			IsSizeChanged = true;
			StartEnsureResizeNotifyTimer();
		}



		public void StartEnsureResizeNotifyTimer()
		{
			if (IsSizeChanged == false)
			{
				return;
			}

			_Timer?.Dispose();

			_Timer = new Timer(TryCalc, this, 100, 250);


		}

		async void TryCalc(object state = null)
		{
			if (IsSizeChanged == false)
			{
				_Timer?.Dispose();
				_Timer = null;
				return;
			}

			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				if (AssociatedObject == null) { return; }

                var playbackSession = _MediaPlayer.PlaybackSession;


                if (playbackSession.PlaybackState == MediaPlaybackState.None)
				{
					return;
				}

				if (playbackSession.NaturalVideoWidth == 0)
				{
					return;
				}

				// ビデオの縦横比と実際に表示している領域の縦横比を出して
				// ビデオの縦横比のほうが大きい時＝縦に余白ができる時に
				// 実際に表示される縦サイズを計算する

				// 縦に余白が出来ない時は、実際の表示領域の縦サイズそのまま使う

				var naturalContentVHRatio = playbackSession.NaturalVideoWidth / (float)playbackSession.NaturalVideoHeight;
				var canvasVHRatio = AssociatedObject.ActualWidth / AssociatedObject.ActualHeight;
				if (naturalContentVHRatio > canvasVHRatio)
				{
					var ratio = AssociatedObject.ActualWidth / playbackSession.NaturalVideoWidth;
					ContentHeight = playbackSession.NaturalVideoHeight * ratio;

					ContentWidth = AssociatedObject.ActualWidth;
				}
				else
				{
					var ratio = AssociatedObject.ActualHeight / playbackSession.NaturalVideoHeight;
					ContentWidth = playbackSession.NaturalVideoWidth * ratio;

					ContentHeight = AssociatedObject.ActualHeight;
				}

				IsSizeChanged = false;

				_Timer?.Dispose();
				_Timer = null;
			});
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			this.AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;

			_Timer?.Dispose();
		}


	}


}
