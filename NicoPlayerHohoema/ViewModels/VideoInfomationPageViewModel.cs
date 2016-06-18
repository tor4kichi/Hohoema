using Mntone.Nico2.Videos.WatchAPI;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml;
using NicoPlayerHohoema.Models;
using Mntone.Nico2;
using Windows.UI.Core;
using NicoPlayerHohoema.ViewModels.VideoInfoContent;
using Mntone.Nico2.Videos.Thumbnail;
using Prism.Commands;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoInfomationPageViewModel : ViewModelBase, IDisposable
	{


		public VideoInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, INavigationService ns)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
			_NavigationService = ns;

			_CompositeDisposable = new CompositeDisposable();

			IsLowQuality = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			CanPlayOriginalQuality = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			CanPlayLowQuality = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			CanDownloadCacheOriginalQuality = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);
			CanDownloadCacheLowQuality = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			PlayOriginalQualityVideoCommand = CanPlayOriginalQuality
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);
			PlayOriginalQualityVideoCommand.Subscribe(_ => OpenPlayer(NicoVideoQuality.Original));

			PlayLowQualityVideoCommand = CanPlayLowQuality
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);
			PlayLowQualityVideoCommand.Subscribe(_ => OpenPlayer(NicoVideoQuality.Low));

			SaveOriginalQualityVideoCommand = CanDownloadCacheOriginalQuality
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);
			SaveOriginalQualityVideoCommand.Subscribe(_ => SaveVideo(NicoVideoQuality.Original));

			SaveLowQualityVideoCommand = CanDownloadCacheLowQuality
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);
			SaveLowQualityVideoCommand.Subscribe(_ => SaveVideo(NicoVideoQuality.Low));

		}

		private void OpenPlayer(NicoVideoQuality quality)
		{
			var json = new VideoPlayPayload()
			{
				VideoId = VideoId,
				Quality = quality
			}.ToParameterString();

			_PageManager.OpenPage(HohoemaPageType.VideoPlayer, json);
		}

		private async void SaveVideo(NicoVideoQuality quality)
		{
			await NicoVideo.RequestCache(quality);
		}

		public void Dispose()
		{
		}



		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (VideoId != null)
			{
				return;
			}

			if (e.Parameter is WatchApiResponse)
			{
				VideoInfo = e.Parameter as WatchApiResponse;
				VideoId = VideoInfo.flashvars.videoId;
			}
			else
			{
				if (e?.Parameter is string)
				{
					VideoId = e.Parameter as string;
				}
				else if (viewModelState.ContainsKey(nameof(VideoId)))
				{
					VideoId = (string)viewModelState[nameof(VideoId)];
				}



				var currentUIDispatcher = Window.Current.Dispatcher;

				try
				{
					if (VideoId == null) { return; }

					NicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(VideoId);

					VideoInfo = NicoVideo.CachedWatchApiResponse;

					// 再生・キャッシュボタンの状態を設定

					NowRestrictedLowQualityMode = NicoVideo.NowLowQualityOnly;

					CanPlayLowQuality.Value = true;
					CanDownloadCacheLowQuality.Value = NicoVideo.CanRequestDownloadLowQuality;
					CanDownloadCacheOriginalQuality.Value = NicoVideo.CanRequestDownloadOriginalQuality;

					IsLowQuality.Value = NowRestrictedLowQualityMode;
					CanPlayOriginalQuality.Value = !NowRestrictedLowQualityMode;


					// TODO: オフライン時の再生・キャッシュ状況の反映
					var nowOffline = false;
					if (nowOffline)
					{
						CanDownloadCacheOriginalQuality.Value = false;
						CanDownloadCacheLowQuality.Value = false;

						CanPlayOriginalQuality.Value = NicoVideo.OriginalQualityCacheState == NicoVideoCacheState.Cached;
						CanPlayLowQuality.Value = NicoVideo.LowQualityCacheState == NicoVideoCacheState.Cached;
					}

					NicoVideo.ObserveProperty(x => x.OriginalQualityCacheState)
						.Subscribe(x =>
						{
							IsOriginalQualityCached = x == NicoVideoCacheState.Cached;
							CanDownloadCacheOriginalQuality.Value = NicoVideo.CanRequestDownloadOriginalQuality;
						});

					NicoVideo.ObserveProperty(x => x.LowQualityCacheState)
						.Subscribe(x =>
						{
							IsLowQualityCached = x == NicoVideoCacheState.Cached;
							CanDownloadCacheLowQuality.Value = NicoVideo.CanRequestDownloadLowQuality;
						});
					
				}
				catch (Exception exception)
				{
					// 動画情報の取得に失敗
					System.Diagnostics.Debug.Write(exception.Message);
					return;
				}
			}


			try
			{
				ThumbnailResponse = await _HohoemaApp.MediaManager.GetThumbnail(VideoId);

				UserName = ThumbnailResponse.UserName;
				UserIconUrl = ThumbnailResponse.UserIconUrl;
				SubmitDate = ThumbnailResponse.PostedAt.LocalDateTime;

				//			UserName = response.UserName;

				Title = ThumbnailResponse.Title;
				PlayCount = ThumbnailResponse.ViewCount;
				CommentCount = ThumbnailResponse.CommentCount;
				MylistCount = ThumbnailResponse.MylistCount;
				ThumbnailUrl = ThumbnailResponse.ThumbnailUrl;

				/*
				IsOnlyOriginalQuality = ThumbnailResponse.SizeLow == 0;
				if (IsOnlyOriginalQuality)
				{
					CanPlayLowQuality.Value = false;
					CanDownloadCacheLowQuality.Value = false;
					IsLowQuality.Value = false;
				}
				*/
			}
			catch (Exception exception)
			{
				System.Diagnostics.Debug.Write("動画サムネイル情報の取得または反映に失敗しました。");
				System.Diagnostics.Debug.Write(exception.Message);
			}

