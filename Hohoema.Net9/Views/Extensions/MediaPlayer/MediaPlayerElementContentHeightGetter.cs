#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Notification;
using Microsoft.Xaml.Interactivity;
using System;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Behaviors;

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


	private bool IsSizeChanged;
	private readonly DispatcherQueueTimer _Timer;


	public MediaPlayerElementContentHeightGetter()
	{
		_Timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		_Timer.IsRepeating = false;
		_Timer.Interval = TimeSpan.FromMilliseconds(100);
		_Timer.Tick += _Timer_Tick;
	}

	protected override void OnAttached()
	{
		base.OnAttached();

		this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;

		this.AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;

		StartEnsureResizeNotifyTimer();
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
		this.AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

		this.AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;

		_Timer.Stop();
	}



	private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
	{
		StartEnsureResizeNotifyTimer();
	}

	private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
	{
		_Timer.Stop();
	}

	private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		StartEnsureResizeNotifyTimer();
	}



	public void StartEnsureResizeNotifyTimer()
	{
		IsSizeChanged = true;

		_Timer.Start();
	}

	private void _Timer_Tick(object sender, object e)
	{
		TryCalc();
	}

	void TryCalc(object state = null)
	{
		try
		{
            if (AssociatedObject?.MediaPlayer == null) { return; }

            var playbackSession = this.AssociatedObject.MediaPlayer.PlaybackSession;
			if (playbackSession.PlaybackState == MediaPlaybackState.None) { return; }
			if (playbackSession.NaturalVideoWidth == 0) { return; }

            IsSizeChanged = false;

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
        }
		catch
		{
			Ioc.Default.GetRequiredService<IMessenger>().Send(new LiteNotificationMessage(new LiteNotificationPayload() { Content = "動画プレイヤーのサイズ計算に失敗しました。" }));
			IsSizeChanged = false;
			throw;
		}
		finally
		{
			if (IsSizeChanged)
			{
				_Timer.Start();
            }
		}
		
    }

}
