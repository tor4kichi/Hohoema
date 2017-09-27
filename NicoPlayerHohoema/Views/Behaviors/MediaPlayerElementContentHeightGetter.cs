using Microsoft.Xaml.Interactivity;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Core;
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
            this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
		}

       

        private bool IsSizeChanged;
		private DispatcherTimer _Timer = new DispatcherTimer();

        MediaPlayer _MediaPlayer;
        CoreDispatcher _UIDispatcher;

        IDisposable Disposer;
        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_MediaPlayer != null)
            {
                _MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            }

            Disposer?.Dispose();
            Disposer = null;
            _Timer.Stop();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
            Disposer = this.AssociatedObject.ObserveDependencyProperty(MediaPlayerElement.MediaPlayerProperty)
                .Subscribe(_ => 
                {
                    this.AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
                    
                    if (this.AssociatedObject.MediaPlayer != null)
                    {
                        this.AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
                        this.AssociatedObject.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

                        _MediaPlayer = this.AssociatedObject.MediaPlayer;
                        IsSizeChanged = true;
                        StartEnsureResizeNotifyTimer();
                    }
                    else
                    {
                        _MediaPlayer = null;
                        this.AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
                        _Timer?.Stop();
                    }
                });

            _UIDispatcher = this.AssociatedObject.Dispatcher;
            _Timer.Interval = TimeSpan.FromMilliseconds(100);
            _Timer.Tick += _Timer_Tick;

            _MediaPlayer = this.AssociatedObject.MediaPlayer;

            this.AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            if (this.AssociatedObject.MediaPlayer != null)
            {
                this.AssociatedObject.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            }

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



		public async void StartEnsureResizeNotifyTimer()
		{
			if (IsSizeChanged == false)
			{
				return;
			}

            await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
            {
                _Timer.Start();
            });
        }

        private void _Timer_Tick(object sender, object e)
        {
            TryCalc();
        }

        async void TryCalc(object state = null)
		{
            if (_MediaPlayer == null) { return; }

			if (IsSizeChanged == false)
			{
				_Timer.Stop();
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

                _Timer.Stop();
			});
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			this.AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;

            _Timer.Stop();
        }


	}


}