//			_PageManager.PageTitle = Title;



			var uri = await VideoDescriptionHelper.PartHtmlOutputToCompletlyHtml(VideoId, VideoInfo.videoDetail.description);

			RelationVideoInfoContentViewModel relatedVideoVM = new RelationVideoInfoContentViewModel(VideoId, _HohoemaApp.ContentFinder, _HohoemaApp.UserSettings.NGSettings, _PageManager);
			VideoInfoContentItems = new List<MediaInfoViewModel>()
			{
				new SummaryVideoInfoContentViewModel(ThumbnailResponse, uri, _PageManager),
				new TagsVideoInfoContentViewModel(ThumbnailResponse, _PageManager),
				relatedVideoVM,
//				new IchibaVideoInfoContentViewModel(VideoId, _HohoemaApp.ContentFinder)
			};

			await relatedVideoVM.LoadRelatedVideo();


			OnPropertyChanged(nameof(VideoInfoContentItems));




			base.OnNavigatedTo(e, viewModelState);
		}



		

		private DelegateCommand _NavigationBackCommand;
		public DelegateCommand NavigationBackCommand
		{
			get
			{
				return _NavigationBackCommand
					?? (_NavigationBackCommand = new DelegateCommand(() =>
					{
						_NavigationService.GoBack();
					}
					, () => _NavigationService.CanGoBack()
					));
			}
		}

		CompositeDisposable _CompositeDisposable;


		public ReactiveCommand PlayOriginalQualityVideoCommand { get; private set; }
		public ReactiveCommand PlayLowQualityVideoCommand { get; private set; }
		public ReactiveCommand SaveOriginalQualityVideoCommand { get; private set; }
		public ReactiveCommand SaveLowQualityVideoCommand { get; private set; }

		private bool _IsOnlyOriginalQuality;
		public bool IsOnlyOriginalQuality
		{
			get { return _IsOnlyOriginalQuality; }
			set { SetProperty(ref _IsOnlyOriginalQuality, value); }
		}

		public ReactiveProperty<bool> IsLowQuality { get; private set; }

		private bool _NowRestrictedLowQualityMode;
		public bool NowRestrictedLowQualityMode
		{
			get { return _NowRestrictedLowQualityMode; }
			set { SetProperty(ref _NowRestrictedLowQualityMode, value); }
		}

		

		public ReactiveProperty<bool> CanPlayOriginalQuality { get; private set; }
		public ReactiveProperty<bool> CanDownloadCacheOriginalQuality { get; private set; }

		public ReactiveProperty<bool> CanPlayLowQuality { get; private set; }
		public ReactiveProperty<bool> CanDownloadCacheLowQuality { get; private set; }


		public NicoVideo NicoVideo { get; private set; }

		public string VideoId { get; private set; }

		private WatchApiResponse _VideoInfo;
		public WatchApiResponse VideoInfo
		{
			get { return _VideoInfo; }
			set { SetProperty(ref _VideoInfo, value); }
		}



		public List<MediaInfoViewModel> VideoInfoContentItems { get; private set; }
		

		public ThumbnailResponse ThumbnailResponse { get; private set; }

		private bool _IsOriginalQualityChaced;
		public bool IsOriginalQualityCached
		{
			get { return _IsOriginalQualityChaced; }
			set { SetProperty(ref _IsOriginalQualityChaced, value); }
		}

		private bool _IsLowQualityChaced;
		public bool IsLowQualityCached
		{
			get { return _IsLowQualityChaced; }
			set { SetProperty(ref _IsLowQualityChaced, value); }
		}


		private string _Title;
		public string Title
		{
			get { return _Title; }
			set { SetProperty(ref _Title, value); }
		}

		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}



		private Uri _UserIconUrl;
		public Uri UserIconUrl
		{
			get { return _UserIconUrl; }
			set { SetProperty(ref _UserIconUrl, value); }
		}

		private DateTime _SubmitDate;
		public DateTime SubmitDate
		{
			get { return _SubmitDate; }
			set { SetProperty(ref _SubmitDate, value); }
		}



		private uint _PlayCount;
		public uint PlayCount
		{
			get { return _PlayCount; }
			set { SetProperty(ref _PlayCount, value); }
		}


		private uint _CommentCount;
		public uint CommentCount
		{
			get { return _CommentCount; }
			set { SetProperty(ref _CommentCount, value); }
		}



		private uint _MylistCount;
		public uint MylistCount
		{
			get { return _MylistCount; }
			set { SetProperty(ref _MylistCount, value); }
		}

		private Uri _ThumbnailUrl;
		public Uri ThumbnailUrl
		{
			get { return _ThumbnailUrl; }
			set { SetProperty(ref _ThumbnailUrl, value); }
		}

		private bool _NowDownload;
		public bool NowDownload
		{
			get { return _NowDownload; }
			set { SetProperty(ref _NowDownload, value); }
		}

		private HohoemaApp _HohoemaApp;
		private PageManager _PageManager;
		private INavigationService _NavigationService;
	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Related,
		Ichiba,
	}

	abstract public class MediaInfoViewModel : BindableBase
	{
	}
}
