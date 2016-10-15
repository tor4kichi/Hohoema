using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Flv;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.ViewModels.VideoInfoContent;
using NicoPlayerHohoema.Views;
using NicoPlayerHohoema.Views.DownloadProgress;
using NicoPlayerHohoema.Views.Service;
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
using System.Reactive.Disposables;
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
using FFmpegInterop;
using Windows.Foundation.Collections;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		
		const uint default_DisplayTime = 400; // 1 = 10ms, 400 = 4000ms = 4.0 Seconds



		private SynchronizationContextScheduler _PlayerWindowUIDispatcherScheduler;
		public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler
		{
			get
			{
				return _PlayerWindowUIDispatcherScheduler
					?? (_PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
			}
		}

		public VideoPlayerPageViewModel(
			HohoemaApp hohoemaApp, 
			EventAggregator ea,
			PageManager pageManager, 
			ToastNotificationService toast,
			TextInputDialogService textInputDialog
			)
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			_ToastService = toast;
			_TextInputDialogService = textInputDialog;

			_SidePaneContentCache = new Dictionary<MediaInfoDisplayType, MediaInfoViewModel>();


			VideoStream = new ReactiveProperty<object>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			CurrentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			ReadVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero);
//				.AddTo(_CompositeDisposable);
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

			IsPlayWithCache = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			CanResumeOnExitWritingComment = new ReactiveProperty<bool>();

			NowCommentWriting
				.Where(x => x)
				.Subscribe(x => 
			{
				// TODO: ウィンドウの表示状態が最小化の時にも再開できないようにしたい
				CanResumeOnExitWritingComment.Value = CurrentState.Value == MediaElementState.Playing
					&& (IsPauseWithCommentWriting?.Value ?? true);
			})
			.AddTo(_CompositeDisposable);

			CommandString = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			CommentCanvasHeight = new ReactiveProperty<double>(0);
			CommentCanvasWidth = new ReactiveProperty<double>(0);







			CurrentVideoQuality = new ReactiveProperty<NicoVideoQuality>(PlayerWindowUIDispatcherScheduler, NicoVideoQuality.Low, ReactivePropertyMode.None)
				.AddTo(_CompositeDisposable);
			CanToggleCurrentQualityCacheState = CurrentVideoQuality
				.SubscribeOnUIDispatcher()
				.Select(x =>
				{
					if (this.Video == null || IsDisposed) { return false; }

					switch (x)
					{
						case NicoVideoQuality.Original:
							if (Video.OriginalQuality.IsCacheRequested)
							{
								// DL中、DL済み
								return true;
							}
							else
							{
								return Video.OriginalQuality.CanRequestDownload;
							}
						case NicoVideoQuality.Low:
							if (Video.LowQuality.IsCacheRequested)
							{
								// DL中、DL済み
								return true;
							}
							else
							{
								return Video.LowQuality.CanRequestDownload;
							}
						default:
							throw new NotSupportedException(x.ToString());
					}
				})
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, ReactivePropertyMode.DistinctUntilChanged)
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
					if (this.Video.IsOriginalQualityOnly) { return false; }

					if (CurrentVideoQuality.Value == NicoVideoQuality.Original)
					{
						return Video.LowQuality.CanPlay && !IsNotSupportVideoType;
					}
					else
					{
						return Video.OriginalQuality.CanPlay && !IsNotSupportVideoType;
					}
				})
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			TogglePlayQualityCommand
				.Where(x => !IsDisposed && !IsNotSupportVideoType)
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ => 
				{
					PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

					if (CurrentVideoQuality.Value == NicoVideoQuality.Low)
					{
						CurrentVideoQuality.Value = NicoVideoQuality.Original;
					}
					else
					{
						CurrentVideoQuality.Value = NicoVideoQuality.Low;
					}


					await PlayingQualityChangeAction();
				})
				.AddTo(_CompositeDisposable);


			ToggleQualityText = CurrentVideoQuality
				.Select(x => x == NicoVideoQuality.Low ? "低画質に切り替え" : "通常画質に切り替え")
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);



			

			this.ObserveProperty(x => x.Video)
				.Where(x => !IsDisposed)
				.Subscribe(async x =>
				{
					if (x != null && !IsNotSupportVideoType)
					{
						var comment = await GetComment();
						if (IsDisposed) { return; }
						CommentData.Value = comment;
						VideoLength.Value = x.VideoLength.TotalSeconds;
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

			CurrentState.Subscribe(async x => 
			{
				if (x == MediaElementState.Opening)
				{
				}
				else if (x == MediaElementState.Playing && NowQualityChanging.Value)
				{
					NowQualityChanging.Value = false;
//					SliderVideoPosition.Value = PreviousVideoPosition;
					CurrentVideoPosition.Value = TimeSpan.FromSeconds(PreviousVideoPosition);
				}
				else if (x == MediaElementState.Closed)
				{
					if (VideoStream.Value != null)
					{
						await Video.StopPlay();

						// TODO: ユーザー手動の再読み込みに変更する
						await Task.Delay(500);

						await this.PlayingQualityChangeAction();

						Debug.WriteLine("再生中に動画がClosedになったため、強制的に再初期化を実行しました。これは非常措置です。");
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



			SelectedSidePaneType = new ReactiveProperty<MediaInfoDisplayType>(PlayerWindowUIDispatcherScheduler, MediaInfoDisplayType.Summary, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);

			Types = new List<MediaInfoDisplayType>()
			{
				MediaInfoDisplayType.Summary,
				MediaInfoDisplayType.Mylist,
//				MediaInfoDisplayType.Comment,
				MediaInfoDisplayType.Shere,
				MediaInfoDisplayType.Settings,
			};

			SidePaneContent = SelectedSidePaneType
				.SelectMany(x => GetMediaInfoVM(x))
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			DownloadCompleted = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
			ProgressPercent = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0);
			IsFullScreen = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
			IsFullScreen
				.Subscribe(isFullScreen => 
			{
				var appView = ApplicationView.GetForCurrentView();
				if (isFullScreen)
				{
					if (!appView.TryEnterFullScreenMode())
					{
						IsFullScreen.Value = false;
					}
				}
				else
				{
					appView.ExitFullScreenMode();
				}
			})
			.AddTo(_CompositeDisposable);

			ProgressFragments = new ObservableCollection<ProgressFragment>();

		}



		protected override void OnSignIn(ICollection<IDisposable> userSessionDisposer)
		{
			IsPauseWithCommentWriting = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(IsPauseWithCommentWriting));

			IsMuted = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(IsMuted));

			SoundVolume = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(SoundVolume));

			CommentDefaultColor = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.CommentColor, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(CommentDefaultColor));


			Observable.Merge(
				IsMuted.ToUnit(),
				SoundVolume.ToUnit(),
				CommentDefaultColor.ToUnit()
				)
				.Throttle(TimeSpan.FromSeconds(5))
				.Where(x => !IsDisposed)
				.Subscribe(_ =>
				{
					HohoemaApp.UserSettings.PlayerSettings.Save().ConfigureAwait(false);
				})
				.AddTo(userSessionDisposer);



			
			RequestFPS = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
				.Select(x => (int)x)
				.ToReactiveProperty()
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(RequestFPS));

			RequestCommentDisplayDuration = HohoemaApp.UserSettings.PlayerSettings
				.ObserveProperty(x => x.CommentDisplayDuration)
				.Select(x => x.TotalSeconds)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(RequestCommentDisplayDuration));

			CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
				.ObserveProperty(x => x.DefaultCommentFontScale)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(CommentFontScale));


			HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.IsKeepDisplayInPlayback)
				.Subscribe(isKeepDisplay =>
				{
					SetKeepDisplayWithCurrentState();
				})
				.AddTo(userSessionDisposer);


			CommandEditerVM = new CommentCommandEditerViewModel(isAnonymousDefault: HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(CommandEditerVM));

			CommandEditerVM.OnCommandChanged += () => UpdateCommandString();


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

		private CompositeDisposable _BufferingMonitorDisposable;



		/// <summary>
		/// 再生処理
		/// </summary>
		/// <returns></returns>
		private async Task PlayingQualityChangeAction()
		{
			if (Video == null || IsDisposed) { IsSaveRequestedCurrentQualityCache.Value = false; return; }


			NowQualityChanging.Value = true;

			var x = CurrentVideoQuality.Value;

			if (PreviousVideoPosition == 0.0)
			{
				PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;
			}

			// キャッシュサポートされたメディアの再生
			if (Video.CanGetVideoStream())
			{
				var stream = await Video.GetVideoStream(x);

				if (stream == null)
				{
					return;
				}

				if (IsDisposed)
				{
					if (Video != null)
					{
						await Video.StopPlay();
					}
					return;
				}

				if (Video.ContentType == MovieType.Mp4)
				{
					VideoStream.Value = stream;
				}
				else
				{
					var mss = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);

					if (mss != null)
					{
						VideoStream.Value = mss;
					}
					else
					{
						throw new NotSupportedException();
					}
				}

				switch (x)
				{
					case NicoVideoQuality.Original:
						IsSaveRequestedCurrentQualityCache.Value = Video.OriginalQuality.IsCacheRequested;
						break;
					case NicoVideoQuality.Low:
						IsSaveRequestedCurrentQualityCache.Value = Video.LowQuality.IsCacheRequested;
						break;
					default:
						IsSaveRequestedCurrentQualityCache.Value = false;
						break;
				}

				if (stream is NicoVideoCachedStream)
				{
					// キャッシュ機能経由の再生
					var cachedStream = stream as NicoVideoCachedStream;
					cachedStream.Downloader.OnCacheProgress += Downloader_OnCacheProgress;
					_TempProgress = cachedStream.Downloader.DownloadProgress.Clone();

					ProgressFragments.Clear();
					var invertedTotalSize = 1.0 / (x == NicoVideoQuality.Original ? Video.OriginalQuality.VideoSize : Video.LowQuality.VideoSize);
					foreach (var cachedRange in _TempProgress.CachedRanges.ToArray())
					{
						ProgressFragments.Add(new ProgressFragment(invertedTotalSize, cachedRange.Key, cachedRange.Value));
					}

					IsPlayWithCache.Value = true;
				}
				else
				{
					// 完全なオンライン再生
					IsPlayWithCache.Value = false;
				}
			}
			else
			{
				// キャッシュがサポートされていない
				throw new Exception();
			}


		}

		private void InitializeBufferingMonitor()
		{
			_BufferingMonitorDisposable?.Dispose();
			_BufferingMonitorDisposable = new CompositeDisposable();

			NowBuffering =
				Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100), PlayerWindowUIDispatcherScheduler)
					.Select(x =>
					{
						if (DownloadCompleted.Value) { return false; }

						if (CurrentState.Value == MediaElementState.Paused)
						{
							return false;
						}

						if (CurrentState.Value == MediaElementState.Buffering 
						|| CurrentState.Value == MediaElementState.Opening)
						{
							return true;
						}

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
				.AddTo(_BufferingMonitorDisposable);

			OnPropertyChanged(nameof(NowBuffering));
#if DEBUG
			NowBuffering
				.Subscribe(x => Debug.WriteLine(x ? "Buffering..." : "Playing..."))
				.AddTo(_BufferingMonitorDisposable);
#endif
			Video.OriginalQuality.ObserveProperty(x => x.CacheProgressSize)
				.Where(_ => CurrentVideoQuality.Value == NicoVideoQuality.Original)
				.Subscribe(originalProgress => 
				{
					UpdadeProgress(Video.OriginalQuality.VideoSize, originalProgress);
				})
				.AddTo(_BufferingMonitorDisposable);

			Video.LowQuality.ObserveProperty(x => x.CacheProgressSize)
				.Where(_ => CurrentVideoQuality.Value == NicoVideoQuality.Low)
				.Subscribe(lowProgress =>
				{
					UpdadeProgress(Video.LowQuality.VideoSize, lowProgress);

					
				})
				.AddTo(_BufferingMonitorDisposable);
		}


		private void UpdadeProgress(float videoSize, float progressSize)
		{
			//ProgressFragments

			DownloadCompleted.Value = progressSize == videoSize;
			if (DownloadCompleted.Value)
			{
				ProgressPercent.Value = 100;
			}
			else
			{
				ProgressPercent.Value = Math.Round((double)progressSize / videoSize * 100, 1);
			}

		}

		private void Downloader_OnCacheProgress(string arg1, NicoVideoQuality quality, uint head, uint length)
		{
			
			// TODO: 
			
			var oldCount = _TempProgress.CachedRanges.Count;
			_TempProgress.Update(head, length);

		

			if (oldCount != _TempProgress.CachedRanges.Count)
			{
				// 追加されている場合
				foreach (var cachedRange in _TempProgress.CachedRanges)
				{
					if (!ProgressFragments.Any(x => x.Start == cachedRange.Key))
					{
						var invertedTotalSize = 1.0 / (quality == NicoVideoQuality.Original ? Video.OriginalQuality.VideoSize : Video.LowQuality.VideoSize);
						ProgressFragments.Add(new ProgressFragment(invertedTotalSize, cachedRange.Key, cachedRange.Value));
					}
				}

				// 削除されている場合
				var removeFragments = ProgressFragments.Where(x => _TempProgress.CachedRanges.All(y => x.Start != y.Key))
					.ToArray();

				foreach (var removeFrag in removeFragments)
				{
					ProgressFragments.Remove(removeFrag);
				}
			}
			else
			{
				// 内部の更新だけ
				foreach (var cachedRange in _TempProgress.CachedRanges)
				{
					var start = cachedRange.Key;
					var end = cachedRange.Value;

					if (start < head && head < end)
					{
						var fragment = ProgressFragments.SingleOrDefault(x => x.Start == start);
						if (fragment != null)
						{
							fragment.End = end;
						}
						break;
					}
				}
			}

			
		}







		static readonly ReadOnlyCollection<char> glassChars =
			new ReadOnlyCollection<char>(new char[] { 'w', 'ｗ', 'W', 'Ｗ' });		

		private Comment ChatToComment(Chat comment)
		{
			
			if (comment.Text == null)
			{
				return null;
			}

			var playerSettings = HohoemaApp.UserSettings.PlayerSettings;

			string commentText = "";
			try
			{
				commentText = comment.GetDecodedText();
			}
			catch
			{
				commentText = comment.Text;
			}

			// 自動芝刈り機
			if (playerSettings.CommentGlassMowerEnable)
			{
				foreach (var someGlassChar in glassChars)
				{
					if (commentText.Last() == someGlassChar)
					{
						commentText = new String(commentText.Reverse().SkipWhile(x => x == someGlassChar).Reverse().ToArray()) + someGlassChar;
						break;
					}
				}
			}

			
			var vpos_value = int.Parse(comment.Vpos);
			var vpos = vpos_value >= 0 ? (uint)vpos_value : 0;



			var commentVM = new Comment(this)
			{
				CommentText = commentText,
				CommentId = comment.GetCommentNo(),
				VideoPosition = vpos,
				EndPosition = vpos + default_DisplayTime,
			};


			commentVM.IsOwnerComment = comment.User_id != null ? comment.User_id == Video.VideoOwnerId.ToString() : false;

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
					commentVM.ApplyCommands(commandList);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(comment.Mail);
				}
			}
			
			
			return commentVM;
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

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");

			NicoVideoQuality? quality = null;
			if (e?.Parameter is string)
			{
				var payload = VideoPlayPayload.FromParameterString(e.Parameter as string);
				VideoId = payload.VideoId;
				quality = payload.Quality;
			}
			else if (viewModelState.ContainsKey(nameof(VideoId)))
			{
				VideoId = (string)viewModelState[nameof(VideoId)];
			}

			cancelToken.ThrowIfCancellationRequested();

			var currentUIDispatcher = Window.Current.Dispatcher;

			try
			{
				var videoInfo = await HohoemaApp.MediaManager.GetNicoVideoAsync(VideoId);

				// 内部状態を更新
				await videoInfo.VisitWatchPage();
				await videoInfo.CheckCacheStatus();

				// 動画が削除されていた場合
				if (videoInfo.IsDeleted)
				{
					Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

					var dispatcher = Window.Current.CoreWindow.Dispatcher;

					await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
					{
						await Task.Delay(100);
						PageManager.NavigationService.GoBack();

						string toastContent = "";
						if (!String.IsNullOrEmpty(videoInfo.Title))
						{
							toastContent = $"\"{videoInfo.Title}\" は削除された動画です";
						}
						else
						{
							toastContent = $"削除された動画です";
						}
						_ToastService.ShowText($"動画 {VideoId} は再生できません", toastContent);
					})
					.AsTask()
					.ConfigureAwait(false);

					return;
				}

				cancelToken.ThrowIfCancellationRequested();

				// 有害動画へのアクセスに対して意思確認された場合
				if (videoInfo.IsBlockedHarmfulVideo)
				{
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

				cancelToken.ThrowIfCancellationRequested();


				Title.Value = videoInfo.Title;


				// ビデオタイプとプロトコルタイプをチェックする
				
				if (videoInfo.ProtocolType != MediaProtocolType.RTSPoverHTTP)
				{
					// サポートしていないプロトコルです
					IsNotSupportVideoType = true;
					CannotPlayReason = videoInfo.ProtocolType.ToString() + " はHohoemaでサポートされないデータ通信形式です";
				}
//				else if (videoInfo.ContentType != MovieType.Mp4)
//				{
					// サポートしていない動画タイプです
//					IsNotSupportVideoType = true;
//					CannotPlayReason = videoInfo.ContentType.ToString() + " はHohoemaでサポートされない動画形式です";
//				}
				else
				{
					IsNotSupportVideoType = false;
					CannotPlayReason = "";
				}
				

				Video = videoInfo;
			}
			catch (Exception exception)
			{
				// 動画情報の取得に失敗
				System.Diagnostics.Debug.Write(exception.Message);

			}

			cancelToken.ThrowIfCancellationRequested();


			// 全画面表示の設定を反映
			if (HohoemaApp.UserSettings.PlayerSettings.IsFullScreenDefault)
			{
				IsFullScreen.Value = true;
			}


			cancelToken.ThrowIfCancellationRequested();

			if (IsNotSupportVideoType)
			{
				// コメント入力不可
				NowSubmittingComment.Value = true;
			}
			else
			{
				// ビデオクオリティをトリガーにしてビデオ関連の情報を更新させる
				// CurrentVideoQualityは代入時に常にNotifyが発行される設定になっている

				NicoVideoQuality realQuality = NicoVideoQuality.Low;
				if ((quality == null || quality == NicoVideoQuality.Original)
					&& Video.OriginalQuality.IsCached)
				{
					realQuality = NicoVideoQuality.Original;
				}
				else if ((quality == null || quality == NicoVideoQuality.Low)
					&& Video.LowQuality.IsCached)
				{
					realQuality = NicoVideoQuality.Low;
				}
				// 低画質動画が存在しない場合はオリジナル画質を選択
				else if (Video.IsOriginalQualityOnly)
				{
					realQuality = NicoVideoQuality.Original;
				}
				// エコノミー時間帯でオリジナル画質が未保存の場合
				else if (!HohoemaApp.IsPremiumUser
					&& Video.NowLowQualityOnly
					&& !Video.OriginalQuality.IsCached)
				{
					realQuality = NicoVideoQuality.Low;
				}
				else if (!quality.HasValue)
				{
					// 画質指定がない場合、ユーザー設定から低画質がリクエストされてないかチェック
					var defaultLowQuality = HohoemaApp.UserSettings.PlayerSettings.IsLowQualityDeafult;
					realQuality = defaultLowQuality ? NicoVideoQuality.Low : NicoVideoQuality.Original;
				}

				// CurrentVideoQualityは同一値の代入でもNotifyがトリガーされるようになっている
				CurrentVideoQuality.Value = realQuality;

				cancelToken.ThrowIfCancellationRequested();


				if (viewModelState.ContainsKey(nameof(CurrentVideoPosition)))
				{
					CurrentVideoPosition.Value = TimeSpan.FromSeconds((double)viewModelState[nameof(CurrentVideoPosition)]);
				}

				

				CommandEditerVM.IsPremiumUser = base.HohoemaApp.IsPremiumUser;

				// TODO: チャンネル動画やコミュニティ動画の検知			
				CommandEditerVM.ChangeEnableAnonymity(true);

				UpdateCommandString();


				cancelToken.ThrowIfCancellationRequested();


				// PlayerSettings
				var playerSettings = HohoemaApp.UserSettings.PlayerSettings;
				IsVisibleComment.Value = playerSettings.DefaultCommentDisplay;



				cancelToken.ThrowIfCancellationRequested();

				// お気に入りフィード上の動画を既読としてマーク
				await HohoemaApp.FeedManager.MarkAsRead(Video.VideoId);
				await HohoemaApp.FeedManager.MarkAsRead(Video.RawVideoId);

				cancelToken.ThrowIfCancellationRequested();

				// バッファリング状態のモニターが使うタイマーだけはページ稼働中のみ動くようにする
				InitializeBufferingMonitor();

				// 再生ストリームの準備を開始する
				await PlayingQualityChangeAction();

				cancelToken.ThrowIfCancellationRequested();

				// Note: 0.4.1現在ではキャッシュはmp4のみ対応
				var isCanCache = Video.ContentType == MovieType.Mp4;
				CanDownload = (HohoemaApp.UserSettings?.CacheSettings?.IsUserAcceptedCache ?? false) && isCanCache;

				// 再生履歴に反映
				//VideoPlayHistoryDb.VideoPlayed(Video.RawVideoId);
			}

			_SidePaneContentCache.Clear();

			_VideoDescriptionHtmlUri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(VideoId, Video.DescriptionWithHtml);

			if (SelectedSidePaneType.Value == MediaInfoDisplayType.Summary)
			{
				SelectedSidePaneType.ForceNotify();
			}
			else
			{
				SelectedSidePaneType.Value = MediaInfoDisplayType.Summary;
			}

			cancelToken.ThrowIfCancellationRequested();


			Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

		}

		protected override async Task OnResumed()
		{
			if (NowSignIn)
			{
				InitializeBufferingMonitor();

				await PlayingQualityChangeAction();
			}
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync start.");

			PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

			IsFullScreen.Value = false;

			if (suspending)
			{
				VideoStream.Value = null;

				// 再生中動画のキャッシュがサスペンドから復帰後にも利用できるように
				// 削除を抑制するように要請する
				HohoemaApp.MediaManager.Context.OncePreventDeleteCacheOnPlayingVideo(Video.RawVideoId);

				viewModelState[nameof(VideoId)] = VideoId;
				viewModelState[nameof(CurrentVideoPosition)] = CurrentVideoPosition.Value.TotalSeconds;
			}
			else 
			{
				var stream = VideoStream.Value;
				VideoStream.Value = null;


				// Note: VideoStopPlayによってストリームの管理が行われます
				// これは再生後もダウンロードしている場合に対応するためです
				// stream.Dispose();
				if (Video != null)
				{
					Video.StopPlay().ConfigureAwait(false);
				}
			}

			_SidePaneContentCache.Clear();

			ExitKeepDisplay();

			Comments.Clear();

			

			_BufferingMonitorDisposable?.Dispose();
			_BufferingMonitorDisposable = new CompositeDisposable();

			base.OnNavigatingFrom(e, viewModelState, suspending);


			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");
		}




		protected override void OnDispose()
		{
			base.OnDispose();

			if (Video != null)
			{
				VideoStream.Value = null;

				Video.StopPlay().ConfigureAwait(false);
			}

			_BufferingMonitorDisposable?.Dispose();
		}

		private async Task<CommentResponse> GetComment()
		{
			if (Video == null) { return null; }

			return await Video.GetCommentResponse(true);
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
					_ToastService.ShowText("コメント投稿完了", $"{VideoId}に「{WritingComment.Value}」をコメント投稿しました");

					Debug.WriteLine("コメントの投稿に成功: " + res.Chat_result.No);

					var commentVM = new Comment(this)
					{
						CommentId = (uint)res.Chat_result.No,
						VideoPosition = vpos,
						EndPosition = vpos + default_DisplayTime,
						UserId = base.HohoemaApp.LoginUserId.ToString(),
						CommentText = WritingComment.Value,
					};

					var commentCommands = CommandEditerVM.MakeCommands();

					commentVM.ApplyCommands(commentCommands);

					if (CommandEditerVM.IsPickedColor.Value)
					{
						var color = CommandEditerVM.FreePickedColor.Value;
						commentVM.Color = color;
					}

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


		private Task<MediaInfoViewModel> GetMediaInfoVM(MediaInfoDisplayType type)
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
						vm = new SummaryVideoInfoContentViewModel(Video, _VideoDescriptionHtmlUri, PageManager);
						break;

					case MediaInfoDisplayType.Mylist:
						vm = new MylistVideoInfoContentViewModel(VideoId, Video.ThreadId, HohoemaApp.UserMylistManager);
						break;

					case MediaInfoDisplayType.Comment:
						vm = new CommentVideoInfoContentViewModel(HohoemaApp.UserSettings, Comments);
						break;

					case MediaInfoDisplayType.Shere:
						vm = new ShereVideoInfoContentViewModel(Video, _TextInputDialogService, _ToastService);
						break;

					case MediaInfoDisplayType.Settings:
						vm = new SettingsVideoInfoContentViewModel(HohoemaApp.UserSettings.PlayerSettings);
						break;
					default:
						throw new NotSupportedException();
				}

				_SidePaneContentCache.Add(type, vm);
			}

			return Task.FromResult(vm);
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


		private DelegateCommand _OpenVideoPageWithBrowser;
		public DelegateCommand OpenVideoPageWithBrowser
		{
			get
			{
				return _OpenVideoPageWithBrowser
					?? (_OpenVideoPageWithBrowser = new DelegateCommand(async () =>
					{
						var watchPageUri = Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl + Video.RawVideoId;
						await Windows.System.Launcher.LaunchUriAsync(new Uri(watchPageUri));
					}
					));
			}
		}

		private DelegateCommand _ToggleFullScreenCommand;
		public DelegateCommand ToggleFullScreenCommand
		{
			get
			{
				return _ToggleFullScreenCommand
					?? (_ToggleFullScreenCommand = new DelegateCommand(() =>
					{
						IsFullScreen.Value = !IsFullScreen.Value;
					}
					));
			}
		}


		



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

		public ReactiveProperty<object> VideoStream { get; private set; }

		public ReactiveProperty<NicoVideoQuality> CurrentVideoQuality { get; private set; }
		public ReactiveProperty<bool> CanToggleCurrentQualityCacheState { get; private set; }
		public ReactiveProperty<bool> IsSaveRequestedCurrentQualityCache { get; private set; }
		public ReactiveProperty<string> ToggleQualityText { get; private set; }

		public ReactiveProperty<bool> IsPlayWithCache { get; private set; }
		public ReactiveProperty<bool> DownloadCompleted { get; private set; }
		public ReactiveProperty<double> ProgressPercent { get; private set; }


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
		public ReactiveProperty<double> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsFullScreen { get; private set; }



		public ReactiveProperty<bool> NowSubmittingComment { get; private set; }
		public ReactiveProperty<string> WritingComment { get; private set; }
		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
		public ReactiveProperty<bool> NowCommentWriting { get; private set; }
		public ObservableCollection<Comment> Comments { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<bool> CanResumeOnExitWritingComment { get; private set; }
		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<double> CommentCanvasWidth { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }


		public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
		public ReactiveProperty<string> CommandString { get; private set; }

		public ReactiveProperty<MediaInfoViewModel> SidePaneContent { get; private set; }
		private Dictionary<MediaInfoDisplayType, MediaInfoViewModel> _SidePaneContentCache;
		public ReactiveProperty<MediaInfoDisplayType> SelectedSidePaneType { get; private set; }
		public List<MediaInfoDisplayType> Types { get; private set; }

		private Uri _VideoDescriptionHtmlUri;


		// プログレス
		public ObservableCollection<ProgressFragment> ProgressFragments { get; private set; }
		private VideoDownloadProgress _TempProgress;

		// 再生できない場合の補助

		private bool _IsCannotPlay;
		public bool IsNotSupportVideoType
		{
			get { return _IsCannotPlay; }
			set { SetProperty(ref _IsCannotPlay, value); }
		}

		private string _CannotPlayReason;
		public string CannotPlayReason
		{
			get { return _CannotPlayReason; }
			set { SetProperty(ref _CannotPlayReason, value); }
		}


		ToastNotificationService _ToastService;
		TextInputDialogService _TextInputDialogService;



		// TODO: コメントのNGユーザー登録
		internal Task AddNgUser(Comment commentViewModel)
		{
			if (commentViewModel.UserId == null) { return Task.CompletedTask; }

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

			return Task.CompletedTask;
		}
	}


	


	

	public class TagViewModel
	{
		public string TagText { get; private set; }
		public bool IsCategoryTag { get; private set; }
		public bool IsLock { get; private set; }
		PageManager _PageManager;


		public TagViewModel(string tag, PageManager pageManager)
		{
			_PageManager = pageManager;

			TagText = tag;
			IsCategoryTag = false;
			IsLock = false;
		}

		public TagViewModel(Tag tag, PageManager pageManager)
		{
			_PageManager = pageManager;

			TagText = tag.Value;
			IsCategoryTag = tag.Category;
			IsLock = tag.Lock;
		}


		private DelegateCommand _OpenSearchPageWithTagCommand;
		public DelegateCommand OpenSearchPageWithTagCommand
		{
			get
			{
				return _OpenSearchPageWithTagCommand
					?? (_OpenSearchPageWithTagCommand = new DelegateCommand(() => 
					{
						var payload = new SearchPagePayload(
							new Models.TagSearchPagePayloadContent()
							{
								Keyword = TagText,
								Sort = Sort.FirstRetrieve,
								Order = Order.Descending
							}
							);

						_PageManager.OpenPage(
							HohoemaPageType.Search
							, payload.ToParameterString()
							);
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

	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Mylist,
		Comment,
		Shere,
		Settings,
	}










}
