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
using Mntone.Nico2.Videos.Comment;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using NicoPlayerHohoema.Views;
using Windows.UI;
using NicoPlayerHohoema.Util;
using Prism.Commands;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.ViewModels
{
	public class PlayerPageViewModel : ViewModelBase
	{
		public PlayerPageViewModel(HohoemaApp hohoemaApp)
		{
			_HohoemaApp = hohoemaApp;

			VideoStream = new ReactiveProperty<IRandomAccessStream>(UIDispatcherScheduler.Default);
			CurrentVideoPosition = new ReactiveProperty<TimeSpan>(TimeSpan.Zero);
			CommentData = new ReactiveProperty<CommentResponse>();
			IsVisibleMediaControl = new ReactiveProperty<bool>(true);
			SliderVideoPosition = new ReactiveProperty<double>(0);
			VideoLength = new ReactiveProperty<double>(0);
			CurrentState = new ReactiveProperty<MediaElementState>();
			IsAutoHideMediaControl = CurrentState.Select(x =>
				{
					return x == MediaElementState.Playing;
				})
				.ToReadOnlyReactiveProperty(true);


			this.ObserveProperty(x => x.VideoInfo)
				.Subscribe(async x =>
				{
					if (x != null)
					{
						CommentData.Value = await GetComment(x);
						VideoLength.Value = x.Length.TotalSeconds;
						SliderVideoPosition.Value = 0;
					}
				});

			Comments = new ObservableCollection<Views.Comment>();

			CommentData.Subscribe(x => 
			{
				Comments.Clear();

				if (x == null)
				{
					return;
				}


				uint fontSize_mid = 14;
				uint fontSize_small = (uint)Math.Ceiling(fontSize_mid * 0.75f);
				uint fontSize_big = (uint)Math.Floor(fontSize_mid * 1.25f);

				uint default_fontSize = fontSize_mid;
				uint default_DisplayTime = 300; // 3秒

				Color defaultColor = ColorExtention.HexStringToColor("FFFFFF");

				foreach (var comment in x.Chat)
				{
					if (comment.Text == null)
					{
						continue;
					}


					var decodedText = comment.GetDecodedText();

					var vpos = comment.GetVpos();

					var commentVM = new Comment()
					{
						CommentText = decodedText,
						CommentId = comment.GetCommentNo(),
						FontSize = default_fontSize,
						Color = defaultColor,
						VideoPosition = vpos,
						EndPosition = vpos + default_DisplayTime,
					};

//					var commandList = c.GetCommandTypes();
					var commandList = Enumerable.Empty<CommandType>();
					bool isCommendCancel = false;
					foreach (var command in commandList)
					{
						switch (command)
						{
							case CommandType.small:
								commentVM.FontSize = fontSize_small;
								break;
							case CommandType.big:
								commentVM.FontSize = fontSize_big;
								break;
							case CommandType.medium:
								commentVM.FontSize = fontSize_mid;
								break;
							case CommandType.ue:
								commentVM.VAlign = VerticalAlignment.Top;
								break;
							case CommandType.shita:
								commentVM.VAlign = VerticalAlignment.Bottom;
								break;
							case CommandType.naka:
								commentVM.VAlign = VerticalAlignment.Center;
								break;
							case CommandType.white:
								commentVM.Color = ColorExtention.HexStringToColor("FFFFFF");
								break;
							case CommandType.red:
								commentVM.Color = ColorExtention.HexStringToColor("FF0000");
								break;
							case CommandType.pink:
								commentVM.Color = ColorExtention.HexStringToColor("FF8080");
								break;
							case CommandType.orange:
								commentVM.Color = ColorExtention.HexStringToColor("FFC000");
								break;
							case CommandType.yellow:
								commentVM.Color = ColorExtention.HexStringToColor("FFFF00");
								break;
							case CommandType.green:
								commentVM.Color = ColorExtention.HexStringToColor("00FF00");
								break;
							case CommandType.cyan:
								commentVM.Color = ColorExtention.HexStringToColor("00FFFF");
								break;
							case CommandType.blue:
								commentVM.Color = ColorExtention.HexStringToColor("0000FF");
								break;
							case CommandType.purple:
								commentVM.Color = ColorExtention.HexStringToColor("C000FF");
								break;
							case CommandType.black:
								commentVM.Color = ColorExtention.HexStringToColor("000000");
								break;
							case CommandType.white2:
								commentVM.Color = ColorExtention.HexStringToColor("CCCC99");
								break;
							case CommandType.niconicowhite:
								commentVM.Color = ColorExtention.HexStringToColor("CCCC99");
								break;
							case CommandType.red2:
								commentVM.Color = ColorExtention.HexStringToColor("CC0033");
								break;
							case CommandType.truered:
								commentVM.Color = ColorExtention.HexStringToColor("CC0033");
								break;
							case CommandType.pink2:
								commentVM.Color = ColorExtention.HexStringToColor("FF33CC");
								break;
							case CommandType.orange2:
								commentVM.Color = ColorExtention.HexStringToColor("FF6600");
								break;
							case CommandType.passionorange:
								commentVM.Color = ColorExtention.HexStringToColor("FF6600");
								break;
							case CommandType.yellow2:
								commentVM.Color = ColorExtention.HexStringToColor("999900");
								break;
							case CommandType.madyellow:
								commentVM.Color = ColorExtention.HexStringToColor("999900");
								break;
							case CommandType.green2:
								commentVM.Color = ColorExtention.HexStringToColor("00CC66");
								break;
							case CommandType.elementalgreen:
								commentVM.Color = ColorExtention.HexStringToColor("00CC66");
								break;
							case CommandType.cyan2:
								commentVM.Color = ColorExtention.HexStringToColor("00CCCC");
								break;
							case CommandType.blue2:
								commentVM.Color = ColorExtention.HexStringToColor("3399FF");
								break;
							case CommandType.marineblue:
								commentVM.Color = ColorExtention.HexStringToColor("3399FF");
								break;
							case CommandType.purple2:
								commentVM.Color = ColorExtention.HexStringToColor("6633CC");
								break;
							case CommandType.nobleviolet:
								commentVM.Color = ColorExtention.HexStringToColor("6633CC");
								break;
							case CommandType.black2:
								commentVM.Color = ColorExtention.HexStringToColor("666666");
								break;
							case CommandType.full:
								break;
							case CommandType._184:
								commentVM.IsAnonimity = true;
								break;
							case CommandType.invisible:
								commentVM.IsVisible = false;
								break;
							case CommandType.all:
								isCommendCancel = true;
								break;
							case CommandType.from_button:
								break;
							case CommandType.is_button:
								break;
							case CommandType._live:
								break;
							default:
								break;
						}

						// TODO: 投稿者のコメント表示時間を伸ばす？（3秒→５秒）
						// usまたはshitaが指定されている場合に限る？
						
						// 　→　投コメ解説をみやすくしたい

						if (isCommendCancel)
						{
							commentVM = new Comment()
							{
								CommentText = decodedText,
								FontSize = default_fontSize,
								Color = defaultColor,
								VideoPosition = vpos,
								EndPosition = vpos + default_DisplayTime,
							};
							break;
						}
					}

					Comments.Add(commentVM);
				}

				System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");

			});


			SliderVideoPosition.Subscribe(x =>
			{
				_NowControlSlider = true;
				CurrentVideoPosition.Value = TimeSpan.FromSeconds(x);
				_NowControlSlider = false;
			});

			CurrentVideoPosition.Subscribe(x =>
			{
				if (_NowControlSlider) { return; }

				SliderVideoPosition.Value = x.TotalSeconds;
			});

			// メディア・コントロールが非表示状態のときShowMediaControlCommandを実行可能
			ShowMediaControlCommand = CurrentState
				.Select(x => x == MediaElementState.Playing)
				.ToReactiveCommand();

			ShowMediaControlCommand.Subscribe(x => IsVisibleMediaControl.Value = true);

			ShowMediaControlCommand
				.Where(x => CurrentState.Value == MediaElementState.Playing)
				.Throttle(TimeSpan.FromSeconds(3))
				.Repeat()
				.Subscribe(_ =>
				{
					IsVisibleMediaControl.Value = false;
				});

			CurrentState.Subscribe(x =>
			{
				if (x == MediaElementState.Paused || x == MediaElementState.Stopped)
				{
					IsVisibleMediaControl.Value = true;
				}
			});
		}

		bool _NowControlSlider = false;
		


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


		private async Task<CommentResponse> GetComment(FlvResponse response)
		{
			return await this._HohoemaApp.NiconicoContext.Video
					.GetCommentAsync(response);
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



		#region Command

		public ReactiveCommand ShowMediaControlCommand { get; private set; }


		private DelegateCommand _PlayCommand;
		public DelegateCommand PlayCommand
		{
			get
			{
				return _PlayCommand
					?? (_PlayCommand = new DelegateCommand(() =>
					{
						IsVisibleMediaControl.Value = true;
					}));
			}
		}

		private DelegateCommand _StopCommand;
		public DelegateCommand StopCommand
		{
			get
			{
				return _StopCommand
					?? (_StopCommand = new DelegateCommand(() =>
					{
						IsVisibleMediaControl.Value = true;
					}));
			}
		}


		private DelegateCommand<object> _CurrentStateChangedCommand;
		public DelegateCommand<object> CurrentStateChangedCommand
		{
			get
			{
				return _CurrentStateChangedCommand
					?? (_CurrentStateChangedCommand = new DelegateCommand<object>((arg) =>
					{
						var e = (RoutedEventArgs)arg;
						
					}
					));
			}
		}
		#endregion


		public ReactiveProperty<CommentResponse> CommentData { get; private set; }

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
	
		public ReactiveProperty<TimeSpan> CurrentVideoPosition { get; private set; }

		public ObservableCollection<Comment> Comments { get; private set; }

		public ReactiveProperty<bool> IsVisibleMediaControl { get; private set; }
		public ReadOnlyReactiveProperty<bool> IsAutoHideMediaControl { get; private set; }

		public ReactiveProperty<double> SliderVideoPosition { get; private set; }

		public ReactiveProperty<double> VideoLength { get; private set; }

		public ReactiveProperty<MediaElementState> CurrentState { get; private set; }

		private HohoemaApp _HohoemaApp;
	}


	



	
}
