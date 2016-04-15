using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.Web.Http;
using NicoPlayerHohoema.Models;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Flv;
using Mntone.Nico2.Videos.WatchAPI;
using Reactive.Bindings.Extensions;
using Reactive.Bindings;
using System.Reactive.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.ViewModels
{
	public class PlayerPageViewModel : ViewModelBase
	{
		public PlayerPageViewModel(HohoemaApp hohoemaApp)
		{
			_HohoemaApp = hohoemaApp;

			VideoStream = new ReactiveProperty<IRandomAccessStream>(UIDispatcherScheduler.Default);
		}


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			string videoUrl = null;
			try
			{
				videoUrl = (string)e.Parameter;
			}
			catch
			{
				// error
			}


			
			try
			{
				if (await _HohoemaApp.CheckSignedInStatus() == NiconicoSignInStatus.Success)
				{
					var videoId = videoUrl.Split('/').Last();

					// UIのDispatcher上で処理するために現在のウィンドウを取得
					var win = Window.Current;

					await _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(videoId)
						.ContinueWith(async prevTask =>
						{
							await win.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
							{
								VideoInfo = prevTask.Result;
								
								VideoStream.Value = await Util.HttpRandomAccessStream.CreateAsync(_HohoemaApp.NiconicoContext.HttpClient, VideoInfo.VideoUrl);
								OnPropertyChanged(nameof(CurrentVideoUrl));
							});
						});
				}
				else
				{
					// ログインに失敗
					VideoInfo = null;
				}
			}
			catch (Exception exception)
			{
				// 動画情報の取得に失敗
				System.Diagnostics.Debug.Write(exception.Message);
				
			}
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			if (VideoStream.Value != null)
			{
				var stream = VideoStream.Value;
				VideoStream.Value = null;

				stream.Dispose();
			}

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

		private WatchApiResponse _VideoInfo;
		public WatchApiResponse VideoInfo
		{
			get { return _VideoInfo; }
			set { SetProperty(ref _VideoInfo, value); }
		}



		public string CurrentVideoUrl
		{
			get
			{
				return VideoInfo?.VideoUrl?.AbsoluteUri ?? String.Empty;
			}
		}

		public ReactiveProperty<IRandomAccessStream> VideoStream { get; private set; }
	

		
		private HohoemaApp _HohoemaApp;
	}
}
