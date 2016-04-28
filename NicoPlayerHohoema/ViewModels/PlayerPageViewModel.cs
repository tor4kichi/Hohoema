using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Flv;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.ViewModels
{
	public class PlayerPageViewModel : ViewModelBase, IDisposable
	{

		public static SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler;

		public PlayerPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea)
		{
			if (PlayerWindowUIDispatcherScheduler == null)
			{
				PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
			}


			ea.GetEvent<Events.PlayerClosedEvent>()
				.Subscribe(_ =>
				{
					StopCommand.Execute();
				});

			_HohoemaApp = hohoemaApp;


			VideoStream = new ReactiveProperty<IRandomAccessStream>(PlayerWindowUIDispatcherScheduler);
			CurrentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero);
			ReadVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero);
			CommentData = new ReactiveProperty<CommentResponse>(PlayerWindowUIDispatcherScheduler);
			IsVisibleMediaControl = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true);
			SliderVideoPosition = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0);
			VideoLength = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0);
			CurrentState = new ReactiveProperty<MediaElementState>(PlayerWindowUIDispatcherScheduler);
			Comments = new ObservableCollection<Views.Comment>();


			IsAutoHideMediaControl = CurrentState.Select(x =>
				{
					return x == MediaElementState.Playing;
				})
				.ToReadOnlyReactiveProperty(true, eventScheduler: PlayerWindowUIDispatcherScheduler);


			this.ObserveProperty(x => x.VideoInfo)
				.Subscribe(async x =>
				{
					if (x != null)
					{
						CommentData.Value = await GetComment(x);
						VideoLength.Value = x.Length.TotalSeconds;
						SliderVideoPosition.Value = 0;
						UpdateMediaInfoContent();
					}
				});

			// メディア・コントロールが非表示状態のときShowMediaControlCommandを実行可能
			ShowMediaControlCommand = CurrentState
				.Select(x => x == MediaElementState.Playing)
				.ToReactiveCommand(PlayerWindowUIDispatcherScheduler);

			ShowMediaControlCommand
				.Subscribe(x => IsVisibleMediaControl.Value = true);


		 
			CommentData.Subscribe(x => 
			{
				Comments.Clear();

				if (x == null)
				{
					return;
				}


				uint fontSize_mid = 26;
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
				ReadVideoPosition.Value = CurrentVideoPosition.Value;
				_NowControlSlider = false;
			});

			ReadVideoPosition.Subscribe(x =>
			{
				if (_NowControlSlider) { return; }

				SliderVideoPosition.Value = x.TotalSeconds;
			});

			
			
			ShowMediaControlCommand
				.Where(x => CurrentState.Value == MediaElementState.Playing)
				.Delay(TimeSpan.FromSeconds(3))
				.Where(x => CurrentState.Value == MediaElementState.Playing)
				.Repeat()
				.SubscribeOnUIDispatcher()
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




			// Media Info

			MediaInfoTypeToVM = new Dictionary<MediaInfoDisplayType, MediaInfoViewModel>();
			MediaInfoTypeList = new List<MediaInfoDisplayType>(
				(IEnumerable<MediaInfoDisplayType>)Enum.GetValues(typeof(MediaInfoDisplayType))
				);

			SelectedMediaInfoType = new ReactiveProperty<MediaInfoDisplayType>(PlayerWindowUIDispatcherScheduler);

			SelectedMediaInfoType
				.SubscribeOn(PlayerWindowUIDispatcherScheduler)
				.Subscribe(x =>
				{
					UpdateMediaInfoContent();
				});				
		}


		private void UpdateMediaInfoContent()
		{
			if (VideoInfo == null) { return; }

			var type = SelectedMediaInfoType.Value;

			MediaInfoContent?.OnLeave();

			if (MediaInfoTypeToVM.ContainsKey(type))
			{
				MediaInfoContent = MediaInfoTypeToVM[type];
			}
			else
			{
				var createTask = CreateMediaInfoVMFromType(type);
				createTask.Wait(500);
				if (createTask.IsCompleted)
				{
					var mediaInfoVM = createTask.Result;
					mediaInfoVM.OnInitailize();
					MediaInfoTypeToVM.Add(type, mediaInfoVM);
					MediaInfoContent = mediaInfoVM;
				}
				else
				{
					throw new Exception();
				}
			}

			MediaInfoContent.OnEnter();
		}


		private async Task<MediaInfoViewModel> CreateMediaInfoVMFromType(MediaInfoDisplayType type)
		{
			switch (type)
			{
				case MediaInfoDisplayType.Summary:
					
					var videoId = Util.NicoVideoExtention.UrlToVideoId(SourceVideoUrl);
					var thumbnail = await _HohoemaApp.GetThumbnail(videoId);
					return new SummaryMediaInfoViewModel(thumbnail, this.VideoInfo);
				case MediaInfoDisplayType.Comment:
					return new CommentMediaInfoViewModel(Comments);
				case MediaInfoDisplayType.Playlist:
					return new PlaylistMediaInfoViewModel();
				case MediaInfoDisplayType.Related:
					return new RelatedVideoMediaInfoViewModel(
						Util.NicoVideoExtention.UrlToVideoId(VideoInfo.VideoUrl.AbsolutePath)
						);
				case MediaInfoDisplayType.Ichiba:
					return new IchibaMediaInfoViewModel(
						Util.NicoVideoExtention.UrlToVideoId(VideoInfo.VideoUrl.AbsolutePath)
						);
				default:
					throw new NotSupportedException($"not support {nameof(MediaInfoDisplayType)}.{type.ToString()}");
			}
		}

		bool _NowControlSlider = false;
		


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			System.Diagnostics.Debug.WriteLine($"Player Navigated ViewId is {ApplicationView.GetForCurrentView().Id}");


			
			if (e?.Parameter is string)
			{
				SourceVideoUrl = e.Parameter as string;
			}
			else if(viewModelState.ContainsKey(nameof(CurrentVideoUrl)))
			{
				SourceVideoUrl = (string)viewModelState[nameof(CurrentVideoUrl)];
			}



			var currentUIDispatcher = Window.Current.Dispatcher;

			try
			{
				if (SourceVideoUrl == null) { return; }

				if (await _HohoemaApp.CheckSignedInStatus() == NiconicoSignInStatus.Success)
				{
					var videoId = SourceVideoUrl.Split('/').Last();

					await _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(videoId)
						.ContinueWith(async prevTask =>
						{
							await currentUIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
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


			if (viewModelState.ContainsKey(nameof(CurrentVideoPosition)))
			{
				CurrentVideoPosition.Value = TimeSpan.FromSeconds((double)viewModelState[nameof(CurrentVideoPosition)]);
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

			Comments.Clear();

			if (suspending)
			{
				viewModelState.Add(nameof(CurrentVideoUrl), SourceVideoUrl);
				viewModelState.Add(nameof(CurrentVideoPosition), CurrentVideoPosition.Value.TotalSeconds);
			}
			base.OnNavigatingFrom(e, viewModelState, suspending);
		}

		public void Dispose()
		{
			foreach (var vm in MediaInfoTypeToVM.Values)
			{
				vm.Dispose();
			}

			VideoStream.Value?.Dispose();
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

		private string _SourceVideoUrl;
		public string SourceVideoUrl
		{
			get
			{
				return _SourceVideoUrl;
			}
			set
			{
				SetProperty(ref _SourceVideoUrl, value);
			}
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
		public ReactiveProperty<TimeSpan> ReadVideoPosition { get; private set; }

		public ObservableCollection<Comment> Comments { get; private set; }

		public ReactiveProperty<bool> IsVisibleMediaControl { get; private set; }
		public ReadOnlyReactiveProperty<bool> IsAutoHideMediaControl { get; private set; }

		public ReactiveProperty<double> SliderVideoPosition { get; private set; }

		public ReactiveProperty<double> VideoLength { get; private set; }

		public ReactiveProperty<MediaElementState> CurrentState { get; private set; }


		public List<MediaInfoDisplayType> MediaInfoTypeList { get; private set; }
		public ReactiveProperty<MediaInfoDisplayType> SelectedMediaInfoType { get; private set; }
		public Dictionary<MediaInfoDisplayType, MediaInfoViewModel> MediaInfoTypeToVM { get; private set; }

		private MediaInfoViewModel _MediaInfoContent;
		public MediaInfoViewModel MediaInfoContent
		{
			get
			{
				return _MediaInfoContent;
			}
			set
			{
				SetProperty(ref _MediaInfoContent, value);
			}
		}

		// Note: 新しいReactivePropertyを追加したときの注意点
		// RPではPlayerWindowUIDispatcherSchedulerを使うこと



		private HohoemaApp _HohoemaApp;
	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Comment,
		Playlist,
		Related,
		Ichiba,
	}

	abstract public class MediaInfoViewModel : BindableBase, IDisposable
	{
		abstract public void OnInitailize();
		abstract public void OnEnter();
		abstract public void OnLeave();

		abstract public void Dispose();
	}


	public class SummaryMediaInfoViewModel : MediaInfoViewModel
	{

		public SummaryMediaInfoViewModel(ThumbnailResponse thumbnail, WatchApiResponse watchapi)
		{
			_ThumbnailResponse = thumbnail;
			_WatchApiRes = watchapi;

			UserName = thumbnail.UserName;
			UserIconUrl = thumbnail.UserIconUrl;
			SubmitDate = thumbnail.PostedAt.LocalDateTime;

			//			UserName = response.UserName;

			Title = thumbnail.Title;
			PlayCount = thumbnail.ViewCount;
			CommentCount = thumbnail.CommentCount;
			MylistCount = thumbnail.MylistCount;


		}

		public override async void OnInitailize()
		{
			// Note: WebViewに渡すHTMLファイルをテンポラリフォルダを経由してアクセスします。
			// WebView.Sourceの仕様上、テンポラリフォルダにサブフォルダを作成し、そのサブフォルダにコンテンツを配置しなければなりません。



			const string VideDescHTMLFolderName = "VideoDesctiptionHTML";
			// ファイルとして動画説明HTMLを書き出す
			var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(VideDescHTMLFolderName, CreationCollisionOption.OpenIfExists);


			string descJoinedHtmlText = "";

			// ファイルのテンプレートになるHTMLテキストを取得して
			var templateHtmlFileStorage = await StorageFile.GetFileFromApplicationUriAsync(
				new Uri("ms-appx:///Assets/VideoDescription/VideoDescription.html")
				);

			// テンプレートHTMLに動画説明を埋め込んだテキストを作成
			using (var stream = await templateHtmlFileStorage.OpenAsync(FileAccessMode.Read))
			using (var textReader = new StreamReader(stream.AsStream()))
			{
				var templateText = textReader.ReadToEnd();
				descJoinedHtmlText = templateText
					.Replace("{Description}", _WatchApiRes.videoDetail.description)
					.Replace("http://", "https://");
			}


			// テンポラリストレージ空間に動画説明HTMLファイルを書き込み
			var filename = _WatchApiRes.videoDetail.id + ".html";
			var savedVideoDescHtmlFile = await tempFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			using (var stream = await savedVideoDescHtmlFile.OpenStreamForWriteAsync())
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(descJoinedHtmlText);
			}

			// 
			VideoDescriptionUri = new Uri($"ms-appdata:///temp/{VideDescHTMLFolderName}/{filename}");
		}

		public override void OnEnter()
		{

		}

		public override void OnLeave()
		{
			
		}

		public override void Dispose()
		{
			
		}

		private DelegateCommand<Uri> _ScriptNotifyCommand;
		public DelegateCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand  
					?? (_ScriptNotifyCommand = new DelegateCommand<Uri>((parameter) =>
				{
					System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

					var path = parameter.AbsoluteUri;
					// is mylist url?
					if (path.StartsWith("https://www.nicovideo.jp/mylist/"))
					{
						var mylistId = parameter.AbsolutePath.Split('/').Last();
						System.Diagnostics.Debug.WriteLine($"open Mylist: {mylistId}");
					}

					// is nico video url?
					if (path.StartsWith("https://www.nicovideo.jp/watch/"))
					{
						var videoId = parameter.AbsolutePath.Split('/').Last();
						System.Diagnostics.Debug.WriteLine($"open Video: {videoId}");
					}

				}));
			}
		}
		


		public string Title { get; private set; }
		

		public string UserName { get; private set; }
		public Uri UserIconUrl { get; private set; }

		public DateTime SubmitDate { get; private set; }


		public uint PlayCount { get; private set; }

		public uint CommentCount { get; private set; }

		public uint MylistCount { get; private set; }


		private Uri _VideoDesctiptionUri;
		public Uri VideoDescriptionUri
		{
			get { return _VideoDesctiptionUri; }
			set { SetProperty(ref _VideoDesctiptionUri, value); }
		}

		WatchApiResponse _WatchApiRes;
		ThumbnailResponse _ThumbnailResponse;
	}

	public class CommentMediaInfoViewModel : MediaInfoViewModel
	{
		public CommentMediaInfoViewModel(ObservableCollection<Comment> comments)
		{
			Comments = comments;
		}

		public override void OnInitailize()
		{
		}

		public override void OnEnter()
		{

		}

		public override void OnLeave()
		{

		}

		public override void Dispose()
		{

		}

		public ObservableCollection<Comment> Comments { get; private set; }
	}


	public class PlaylistMediaInfoViewModel : MediaInfoViewModel
	{
		public PlaylistMediaInfoViewModel()
		{

		}

		public override void OnInitailize()
		{
		}

		public override void OnEnter()
		{

		}

		public override void OnLeave()
		{

		}

		public override void Dispose()
		{

		}

	}

	public class RelatedVideoMediaInfoViewModel : MediaInfoViewModel
	{
		public RelatedVideoMediaInfoViewModel(string videoId)
		{

		}

		public override void OnInitailize()
		{
		}

		public override void OnEnter()
		{

		}

		public override void OnLeave()
		{

		}

		public override void Dispose()
		{

		}

	}

	public class IchibaMediaInfoViewModel : MediaInfoViewModel
	{
		public IchibaMediaInfoViewModel(string videoId)
		{

		}

		public override void OnInitailize()
		{
		}

		public override void OnEnter()
		{

		}

		public override void OnLeave()
		{

		}

		public override void Dispose()
		{

		}

	}





}
