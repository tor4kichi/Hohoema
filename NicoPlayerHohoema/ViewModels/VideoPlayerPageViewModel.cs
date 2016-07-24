using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Flv;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.ViewModels.VideoInfoContent;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Media.Playback;
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
	public class VideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		const float fontSize_mid = 1.0f;
		const float fontSize_small = 0.75f;
		const float fontSize_big = 1.25f;

		const float default_fontSize = fontSize_mid;
		const uint default_DisplayTime = 300; // 3秒

		static readonly Color defaultColor = ColorExtention.HexStringToColor("FFFFFF");

		

		public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler;

		public VideoPlayerPageViewModel(HohoemaApp hohoemaApp, EventAggregator ea, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);

			_SidePaneContentCache = new Dictionary<MediaInfoDisplayType, MediaInfoViewModel>();


			VideoStream = new ReactiveProperty<IRandomAccessStream>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			CurrentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			ReadVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			CommentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			CommentData = new ReactiveProperty<CommentResponse>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			NowSubmittingComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			SliderVideoPosition = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0)
				.AddTo(_CompositeDisposable);
			VideoLength = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0)
				.AddTo(_CompositeDisposable);
			CurrentState = new ReactiveProperty<MediaElementState>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			NowBuffering = CurrentState.Select(x => x == MediaElementState.Buffering || x == MediaElementState.Opening)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			NowQualityChanging = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
			Comments = new ObservableCollection<Comment>();
			NowCommentWriting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			NowSoundChanging = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			IsVisibleComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true)
				.AddTo(_CompositeDisposable);
			IsEnableRepeat = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			IsMuted = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			SoundVolume = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
				.ObserveProperty(x => x.DefaultCommentFontScale)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);


			Title = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);
			WritingComment = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand = WritingComment.Select(x => !string.IsNullOrWhiteSpace(x))
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand.Subscribe(async x => await SubmitComment())
				.AddTo(_CompositeDisposable);

			NowCommentWriting.Subscribe(x => Debug.WriteLine("NowCommentWriting:" + NowCommentWriting.Value))
				.AddTo(_CompositeDisposable);

			IsPauseWithCommentWriting = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			CanResumeOnExitWritingComment = new ReactiveProperty<bool>();
			NowCommentWriting
				.Where(x => x)
				.Subscribe(x => 
			{
				// TODO: ウィンドウの表示状態が最小化の時にも再開できないようにしたい
				CanResumeOnExitWritingComment.Value = CurrentState.Value == MediaElementState.Playing
					&& IsPauseWithCommentWriting.Value;
			})
			.AddTo(_CompositeDisposable);

			CommandString = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			CommandEditerVM = new CommentCommandEditerViewModel(isAnonymousDefault: HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous)
				.AddTo(_CompositeDisposable);

			CommandEditerVM.OnCommandChanged += () => UpdateCommandString();




			

			NowBuffering = 
					Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100), PlayerWindowUIDispatcherScheduler)
						.Where(x => CurrentState.Value != MediaElementState.Paused)
						.Select(x =>
						{
							if (ReadVideoPosition.Value == _PreviosPlayingVideoPosition)
							{
								return true;
							}
							else
							{
								_PreviosPlayingVideoPosition = ReadVideoPosition.Value;
								return false;
							}
						}
						)
						.ObserveOnUIDispatcher()
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			NowBuffering
				.Subscribe(x => Debug.WriteLine(x ? "Buffering..." : "Playing..."))
				.AddTo(_CompositeDisposable);



			_VideoUpdaterSubject = new BehaviorSubject<object>(null)
				.AddTo(_CompositeDisposable);
			CurrentVideoQuality = new ReactiveProperty<NicoVideoQuality>(PlayerWindowUIDispatcherScheduler, NicoVideoQuality.Low, ReactivePropertyMode.None)
				.AddTo(_CompositeDisposable);
			CanToggleCurrentQualityCacheState = CurrentVideoQuality
				.SubscribeOnUIDispatcher()
				.Select(x =>
				{
					if (this.Video == null || IsDisposed) { return false; }

					switch (CurrentVideoQuality.Value)
					{
						case NicoVideoQuality.Original:
							return Video.OriginalQualityCacheState == NicoVideoCacheState.Incomplete ? Video.CanRequestDownloadOriginalQuality : false;
						case NicoVideoQuality.Low:
							return Video.LowQualityCacheState == NicoVideoCacheState.Incomplete ? Video.CanRequestDownloadLowQuality : false;
						default:
							return false;
					}
				})
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);

			Observable.Merge(
				_VideoUpdaterSubject
				)
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ => 
			{
				if (Video == null || IsDisposed) { IsSaveRequestedCurrentQualityCache.Value = false; return; }

				NowQualityChanging.Value = true;

				var x = CurrentVideoQuality.Value;
				PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

				var stream = await Video.GetVideoStream(x);

				if (IsDisposed)
				{
					await Video?.StopPlay();
					return;
				}

				VideoStream.Value = stream;

				switch (x)
				{
					case NicoVideoQuality.Original:
						IsSaveRequestedCurrentQualityCache.Value = !Video.CanRequestDownloadOriginalQuality;
						break;
					case NicoVideoQuality.Low:
						IsSaveRequestedCurrentQualityCache.Value = !Video.CanRequestDownloadLowQuality;
						break;
					default:
						IsSaveRequestedCurrentQualityCache.Value = false;
						break;
				}

			})
			.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache
				.Where(x => !IsDisposed)
				.SubscribeOnUIDispatcher()
				.Subscribe(async saveRequested => 
			{
				if (saveRequested)
				{
					await Video.RequestCache(this.CurrentVideoQuality.Value);
				}
				else
				{
					await Video.CancelCacheRequest(this.CurrentVideoQuality.Value);
				}

				CanToggleCurrentQualityCacheState.ForceNotify();
			})
			.AddTo(_CompositeDisposable);



			TogglePlayQualityCommand =
				Observable.Merge(
					this.ObserveProperty(x => x.Video).ToUnit(),
					CurrentVideoQuality.ToUnit()
					)
				.Where(x => !IsDisposed)
				.Select(_ =>
				{
					if (Video == null) { return false; }
					// 低画質動画が存在しない場合は画質の変更はできない
					if (this.Video.LowQualityVideoSize == 0) { return false; }

					if (CurrentVideoQuality.Value == NicoVideoQuality.Original)
					{
						return Video.CanPlayLowQuality;
					}
					else
					{
						return Video.CanPlayOriginalQuality;
					}
				})
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			TogglePlayQualityCommand
				.Where(x => !IsDisposed)
				.SubscribeOnUIDispatcher()
				.Subscribe(_ => 
				{

					if (CurrentVideoQuality.Value == NicoVideoQuality.Low)
					{
						CurrentVideoQuality.Value = NicoVideoQuality.Original;
					}
					else
					{
						CurrentVideoQuality.Value = NicoVideoQuality.Low;
					}

					_VideoUpdaterSubject.OnNext(null);
				})
				.AddTo(_CompositeDisposable);


			ToggleQualityText = CurrentVideoQuality
				.Select(x => x == NicoVideoQuality.Low ? "低画質に切り替え" : "通常画質に切り替え")
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);



			Observable.Merge(
				IsMuted.ToUnit(),
				SoundVolume.ToUnit()
				)
				.Throttle(TimeSpan.FromSeconds(5))
				.Where(x => !IsDisposed)
				.Subscribe(_ => 
				{
					HohoemaApp.UserSettings.PlayerSettings.Save().ConfigureAwait(false);
				})
				.AddTo(_CompositeDisposable);

			this.ObserveProperty(x => x.Video)
				.Where(x => !IsDisposed)
				.Subscribe(async x =>
				{
					if (x != null)
					{
						var comment = await GetComment();
						if (IsDisposed) { return; }
						CommentData.Value = comment;
						VideoLength.Value = x.CachedWatchApiResponse.Length.TotalSeconds;
						CurrentVideoPosition.Value = TimeSpan.Zero;
					}
				})
				.AddTo(_CompositeDisposable);


			CommentData.Subscribe(x => 
			{
				Comments.Clear();

				if (x == null)
				{
					return;
				}

				var list = x.Chat
					.Where(y => y != null)
					.Select(ChatToComment)
					.Where(y => y != null)
					.OrderBy(y => y.VideoPosition);

				foreach (var comment in list)
				{
					Comments.Add(comment);
				}

				UpdateCommentNGStatus();

				System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
			})
			.AddTo(_CompositeDisposable);


			SliderVideoPosition.Subscribe(x =>
			{
				_NowControlSlider = true;
				if (x > VideoLength.Value)
				{
					x = VideoLength.Value;
				}

				if (!_NowReadingVideoPosition)
				{
					CurrentVideoPosition.Value = TimeSpan.FromSeconds(x);
				}

				_NowControlSlider = false;
			})
			.AddTo(_CompositeDisposable);

			ReadVideoPosition.Subscribe(x =>
			{
				if (_NowControlSlider) { return; }

				_NowReadingVideoPosition = true;

				SliderVideoPosition.Value = x.TotalSeconds;
				CommentVideoPosition.Value = x;

				_NowReadingVideoPosition = false;
			})
			.AddTo(_CompositeDisposable);

			NowPlaying = CurrentState
				.Select(x =>
				{
					return
						x == MediaElementState.Opening ||
						x == MediaElementState.Buffering ||
						x == MediaElementState.Playing;
				})
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			CurrentState.Subscribe(x => 
			{
				if (x == MediaElementState.Opening)
				{
				}
				else if (x == MediaElementState.Playing && NowQualityChanging.Value)
				{
					NowQualityChanging.Value = false;
					SliderVideoPosition.Value = PreviousVideoPosition;
//							CurrentVideoPosition.Value = TimeSpan.FromSeconds(PreviousVideoPosition);
				}
				else if (x == MediaElementState.Closed)
				{
					if (VideoStream.Value != null)
					{
						VideoStream.ForceNotify();
					}
				}


				SetKeepDisplayWithCurrentState();

				Debug.WriteLine("player state :" + x.ToString());
			})
			.AddTo(_CompositeDisposable);


			IsAutoHideEnable =
				Observable.CombineLatest(
					NowPlaying,
					NowSoundChanging.Select(x => !x),
					NowCommentWriting.Select(x => !x)
					)
				.Select(x => x.All(y => y))
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);



			SelectedSidePaneType = new ReactiveProperty<MediaInfoDisplayType>(MediaInfoDisplayType.Summary, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);

			Types = new List<MediaInfoDisplayType>()
			{
				MediaInfoDisplayType.Summary,
				MediaInfoDisplayType.Comment,
				MediaInfoDisplayType.Settings,
			};

			SidePaneContent = SelectedSidePaneType
				.SelectMany(x => GetMediaInfoVM(x))
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			RequestFPS = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
				.Select(x => (int)x)
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);


			HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.IsKeepDisplayInPlayback)
				.Subscribe(isKeepDisplay =>
				{
					SetKeepDisplayWithCurrentState();
				})
				.AddTo(_CompositeDisposable);


		}


		private void SetKeepDisplayWithCurrentState()
		{
			var x = CurrentState.Value;
			if (x == MediaElementState.Paused || x == MediaElementState.Stopped || x == MediaElementState.Closed)
			{
				ExitKeepDisplay();
			}
			else
			{
				SetKeepDisplayIfEnable();
			}
		}

		bool _NowControlSlider = false;
		bool _NowReadingVideoPosition = false;


		private double PreviousVideoPosition;


		static readonly ReadOnlyCollection<char> glassChars = new ReadOnlyCollection<char>(new char[] { 'w', 'ｗ', 'W', 'Ｗ' });		

		private Comment ChatToComment(Chat comment)
		{
			
			if (comment.Text == null)
			{
				return null;
			}

			var playerSettings = HohoemaApp.UserSettings.PlayerSettings;

			var decodedText = comment.GetDecodedText();

			// 自動芝刈り機
			if (playerSettings.CommentGlassMowerEnable)
			{
				foreach (var someGlassChar in glassChars)
				{
					if (decodedText.Last() == someGlassChar)
					{
						decodedText = new String(decodedText.Reverse().SkipWhile(x => x == someGlassChar).Reverse().ToArray()) + someGlassChar;
						break;
					}
				}
			}

			
			var vpos_value = int.Parse(comment.Vpos);
			var vpos = vpos_value >= 0 ? (uint)vpos_value : 0;



			var commentVM = new Comment(this)
			{
				CommentText = decodedText,
				CommentId = comment.GetCommentNo(),
				FontScale = default_fontSize,
				Color = defaultColor,
				VideoPosition = vpos,
				EndPosition = vpos + default_DisplayTime,
			};


			commentVM.IsOwnerComment = comment.User_id != null ? comment.User_id == Video.CachedThumbnailInfo.UserId.ToString() : false;

			IEnumerable<CommandType> commandList = null;

			// コメントの装飾許可設定に従ってコメントコマンドの取得を行う
			var isAllowOwnerCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Owner) == CommentCommandPermissionType.Owner;
			var isAllowUserCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.User) == CommentCommandPermissionType.User;
			var isAllowAnonymousCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Anonymous) == CommentCommandPermissionType.Anonymous;
			if ((commentVM.IsOwnerComment && isAllowOwnerCommentCommnad)
				|| (comment.User_id != null && isAllowUserCommentCommnad)
				|| (comment.User_id == null && isAllowAnonymousCommentCommnad)
				)
			{
				try
				{
					commandList = comment.GetCommandTypes();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(comment.Mail);
				}
			}
			
			
			return commentVM;
		}
		

		private void CommentDecorateFromCommands(Comment commentVM, IEnumerable<CommandType> commandList)
		{
			if (commandList == null || commandList.Any(x => x == CommandType.all))
			{
				commandList = Enumerable.Empty<CommandType>();
			}

			foreach (var command in commandList)
			{
				switch (command)
				{
					case CommandType.small:
						commentVM.FontScale = fontSize_small;
						break;
					case CommandType.big:
						commentVM.FontScale = fontSize_big;
						break;
					case CommandType.medium:
						commentVM.FontScale = fontSize_mid;
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
						// Note: 事前に判定しているのでここでは評価しない
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



			}

			// TODO: 投稿者のコメント表示時間を伸ばす？（3秒→５秒）
			// usまたはshitaが指定されている場合に限る？

			// 　→　投コメ解説をみやすくしたい

			if (commentVM.IsOwnerComment && commentVM.VAlign.HasValue)
			{
				var displayTime = Math.Max(3.0f, commentVM.CommentText.Count() * 0.3f); // 4文字で1秒？ 年齢層的に読みが遅いかもしれないのでやや長めに
				var displayTimeVideoLength = (uint)(displayTime * 100);
				commentVM.EndPosition = commentVM.VideoPosition + displayTimeVideoLength;
			}


		}

		private void UpdateCommentNGStatus()
		{
			var ngSettings = HohoemaApp.UserSettings.NGSettings;
			foreach (var comment in Comments)
			{
				if (comment.UserId != null)
				{
					var userNg = ngSettings.IsNGCommentUser(comment.UserId);
					if (userNg != null)
					{
						comment.NgResult = userNg;
						continue;
					}
				}

				var keywordNg = ngSettings.IsNGComment(comment.CommentText);
				if (keywordNg != null)
				{
					comment.NgResult = keywordNg;
					continue;
				}

				comment.NgResult = null;
			}
		}

		

		

		protected override async Task OnNavigatedToAsync(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");


			CanDownload = HohoemaApp.UserSettings?.CacheSettings?.IsUserAcceptedCache ?? false;


			NicoVideoQuality? quality = null;
			if (e?.Parameter is string)
			{
				var payload = VideoPlayPayload.FromParameterString(e.Parameter as string);
				VideoId = payload.VideoId;
				quality =  payload.Quality;
			}
			else if(viewModelState.ContainsKey(nameof(VideoId)))
			{
				VideoId = (string)viewModelState[nameof(VideoId)];
			}



			var currentUIDispatcher = Window.Current.Dispatcher;

			try
			{
				var videoInfo = await HohoemaApp.MediaManager.GetNicoVideo(VideoId);

				// 内部状態を更新
				await videoInfo.GetVideoInfo();
				await videoInfo.CheckCacheStatus();

				if (videoInfo.IsBlockedHarmfulVideo)
				{
					// ここに来たナビゲーションを忘れさせる
					PageManager.ForgetLastPage();
					// 有害動画を視聴するか確認するページを表示
					PageManager.OpenPage(HohoemaPageType.ConfirmWatchHurmfulVideo,
						new VideoPlayPayload()
						{
							VideoId = VideoId,
							Quality = quality,
						}
						.ToParameterString()
						);
					return;
				}

				Video = videoInfo;
			}
			catch (Exception exception)
			{
				// 動画情報の取得に失敗
				System.Diagnostics.Debug.Write(exception.Message);
				
			}

			// 動画ページにアクセスできず、キャッシュからも復元できなかった場合
			if (Video.CachedWatchApiResponse == null)
			{
				Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

				PageManager.NavigationService.GoBack();
				
				// TODO: 再生できなかった旨をアプリ内のトースト表示で通知する
				return;
			}


			// ビデオクオリティをトリガーにしてビデオ関連の情報を更新させる
			// CurrentVideoQualityは代入時に常にNotifyが発行される設定になっている

			// 低画質動画が存在しない場合はオリジナル画質を選択
			if (Video.LowQualityVideoSize == 0)
			{
				quality = NicoVideoQuality.Original;
			}
			// エコノミー時間帯でオリジナル画質が未保存の場合
			else if (Video.NowLowQualityOnly && Video.OriginalQualityCacheState != NicoVideoCacheState.Cached)
			{
				quality = NicoVideoQuality.Low;
			}
			else if (!quality.HasValue)
			{
				// 画質指定がない場合、ユーザー設定から低画質がリクエストされてないかチェック
				var defaultLowQuality = HohoemaApp.UserSettings.PlayerSettings.IsLowQualityDeafult;
				quality = defaultLowQuality ? NicoVideoQuality.Low : NicoVideoQuality.Original;
			}

			// CurrentVideoQualityは同一値の代入でもNotifyがトリガーされるようになっている
			CurrentVideoQuality.Value = quality.Value;
			


			if (viewModelState.ContainsKey(nameof(CurrentVideoPosition)))
			{
				CurrentVideoPosition.Value = TimeSpan.FromSeconds((double)viewModelState[nameof(CurrentVideoPosition)]);
			}

			Title.Value = Video.Title;
			_SidePaneContentCache.Clear();

			if (SelectedSidePaneType.Value == MediaInfoDisplayType.Summary)
			{
				SelectedSidePaneType.ForceNotify();
			}
			else
			{
				SelectedSidePaneType.Value = MediaInfoDisplayType.Summary;
			}

			CommandEditerVM.IsPremiumUser = base.HohoemaApp.IsPremiumUser;

			// TODO: チャンネル動画やコミュニティ動画の検知			
			CommandEditerVM.ChangeEnableAnonymity(true);

			UpdateCommandString();

			// PlayerSettings
			var playerSettings = HohoemaApp.UserSettings.PlayerSettings;
			IsVisibleComment.Value = playerSettings.DefaultCommentDisplay;

			_VideoUpdaterSubject.OnNext(null);


			// FavFeedList
			await HohoemaApp.FavFeedManager.MarkAsRead(Video.VideoId);
			await HohoemaApp.FavFeedManager.MarkAsRead(Video.RawVideoId);

			Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");
		}






		protected override Task OnNavigatingFromAsync(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync start.");

			Comments.Clear();


			if (suspending)
			{
				VideoStream.Value = null;

				viewModelState.Add(nameof(VideoId), VideoId);
				viewModelState.Add(nameof(CurrentVideoPosition), CurrentVideoPosition.Value.TotalSeconds);
			}
			else if (VideoStream.Value != null)
			{
				var stream = VideoStream.Value;
				VideoStream.Value = null;


				// Note: VideoStopPlayによってストリームの管理が行われます
				// これは再生後もダウンロードしている場合に対応するためです
				// stream.Dispose();
			}

			Video?.StopPlay();

			_SidePaneContentCache.Clear();

			ExitKeepDisplay();

			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");

			return Task.CompletedTask;
		}

		protected override void OnDispose()
		{
			Debug.WriteLine("VideoPlayer OnDispose start.");

			Video?.StopPlay();

			Debug.WriteLine("VideoPlayer OnDispose done.");
		}



		private async Task<CommentResponse> GetComment()
		{
			if (Video == null) { return null; }

			return await Video.GetComment(true);
			//			return await this._HohoemaApp.NiconicoContext.Video
			//					.GetCommentAsync(response);
		}


		private async Task SubmitComment()
		{
			Debug.WriteLine($"try comment submit:{WritingComment.Value}");

			NowSubmittingComment.Value = true;
			try
			{
				var vpos = (uint)(ReadVideoPosition.Value.TotalMilliseconds / 10);
				var commands = CommandString.Value;
				var res = await Video.SubmitComment(WritingComment.Value, ReadVideoPosition.Value, commands);

				if (res.Chat_result.Status == ChatResult.Success)
				{
					Debug.WriteLine("コメントの投稿に成功: " + res.Chat_result.No);

					var commentVM = new Comment(this)
					{
						CommentId = (uint)res.Chat_result.No,
						VideoPosition = vpos,
						UserId = base.HohoemaApp.LoginUserId.ToString(),
						CommentText = WritingComment.Value,
					};
//					CommentDecorateFromCommands(commentVM, commands);

					Comments.Add(commentVM);

					WritingComment.Value = "";
				}
				else
				{
					Debug.WriteLine("コメントの投稿に失敗: " + res.Chat_result.Status.ToString());
				}

			}
			finally
			{
				NowSubmittingComment.Value = false;
			}
		}


		private void UpdateCommandString()
		{
			var str = CommandEditerVM.MakeCommandsString();
			if (String.IsNullOrEmpty(str))
			{
				CommandString.Value = "コマンド";
			}
			else
			{
				CommandString.Value = str;
			}
		}


		private async Task<MediaInfoViewModel> GetMediaInfoVM(MediaInfoDisplayType type)
		{
			MediaInfoViewModel vm = null;
			if (_SidePaneContentCache.ContainsKey(type))
			{
				vm = _SidePaneContentCache[type];
			}
			else 
			{
				switch (type)
				{
					case MediaInfoDisplayType.Summary:
						var uri = await VideoDescriptionHelper.PartHtmlOutputToCompletlyHtml(VideoId, Video.CachedWatchApiResponse.videoDetail.description);
						if (IsDisposed) { return null; }
						vm = new SummaryVideoInfoContentViewModel(Video.CachedThumbnailInfo, uri, PageManager);
						break;

					case MediaInfoDisplayType.Comment:
						vm = new CommentVideoInfoContentViewModel(HohoemaApp.UserSettings, Comments);
						break;

					case MediaInfoDisplayType.Settings:
						vm = new SettingsVideoInfoContentViewModel(HohoemaApp.UserSettings.PlayerSettings);
						break;
					default:
						throw new NotSupportedException();
				}

				_SidePaneContentCache.Add(type, vm);
			}

			return vm;
		}

		#region Command	

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


		private DelegateCommand _ToggleMuteCommand;
		public DelegateCommand ToggleMuteCommand
		{
			get
			{
				return _ToggleMuteCommand
					?? (_ToggleMuteCommand = new DelegateCommand(() => 
					{
						IsMuted.Value = !IsMuted.Value;
					}));
			}
		}


		private DelegateCommand _VolumeUpCommand;
		public DelegateCommand VolumeUpCommand
		{
			get
			{
				return _VolumeUpCommand
					?? (_VolumeUpCommand = new DelegateCommand(() =>
					{
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
						SoundVolume.Value = Math.Min(1.0, SoundVolume.Value + amount);
					}));
			}
		}

		private DelegateCommand _VolumeDownCommand;
		public DelegateCommand VolumeDownCommand
		{
			get
			{
				return _VolumeDownCommand
					?? (_VolumeDownCommand = new DelegateCommand(() =>
					{
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
						SoundVolume.Value = Math.Max(0.0, SoundVolume.Value - amount);
					}));
			}
		}



		private DelegateCommand _ToggleRepeatCommand;
		public DelegateCommand ToggleRepeatCommand
		{
			get
			{
				return _ToggleRepeatCommand
					?? (_ToggleRepeatCommand = new DelegateCommand(() =>
					{
						IsEnableRepeat.Value = !IsEnableRepeat.Value;
					}));
			}
		}

		public ReactiveCommand CommentSubmitCommand { get; private set; }
		public ReactiveCommand TogglePlayQualityCommand { get; private set; }


		#endregion


		

		#region player settings method
		
		
		void SetKeepDisplayIfEnable()
		{
			ExitKeepDisplay();

			if (HohoemaApp.UserSettings.PlayerSettings.IsKeepDisplayInPlayback)
			{
				DisplayRequestHelper.StartVideoPlayback();
			}
		}

		void ExitKeepDisplay()
		{
			DisplayRequestHelper.StopVideoPlayback();
		}



		



		#endregion

		public ReactiveProperty<CommentResponse> CommentData { get; private set; }

		private NicoVideo _Video;
		public NicoVideo Video
		{
			get { return _Video; }
			set { SetProperty(ref _Video, value); }
		}

		private string _VideoId;
		public string VideoId
		{
			get { return _VideoId; }
			set { SetProperty(ref _VideoId, value); }
		}



		private bool _CanDownload;
		public bool CanDownload
		{
			get { return _CanDownload; }
			set { SetProperty(ref _CanDownload, value); }
		}


		// Note: 新しいReactivePropertyを追加したときの注意点
		// ReactivePorpertyの初期化にPlayerWindowUIDispatcherSchedulerを使うこと


		public ReactiveProperty<string> Title { get; private set; }

		public ReactiveProperty<IRandomAccessStream> VideoStream { get; private set; }

		private ISubject<object> _VideoUpdaterSubject;
		public ReactiveProperty<NicoVideoQuality> CurrentVideoQuality { get; private set; }
		public ReactiveProperty<bool> CanToggleCurrentQualityCacheState { get; private set; }
		public ReactiveProperty<bool> IsSaveRequestedCurrentQualityCache { get; private set; }
		public ReactiveProperty<string> ToggleQualityText { get; private set; }


		public ReactiveProperty<TimeSpan> CurrentVideoPosition { get; private set; }
		public ReactiveProperty<TimeSpan> ReadVideoPosition { get; private set; }
		public ReactiveProperty<TimeSpan> CommentVideoPosition { get; private set; }

		public ReactiveProperty<double> SliderVideoPosition { get; private set; }
		public ReactiveProperty<double> VideoLength { get; private set; }
		public ReactiveProperty<MediaElementState> CurrentState { get; private set; }
		public ReactiveProperty<bool> NowBuffering { get; private set; }
		public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReactiveProperty<bool> NowQualityChanging { get; private set; }
		public ReactiveProperty<bool> IsEnableRepeat { get; private set; }

		public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }

		private TimeSpan _PreviosPlayingVideoPosition;

		// Sound
		public ReactiveProperty<bool> NowSoundChanging { get; private set; }
		public ReactiveProperty<bool> IsMuted { get; private set; }
		public ReactiveProperty<double> SoundVolume { get; private set; }

		// Settings
		public ReactiveProperty<int> RequestFPS { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }



		public ReactiveProperty<bool> NowSubmittingComment { get; private set; }
		public ReactiveProperty<string> WritingComment { get; private set; }
		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
		public ReactiveProperty<bool> NowCommentWriting { get; private set; }
		public ObservableCollection<Comment> Comments { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<bool> CanResumeOnExitWritingComment { get; private set; }

		public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
		public ReactiveProperty<string> CommandString { get; private set; }

		public ReactiveProperty<MediaInfoViewModel> SidePaneContent { get; private set; }
		private Dictionary<MediaInfoDisplayType, MediaInfoViewModel> _SidePaneContentCache;
		public ReactiveProperty<MediaInfoDisplayType> SelectedSidePaneType { get; private set; }
		public List<MediaInfoDisplayType> Types { get; private set; }


		internal async Task AddNgUser(Comment commentViewModel)
		{
			if (commentViewModel.UserId == null) { return; }

			string userName = "";
			try
			{
//				var commentUser = await _HohoemaApp.ContentFinder.GetUserInfo(commentViewModel.UserId);
//				userName = commentUser.Nickname;
			}
			catch { }

		    HohoemaApp.UserSettings.NGSettings.NGCommentUserIds.Add(new UserIdInfo()
			{
				UserId = commentViewModel.UserId,
				Description = userName
			});

			UpdateCommentNGStatus();
			
		}
	}


	


	

	public class TagViewModel
	{
		public TagViewModel(Tag tag)
		{
			_Tag = tag;

			TagText = _Tag.Value;
			IsCategoryTag = _Tag.Category;
			IsLock = _Tag.Lock;
		}

		public string TagText { get; private set; }
		public bool IsCategoryTag { get; private set; }
		public bool IsLock { get; private set; }


		private DelegateCommand _OpenSearchPageWithTagCommand;
		public DelegateCommand OpenSearchPageWithTagCommand
		{
			get
			{
				return _OpenSearchPageWithTagCommand
					?? (_OpenSearchPageWithTagCommand = new DelegateCommand(() => 
					{
						// TODO: 
					}));
			}
		}


		private DelegateCommand _OpenTagDictionaryInBrowserCommand;
		public DelegateCommand OpenTagDictionaryInBrowserCommand
		{
			get
			{
				return _OpenTagDictionaryInBrowserCommand
					?? (_OpenTagDictionaryInBrowserCommand = new DelegateCommand(() =>
					{
						// TODO: 
					}));
			}
		}

		Tag _Tag;
	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Comment,
		Settings,
	}










}
